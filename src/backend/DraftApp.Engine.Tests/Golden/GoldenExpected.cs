using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Expected output data for a golden test case.
/// </summary>
public sealed class GoldenExpected
{
    [JsonPropertyName("format")]
    public required string Format { get; init; }

    [JsonPropertyName("totalRounds")]
    public required int TotalRounds { get; init; }

    [JsonPropertyName("prizePacks")]
    public required int PrizePacks { get; init; }

    [JsonPropertyName("rounds")]
    public required Dictionary<int, List<GoldenMatch>> Rounds { get; init; }

    [JsonPropertyName("finalStandings")]
    public required List<GoldenStanding> FinalStandings { get; init; }

    [JsonPropertyName("prizeAllocations")]
    public required Dictionary<string, int> PrizeAllocations { get; init; }
}
