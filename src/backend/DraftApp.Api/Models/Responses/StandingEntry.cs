namespace DraftApp.Api.Models.Responses;

/// <summary>
/// A single entry in the standings.
/// </summary>
public sealed record StandingEntry
{
    public required int Rank { get; init; }

    public required Guid PlayerId { get; init; }

    public required string PlayerName { get; init; }

    public required int MatchWins { get; init; }

    public required int MatchLosses { get; init; }

    public required bool ByeReceived { get; init; }

    public required bool IsDropped { get; init; }
}
