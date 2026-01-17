using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Input data for a golden test case.
/// </summary>
public sealed class GoldenInput
{
    [JsonPropertyName("eventId")]
    public required string EventId { get; init; }

    [JsonPropertyName("players")]
    public required List<GoldenPlayer> Players { get; init; }

    [JsonPropertyName("packsInBox")]
    public required int PacksInBox { get; init; }

    [JsonPropertyName("matchResults")]
    public required Dictionary<int, List<GoldenMatchResult>> MatchResults { get; init; }
}
