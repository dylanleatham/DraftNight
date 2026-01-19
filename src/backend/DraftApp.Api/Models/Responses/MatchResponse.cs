using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Match data in event snapshot.
/// </summary>
public sealed record MatchResponse
{
    public required Guid Id { get; init; }

    public required string MatchCode { get; init; }

    public required Guid PlayerAId { get; init; }

    public required Guid? PlayerBId { get; init; }

    public required Guid? WinnerId { get; init; }

    public required MatchStatus Status { get; init; }

    public required bool IsBye { get; init; }
}
