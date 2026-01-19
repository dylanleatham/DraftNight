namespace DraftApp.Api.Models.Responses;

/// <summary>
/// Response after creating an event.
/// </summary>
public sealed record CreateEventResponse
{
    /// <summary>
    /// The new event ID.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Join code for players.
    /// </summary>
    public required string JoinCode { get; init; }

    /// <summary>
    /// Host token for authorization.
    /// </summary>
    public required string HostToken { get; init; }
}
