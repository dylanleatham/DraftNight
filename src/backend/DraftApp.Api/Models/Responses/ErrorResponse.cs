namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Standard error response.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// Gets the error code for client handling.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the additional details (optional).
    /// </summary>
    public object? Details { get; init; }
}
