namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Response after creating an event.
/// </summary>
public sealed record CreateEventResponse
{
    /// <summary>
    /// Gets the new event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Gets the join code for players.
    /// </summary>
    public required string JoinCode { get; init; }

    /// <summary>
    /// Gets the host token for authorization.
    /// </summary>
    public required string HostToken { get; init; }

    /// <summary>
    /// Gets the host's player ID.
    /// </summary>
    public required Guid PlayerId { get; init; }

    /// <summary>
    /// Gets the host's player token for authorization.
    /// </summary>
    public required string PlayerToken { get; init; }
}
