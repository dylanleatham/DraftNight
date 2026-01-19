namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Response after joining an event.
/// </summary>
public sealed record JoinEventResponse
{
    /// <summary>
    /// The event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// The assigned player ID.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Player token for authorization.
    /// </summary>
    public required string PlayerToken { get; init; }
}
