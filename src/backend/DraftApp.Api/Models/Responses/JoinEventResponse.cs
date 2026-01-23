namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Response after joining an event.
/// </summary>
public sealed record JoinEventResponse
{
    /// <summary>
    /// Gets the event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the assigned player ID.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Gets the player token for authorization.
    /// </summary>
    public required string PlayerToken { get; init; }
}
