using DraftApp.Api.Data.Enums;
using DraftApp.Engine.Models;

namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Full event snapshot returned from API.
/// </summary>
public sealed record EventSnapshotResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required EventStatus Status { get; init; }

    public required string? JoinCode { get; init; }

    public required int PacksInBox { get; init; }

    public required int PrizePacks { get; init; }

    public required TournamentFormat Format { get; init; }

    public required int TotalRounds { get; init; }

    public required int CurrentRound { get; init; }

    public required bool PrizesAllocated { get; init; }

    public required int Version { get; init; }

    public required IReadOnlyList<PlayerResponse> Players { get; init; }

    public required IReadOnlyList<RoundResponse> Rounds { get; init; }

    public required IReadOnlyList<PrizeAllocationResponse> PrizeAllocations { get; init; }
}
