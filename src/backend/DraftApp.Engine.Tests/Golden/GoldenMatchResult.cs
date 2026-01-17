using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Match result data for a golden test case.
/// </summary>
public sealed class GoldenMatchResult
{
    [JsonPropertyName("matchId")]
    public required string MatchId { get; init; }

    [JsonPropertyName("winnerId")]
    public required string WinnerId { get; init; }
}
