namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Standard response for mutation operations.
/// </summary>
public sealed record MutationResponse
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the new event version after mutation.
    /// </summary>
    public required int NewVersion { get; init; }

    /// <summary>
    /// Gets the error message if operation failed.
    /// </summary>
    public string? Error { get; init; }
}
