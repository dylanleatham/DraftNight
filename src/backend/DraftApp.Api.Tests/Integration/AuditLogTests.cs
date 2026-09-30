using System.Net.Http.Json;
using System.Text.Json;
using DraftApp.Api.Data.Enums;
using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;

namespace DraftApp.Api.Tests.Integration;

/// <summary>
/// Tests that audit entries carry the details the UI needs to describe them.
/// </summary>
public class AuditLogTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SqliteWebApplicationFactory factory;
    private readonly HttpClient client;

    public AuditLogTests()
    {
        factory = new SqliteWebApplicationFactory();
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AuditLog_IncludesPlayerName_WinnerId_AndRoundNumber()
    {
        // Arrange: 3-player round-robin, finalize round 1, publish round 2
        var created = await PostAsync<CreateEventResponse>("/api/events", new CreateEventRequest
        {
            Name = "Audit Test",
            PacksInBox = 36,
            HostPin = "1234",
            HostName = "Host"
        });
        foreach (var name in new[] { "Alice", "Bob" })
        {
            await PostAsync<JoinEventResponse>("/api/events/join", new JoinEventRequest { JoinCode = created.JoinCode, PlayerName = name, PlayerPin = "0000" });
        }

        var host = created.HostToken;
        await PostAsync<MutationResponse>($"/api/events/{created.EventId}/start", new HostActionRequest { ExpectedVersion = (await SnapshotAsync(created.EventId)).Version }, host);

        var snapshot = await SnapshotAsync(created.EventId);
        var match = snapshot.Rounds.Single(r => r.RoundNumber == 1).Matches.Single(m => !m.IsBye);
        await PostAsync<MutationResponse>(
            $"/api/events/{created.EventId}/matches/{match.Id}/finalize",
            new FinalizeMatchRequest { WinnerId = match.PlayerAId, ExpectedVersion = snapshot.Version },
            host);

        await PostAsync<MutationResponse>(
            $"/api/events/{created.EventId}/rounds/2/publish",
            new HostActionRequest { ExpectedVersion = (await SnapshotAsync(created.EventId)).Version },
            host);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/events/{created.EventId}/audit");
        request.Headers.Add("X-Host-Token", host);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var audit = (await response.Content.ReadFromJsonAsync<AuditLogResponse>(JsonOptions))!;

        // Assert
        Assert.Contains(audit.Entries, e => e.ActionType == AuditActionType.PlayerJoined && e.PlayerName == "Alice");

        var finalized = Assert.Single(audit.Entries, e => e.ActionType == AuditActionType.MatchFinalized);
        Assert.Equal(match.Id, finalized.EntityId);
        Assert.Equal(match.PlayerAId, finalized.WinnerId);

        var published = Assert.Single(audit.Entries, e => e.ActionType == AuditActionType.PairingsGenerated);
        Assert.Equal(2, published.RoundNumber);
    }

    private async Task<EventSnapshotResponse> SnapshotAsync(Guid eventId) =>
        (await client.GetFromJsonAsync<EventSnapshotResponse>($"/api/events/{eventId}", JsonOptions))!;

    private async Task<T> PostAsync<T>(string url, object body, string? hostToken = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (hostToken is not null)
        {
            request.Headers.Add("X-Host-Token", hostToken);
        }

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{url} failed: {content}");
        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }
}
