using System.Net.Http.Json;
using System.Text.Json;
using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore.Diagnostics;

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

    public SignalRIntegrationTests()
    {
        factory = new SqliteWebApplicationFactory();

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
            HostPin = "1234",
            HostName = "Host"
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
            HostPin = "1234",
            HostName = "Host"
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

    [Fact]
    public async Task TwoClients_JoinSameEvent_BothReceiveUpdates()
    {
        // Arrange - Create an event and join a player
        var createRequest = new CreateEventRequest
        {
            Name = "Test Event",
            PacksInBox = 36,
            HostPin = "1234",
            HostName = "Host"
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

        // The host is seated as a player on creation, so Alice is the second player
        Assert.Equal(2, latestClient1Snapshot.Players.Count);
        Assert.Equal(2, latestClient2Snapshot.Players.Count);
        Assert.Contains(latestClient1Snapshot.Players, p => p.Name == "Alice");
        Assert.Contains(latestClient2Snapshot.Players, p => p.Name == "Alice");

        // Cleanup
        await client1.DisposeAsync();
        await client2.DisposeAsync();
    }

    [Fact]
    public async Task RequestSnapshot_AfterStateChange_ReturnsUpdatedState()
    {
        // Arrange - Create an event
        var createRequest = new CreateEventRequest
        {
            Name = "Snapshot Update Test",
            PacksInBox = 36,
            HostPin = "1234",
            HostName = "Host"
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

        var snapshots = new List<EventSnapshotResponse>();
        var snapshotReceived = new TaskCompletionSource<bool>();

        hubConnection.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            snapshots.Add(snapshot);
            snapshotReceived.TrySetResult(true);
        });

        await hubConnection.StartAsync();

        // Join the event group
        await hubConnection.InvokeAsync("JoinEventGroup", createResult.EventId);

        // Wait for initial snapshot
        await Task.WhenAny(snapshotReceived.Task, Task.Delay(5000));
        Assert.True(snapshots.Count >= 1, "Did not receive initial snapshot");
        Assert.Single(snapshots[0].Players); // just the host

        // Act - Add a player via REST API
        var joinRequest = new JoinEventRequest
        {
            JoinCode = createResult.JoinCode,
            PlayerName = "TestPlayer",
            PlayerPin = "0000"
        };
        var joinResponse = await httpClient.PostAsJsonAsync("/api/events/join", joinRequest);
        var joinContent = await joinResponse.Content.ReadAsStringAsync();
        Assert.True(joinResponse.IsSuccessStatusCode, $"Join failed: {joinResponse.StatusCode} - {joinContent}");

        // Now request a fresh snapshot
        snapshots.Clear();
        var updatedSnapshotReceived = new TaskCompletionSource<bool>();
        hubConnection.Remove("EventUpdated");
        hubConnection.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            snapshots.Add(snapshot);
            updatedSnapshotReceived.TrySetResult(true);
        });

        await hubConnection.InvokeAsync("RequestSnapshot", createResult.EventId);

        // Assert - Snapshot should include the new player
        var received = await Task.WhenAny(updatedSnapshotReceived.Task, Task.Delay(5000)) == updatedSnapshotReceived.Task;
        Assert.True(received, "Did not receive updated snapshot within timeout");
        Assert.NotEmpty(snapshots);
        var latestSnapshot = snapshots[^1];
        Assert.Equal(2, latestSnapshot.Players.Count);
        Assert.Contains(latestSnapshot.Players, p => p.Name == "TestPlayer");

        // Cleanup
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task Client_RejoinsGroup_AfterReconnect_ReceivesCurrentState()
    {
        // Arrange - Create an event and add a player
        var createRequest = new CreateEventRequest
        {
            Name = "Reconnect Test",
            PacksInBox = 36,
            HostPin = "1234",
            HostName = "Host"
        };
        var createResponse = await httpClient.PostAsJsonAsync("/api/events", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, $"Create failed: {createContent}");
        var createResult = JsonSerializer.Deserialize<CreateEventResponse>(createContent, JsonOptions);
        Assert.NotNull(createResult);

        // Add a player before the client connects
        var joinRequest = new JoinEventRequest
        {
            JoinCode = createResult.JoinCode,
            PlayerName = "ExistingPlayer",
            PlayerPin = "0000"
        };
        var joinResponse = await httpClient.PostAsJsonAsync("/api/events/join", joinRequest);
        var joinContent = await joinResponse.Content.ReadAsStringAsync();
        Assert.True(joinResponse.IsSuccessStatusCode, $"Join failed: {joinResponse.StatusCode} - {joinContent}");

        var server = factory.Server;

        // First connection - simulate initial connection
        var connection1 = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        EventSnapshotResponse? initialSnapshot = null;
        var initialReceived = new TaskCompletionSource<bool>();

        connection1.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            initialSnapshot = snapshot;
            initialReceived.TrySetResult(true);
        });

        await connection1.StartAsync();
        await connection1.InvokeAsync("JoinEventGroup", createResult.EventId);

        var received1 = await Task.WhenAny(initialReceived.Task, Task.Delay(5000)) == initialReceived.Task;
        Assert.True(received1, "Did not receive initial snapshot");
        Assert.NotNull(initialSnapshot);
        Assert.Equal(2, initialSnapshot.Players.Count); // host + ExistingPlayer

        // Simulate disconnection
        await connection1.StopAsync();
        await connection1.DisposeAsync();

        // Act - Simulate reconnection with a new connection (as would happen after network recovery)
        var connection2 = new HubConnectionBuilder()
            .WithUrl(
                server.BaseAddress + "hubs/event",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        EventSnapshotResponse? reconnectSnapshot = null;
        var reconnectReceived = new TaskCompletionSource<bool>();

        connection2.On<EventSnapshotResponse>("EventUpdated", snapshot =>
        {
            reconnectSnapshot = snapshot;
            reconnectReceived.TrySetResult(true);
        });

        await connection2.StartAsync();

        // Rejoin the event group (as the client would do after reconnect)
        await connection2.InvokeAsync("JoinEventGroup", createResult.EventId);

        // Assert - Should receive current state including the player
        var received2 = await Task.WhenAny(reconnectReceived.Task, Task.Delay(5000)) == reconnectReceived.Task;
        Assert.True(received2, "Did not receive snapshot after reconnect");
        Assert.NotNull(reconnectSnapshot);
        Assert.Equal(createResult.EventId, reconnectSnapshot.Id);
        Assert.Equal(2, reconnectSnapshot.Players.Count);
        Assert.Contains(reconnectSnapshot.Players, p => p.Name == "ExistingPlayer");

        // Cleanup
        await connection2.DisposeAsync();
    }
}
