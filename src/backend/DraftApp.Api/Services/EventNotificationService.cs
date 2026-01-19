using DraftApp.Api.Hubs;
using DraftApp.Api.Models.Responses;
using Microsoft.AspNetCore.SignalR;

namespace DraftApp.Api.Services;

/// <summary>
/// Implementation of event notification service using SignalR.
/// </summary>
public sealed class EventNotificationService(IHubContext<EventHub> hubContext) : IEventNotificationService
{
    /// <inheritdoc />
    public async Task BroadcastEventUpdateAsync(Guid eventId, EventSnapshotResponse snapshot, CancellationToken ct = default)
    {
        var groupName = EventHub.GetGroupName(eventId);
        await hubContext.Clients.Group(groupName).SendAsync("EventUpdated", snapshot, ct);
    }
}
