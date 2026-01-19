namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Standard response for mutation operations.
/// </summary>
public sealed record MutationResponse
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// New event version after mutation.
    /// </summary>
    public required int NewVersion { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? Error { get; init; }
}
