using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DraftApp.Api.Data;
using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;
using DraftApp.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DraftApp.Api.Tests.Integration;

/// <summary>
/// Tests for token storage, resuming a seat with name + PIN, and rate limiting.
/// </summary>
public class SessionSecurityTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SqliteWebApplicationFactory factory;
    private readonly HttpClient client;

    public SessionSecurityTests()
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
    public async Task Tokens_AreStoredHashed_NotAsIssued()
    {
        var created = await CreateEventAsync();
        var joined = await JoinAsync(created.JoinCode, "Alice", "5555");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DraftAppDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var evt = await db.Events.Include(e => e.Players).SingleAsync(e => e.Id == created.EventId);
        Assert.NotEqual(created.HostToken, evt.HostToken);
        Assert.Equal(auth.HashToken(created.HostToken), evt.HostToken);

        var alice = evt.Players.Single(p => p.Id == joined.PlayerId);
        Assert.NotEqual(joined.PlayerToken, alice.PlayerToken);
        Assert.Equal(auth.HashToken(joined.PlayerToken), alice.PlayerToken);
    }

    [Fact]
    public async Task Resume_WithCorrectPin_IssuesNewPlayerToken_AndRevokesOldOne()
    {
        var created = await CreateEventAsync();
        var joined = await JoinAsync(created.JoinCode, "Alice", "5555");

        var response = await client.PostAsJsonAsync("/api/events/resume", new ResumeSessionRequest
        {
            JoinCode = created.JoinCode.ToLowerInvariant(),
            PlayerName = " alice ",
            Pin = "5555"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resumed = (await response.Content.ReadFromJsonAsync<ResumeSessionResponse>(JsonOptions))!;
        Assert.Equal(created.EventId, resumed.EventId);
        Assert.Equal(joined.PlayerId, resumed.PlayerId);
        Assert.NotEqual(joined.PlayerToken, resumed.PlayerToken);
        Assert.Null(resumed.HostToken);

        Assert.Equal(HttpStatusCode.Unauthorized, await LeaveAsync(created.EventId, joined.PlayerToken));
        Assert.Equal(HttpStatusCode.OK, await LeaveAsync(created.EventId, resumed.PlayerToken));
    }

    [Fact]
    public async Task Resume_HostSeat_ReturnsWorkingHostToken_AndRevokesOldOne()
    {
        var created = await CreateEventAsync();

        var response = await client.PostAsJsonAsync("/api/events/resume", new ResumeSessionRequest
        {
            JoinCode = created.JoinCode,
            PlayerName = "Host",
            Pin = "1234"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resumed = (await response.Content.ReadFromJsonAsync<ResumeSessionResponse>(JsonOptions))!;
        Assert.Equal(created.PlayerId, resumed.PlayerId);
        Assert.NotNull(resumed.HostToken);

        Assert.Equal(HttpStatusCode.Unauthorized, await GetAuditStatusAsync(created.EventId, created.HostToken));
        Assert.Equal(HttpStatusCode.OK, await GetAuditStatusAsync(created.EventId, resumed.HostToken!));
    }

    [Fact]
    public async Task Resume_PlayerWithSamePinAsHost_DoesNotGetHostToken()
    {
        var created = await CreateEventAsync(hostPin: "1234");
        await JoinAsync(created.JoinCode, "Mallory", "1234");

        var response = await client.PostAsJsonAsync("/api/events/resume", new ResumeSessionRequest
        {
            JoinCode = created.JoinCode,
            PlayerName = "Mallory",
            Pin = "1234"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resumed = (await response.Content.ReadFromJsonAsync<ResumeSessionResponse>(JsonOptions))!;
        Assert.Null(resumed.HostToken);
    }

    [Theory]
    [InlineData("Alice", "9999")] // wrong PIN
    [InlineData("Nobody", "5555")] // unknown name
    public async Task Resume_WithWrongNameOrPin_ReturnsUnauthorized(string name, string pin)
    {
        var created = await CreateEventAsync();
        await JoinAsync(created.JoinCode, "Alice", "5555");

        var response = await client.PostAsJsonAsync("/api/events/resume", new ResumeSessionRequest
        {
            JoinCode = created.JoinCode,
            PlayerName = name,
            Pin = pin
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Resume_WithUnknownJoinCode_ReturnsUnauthorized()
    {
        var response = await client.PostAsJsonAsync("/api/events/resume", new ResumeSessionRequest
        {
            JoinCode = "ZZZZZZ",
            PlayerName = "Alice",
            Pin = "5555"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Resume_IsRateLimited_AfterTenAttemptsPerMinute()
    {
        var request = new ResumeSessionRequest { JoinCode = "ZZZZZZ", PlayerName = "Alice", Pin = "0000" };

        for (var i = 0; i < 10; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/events/resume", request);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/events/resume", request);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    private async Task<CreateEventResponse> CreateEventAsync(string hostPin = "1234")
    {
        var response = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            Name = "Security Test",
            PacksInBox = 36,
            HostPin = hostPin,
            HostName = "Host"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateEventResponse>(JsonOptions))!;
    }

    private async Task<JoinEventResponse> JoinAsync(string joinCode, string name, string pin)
    {
        var response = await client.PostAsJsonAsync("/api/events/join", new JoinEventRequest
        {
            JoinCode = joinCode,
            PlayerName = name,
            PlayerPin = pin
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JoinEventResponse>(JsonOptions))!;
    }

    private async Task<HttpStatusCode> LeaveAsync(Guid eventId, string playerToken)
    {
        var snapshot = (await client.GetFromJsonAsync<EventSnapshotResponse>($"/api/events/{eventId}", JsonOptions))!;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/leave")
        {
            Content = JsonContent.Create(new LeaveEventRequest { ExpectedVersion = snapshot.Version })
        };
        request.Headers.Add("X-Player-Token", playerToken);
        return (await client.SendAsync(request)).StatusCode;
    }

    private async Task<HttpStatusCode> GetAuditStatusAsync(Guid eventId, string hostToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/events/{eventId}/audit");
        request.Headers.Add("X-Host-Token", hostToken);
        return (await client.SendAsync(request)).StatusCode;
    }
}
