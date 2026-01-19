using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Round data in event snapshot.
/// </summary>
public sealed record RoundResponse
{
    public required int RoundNumber { get; init; }

    public required RoundStatus Status { get; init; }

    public required IReadOnlyList<MatchResponse> Matches { get; init; }
}
