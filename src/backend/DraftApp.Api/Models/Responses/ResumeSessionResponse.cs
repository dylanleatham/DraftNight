namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Response after successfully resuming a session. Tokens are freshly issued; any
/// previously issued tokens for this seat stop working.
/// </summary>
public sealed record ResumeSessionResponse
{
    /// <summary>
    /// Gets the event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the event join code.
    /// </summary>
    public required string JoinCode { get; init; }

    /// <summary>
    /// Gets the player ID of the reclaimed seat.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Gets the new player token.
    /// </summary>
    public required string PlayerToken { get; init; }

    /// <summary>
    /// Gets the new host token, present only when the reclaimed seat belongs to the host.
    /// </summary>
    public string? HostToken { get; init; }
}
