namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Tournament standings response.
/// </summary>
public sealed record StandingsResponse
{
    public required IReadOnlyList<StandingEntry> Standings { get; init; }
}
