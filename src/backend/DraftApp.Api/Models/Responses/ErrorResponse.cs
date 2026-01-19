namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Standard error response.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// Error code for client handling.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Additional details (optional).
    /// </summary>
    public object? Details { get; init; }
}
