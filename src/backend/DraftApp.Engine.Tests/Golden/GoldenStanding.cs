using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Standing data for a golden test case.
/// </summary>
public sealed class GoldenStanding
{
    [JsonPropertyName("playerId")]
    public required string PlayerId { get; init; }

    [JsonPropertyName("matchWins")]
    public required int MatchWins { get; init; }

    [JsonPropertyName("matchLosses")]
    public required int MatchLosses { get; init; }

    [JsonPropertyName("seed")]
    public required int Seed { get; init; }
}
