using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;

namespace DraftApp.Api.Services;

/// <summary>
/// Service for managing tournament events.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Creates a new tournament event.
    /// </summary>
    Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Joins an event as a player.
    /// </summary>
    Task<JoinEventResponse?> JoinEventAsync(JoinEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets an event snapshot by ID.
    /// </summary>
    Task<EventSnapshotResponse?> GetEventAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Validates a host token and returns the event ID if valid.
    /// </summary>
    Task<Guid?> ValidateHostTokenAsync(string hostToken, CancellationToken ct = default);

    /// <summary>
    /// Starts an event and generates round 1 pairings.
    /// </summary>
    Task<MutationResponse> StartEventAsync(Guid eventId, int expectedVersion, CancellationToken ct = default);

    /// <summary>
    /// Publishes pairings for a round.
    /// </summary>
    Task<MutationResponse> PublishPairingsAsync(Guid eventId, int roundNumber, int expectedVersion, CancellationToken ct = default);

    /// <summary>
    /// Finalizes a match result.
    /// </summary>
    Task<MutationResponse> FinalizeMatchAsync(Guid eventId, Guid matchId, Guid winnerId, int expectedVersion, CancellationToken ct = default);

    /// <summary>
    /// Drops a player from the tournament.
    /// </summary>
    Task<MutationResponse> DropPlayerAsync(Guid eventId, Guid playerId, int expectedVersion, string? reason, CancellationToken ct = default);

    /// <summary>
    /// Allocates prizes after the tournament completes.
    /// </summary>
    Task<MutationResponse> AllocatePrizesAsync(Guid eventId, int expectedVersion, CancellationToken ct = default);

    /// <summary>
    /// Gets tournament standings.
    /// </summary>
    Task<StandingsResponse?> GetStandingsAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Gets audit log for an event.
    /// </summary>
    Task<AuditLogResponse?> GetAuditLogAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Reopens a finalized match, reversing the result.
    /// </summary>
    Task<MutationResponse> ReopenMatchAsync(Guid eventId, Guid matchId, int expectedVersion, string reason, CancellationToken ct = default);

    /// <summary>
    /// Swaps two players between their respective matches.
    /// </summary>
    Task<MutationResponse> SwapOpponentsAsync(
        Guid eventId,
        int roundNumber,
        Guid matchId1,
        Guid playerId1,
        Guid matchId2,
        Guid playerId2,
        int expectedVersion,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Reopens all finalized matches in a round.
    /// </summary>
    Task<MutationResponse> ReopenRoundAsync(Guid eventId, int roundNumber, int expectedVersion, string reason, CancellationToken ct = default);

    /// <summary>
    /// Regenerates pairings for a round.
    /// </summary>
    Task<MutationResponse> RegeneratePairingsAsync(Guid eventId, int roundNumber, int expectedVersion, string reason, CancellationToken ct = default);
}
