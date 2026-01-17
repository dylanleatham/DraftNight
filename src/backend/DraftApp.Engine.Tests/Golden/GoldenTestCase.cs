using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Represents a golden test case with inputs and expected outputs.
/// </summary>
public sealed class GoldenTestCase
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("input")]
    public required GoldenInput Input { get; init; }

    [JsonPropertyName("expected")]
    public required GoldenExpected Expected { get; init; }
}
