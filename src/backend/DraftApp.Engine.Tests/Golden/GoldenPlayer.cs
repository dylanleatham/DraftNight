using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Player data for a golden test case.
/// </summary>
public sealed class GoldenPlayer
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
