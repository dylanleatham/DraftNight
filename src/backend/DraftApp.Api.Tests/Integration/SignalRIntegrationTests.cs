using System.Net.Http.Json;
using System.Text.Json;
using DraftApp.Api.Data;
using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DraftApp.Api.Tests.Integration;

/// <summary>
/// Integration tests for SignalR real-time synchronization.
/// </summary>
public class SignalRIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient httpClient;
    private readonly string databaseName;

    public SignalRIntegrationTests()
    {
        databaseName = $"TestDb_{Guid.NewGuid()}";

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all EF Core related services to avoid provider conflicts
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<DraftAppDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType == typeof(DraftAppDbContext) ||
                        (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("EntityFramework")) ||
                        (d.ImplementationType?.FullName != null && d.ImplementationType.FullName.Contains("SqlServer")))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<DraftAppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });

        httpClient = factory.CreateClient();
    }

    public void Dispose()
    {
        httpClient.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Client_CanConnectToHub()
    {
        // Arrange
        var server = factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        // Act
        await hubConnection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task JoinEventGroup_WithValidEvent_ReceivesSnapshot()
    {
        // Arrange - Create an event first
        var createRequest = new CreateEventRequest
        {
            Name = "Test Event",
            PacksInBox = 36,
            HostPin = "1234"
        };
        var createResponse = await httpClient.PostAsJsonAsync("/api/events", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, $"Create failed: {createContent}");
        var createResult = JsonSerializer.Deserialize<CreateEventResponse>(createContent, JsonOptions);
        Assert.NotNull(createResult);

        var server = factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        EventSnapshotResponse? receivedSnapshot = null;
        var snapshotReceived = new TaskCompletionSource<bool>();

        hubConnection.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            receivedSnapshot = snapshot;
            snapshotReceived.TrySetResult(true);
        });

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("JoinEventGroup", createResult.EventId);

        // Assert
        var received = await Task.WhenAny(snapshotReceived.Task, Task.Delay(5000)) == snapshotReceived.Task;
        Assert.True(received, "Did not receive snapshot within timeout");
        Assert.NotNull(receivedSnapshot);
        Assert.Equal(createResult.EventId, receivedSnapshot.Id);
        Assert.Equal("Test Event", receivedSnapshot.Name);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task JoinEventGroup_WithInvalidEvent_ReceivesError()
    {
        // Arrange
        var server = factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        string? errorCode = null;
        string? errorMessage = null;
        var errorReceived = new TaskCompletionSource<bool>();

        hubConnection.On<string, string>("Error", (code, message) =>
        {
            errorCode = code;
            errorMessage = message;
            errorReceived.TrySetResult(true);
        });

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("JoinEventGroup", Guid.NewGuid());

        // Assert
        var received = await Task.WhenAny(errorReceived.Task, Task.Delay(5000)) == errorReceived.Task;
        Assert.True(received, "Did not receive error within timeout");
        Assert.Equal("EVENT_NOT_FOUND", errorCode);
        Assert.Equal("The specified event does not exist.", errorMessage);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task RequestSnapshot_WithValidEvent_ReceivesSnapshot()
    {
        // Arrange - Create an event first
        var createRequest = new CreateEventRequest
        {
            Name = "Test Event",
            PacksInBox = 36,
            HostPin = "1234"
        };
        var createResponse = await httpClient.PostAsJsonAsync("/api/events", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, $"Create failed: {createContent}");
        var createResult = JsonSerializer.Deserialize<CreateEventResponse>(createContent, JsonOptions);
        Assert.NotNull(createResult);

        var server = factory.Server;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        EventSnapshotResponse? receivedSnapshot = null;
        var snapshotReceived = new TaskCompletionSource<bool>();

        hubConnection.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            receivedSnapshot = snapshot;
            snapshotReceived.TrySetResult(true);
        });

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("RequestSnapshot", createResult.EventId);

        // Assert
        var received = await Task.WhenAny(snapshotReceived.Task, Task.Delay(5000)) == snapshotReceived.Task;
        Assert.True(received, "Did not receive snapshot within timeout");
        Assert.NotNull(receivedSnapshot);
        Assert.Equal(createResult.EventId, receivedSnapshot.Id);

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact(Skip = "EF Core InMemory provider has issues with entity tracking across different DbContext scopes. This test passes with SQL Server.")]
    public async Task TwoClients_JoinSameEvent_BothReceiveUpdates()
    {
        // Arrange - Create an event and join a player
        var createRequest = new CreateEventRequest
        {
            Name = "Test Event",
            PacksInBox = 36,
            HostPin = "1234"
        };
        var createResponse = await httpClient.PostAsJsonAsync("/api/events", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, $"Create failed: {createContent}");
        var createResult = JsonSerializer.Deserialize<CreateEventResponse>(createContent, JsonOptions);
        Assert.NotNull(createResult);

        var server = factory.Server;

        // Create two SignalR connections
        var client1 = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var client2 = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var client1Snapshots = new List<EventSnapshotResponse>();
        var client2Snapshots = new List<EventSnapshotResponse>();
        var client1Updated = new TaskCompletionSource<bool>();
        var client2Updated = new TaskCompletionSource<bool>();

        client1.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            client1Snapshots.Add(snapshot);
            if (client1Snapshots.Count >= 2)
            {
                client1Updated.TrySetResult(true);
            }
        });

        client2.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            client2Snapshots.Add(snapshot);
            if (client2Snapshots.Count >= 2)
            {
                client2Updated.TrySetResult(true);
            }
        });

        await client1.StartAsync();
        await client2.StartAsync();

        // Both clients join the event group
        await client1.InvokeAsync("JoinEventGroup", createResult.EventId);
        await client2.InvokeAsync("JoinEventGroup", createResult.EventId);

        // Wait for initial snapshots
        await Task.Delay(500);

        // Act - Add a player via REST API (should trigger broadcast)
        var joinRequest = new JoinEventRequest
        {
            JoinCode = createResult.JoinCode,
            PlayerName = "Alice",
            PlayerPin = "0000"
        };
        var joinResponse = await httpClient.PostAsJsonAsync("/api/events/join", joinRequest);
        Assert.True(joinResponse.IsSuccessStatusCode, "Join failed");

        // Assert - Both clients should receive the update
        var bothReceived = await Task.WhenAll(
            Task.WhenAny(client1Updated.Task, Task.Delay(5000)),
            Task.WhenAny(client2Updated.Task, Task.Delay(5000)));

        Assert.True(client1Snapshots.Count >= 2, $"Client 1 only received {client1Snapshots.Count} snapshots");
        Assert.True(client2Snapshots.Count >= 2, $"Client 2 only received {client2Snapshots.Count} snapshots");

        // The latest snapshot should have the player
        var latestClient1Snapshot = client1Snapshots[^1];
        var latestClient2Snapshot = client2Snapshots[^1];

        Assert.Single(latestClient1Snapshot.Players);
        Assert.Single(latestClient2Snapshot.Players);
        Assert.Equal("Alice", latestClient1Snapshot.Players[0].Name);
        Assert.Equal("Alice", latestClient2Snapshot.Players[0].Name);

        // Cleanup
        await client1.DisposeAsync();
        await client2.DisposeAsync();
    }
}
