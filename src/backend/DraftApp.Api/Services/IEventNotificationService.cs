using DraftApp.Api.Models.Responses;

namespace DraftApp.Api.Services;

/// <summary>
/// Service for broadcasting event updates to connected clients.
/// </summary>
public interface IEventNotificationService
{
    /// <summary>
    /// Broadcasts an event update to all clients in the event's group.
    /// </summary>
    /// <param name="eventId">The event ID to broadcast to.</param>
    /// <param name="snapshot">The current event snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    Task BroadcastEventUpdateAsync(Guid eventId, EventSnapshotResponse snapshot, CancellationToken ct = default);
}
