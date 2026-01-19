namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Player data in event snapshot.
/// </summary>
public sealed record PlayerResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required int Seed { get; init; }

    public required int MatchWins { get; init; }

    public required int MatchLosses { get; init; }

    public required bool ByeReceived { get; init; }

    public required bool IsDropped { get; init; }
}
