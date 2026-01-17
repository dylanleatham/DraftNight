using System.Text.Json.Serialization;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Match data for a golden test case.
/// </summary>
public sealed class GoldenMatch
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("playerAId")]
    public required string PlayerAId { get; init; }

    [JsonPropertyName("playerBId")]
    public string? PlayerBId { get; init; }

    [JsonPropertyName("isBye")]
    public required bool IsBye { get; init; }

    [JsonPropertyName("winnerId")]
    public string? WinnerId { get; init; }
}
