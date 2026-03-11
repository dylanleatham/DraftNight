using DraftApp.Api.Models.Responses;
using DraftApp.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace DraftApp.Api.Hubs;

/// <summary>
/// SignalR hub for real-time event synchronization.
/// </summary>
public sealed class EventHub(IEventService eventService) : Hub
{
    /// <summary>
    /// Joins the client to an event's group and returns the current snapshot.
    /// </summary>
    /// <param name="eventId">The event ID to join.</param>
    public async Task JoinEventGroup(Guid eventId)
    {
        var snapshot = await eventService.GetEventAsync(eventId);
        if (snapshot is null)
        {
            await Clients.Caller.SendAsync("Error", "EVENT_NOT_FOUND", "The specified event does not exist.");
            return;
        }

        var groupName = GetGroupName(eventId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("EventUpdated", snapshot);
    }

    /// <summary>
    /// Removes the client from an event's group.
    /// </summary>
    /// <param name="eventId">The event ID to leave.</param>
    public async Task LeaveEventGroup(Guid eventId)
    {
        var groupName = GetGroupName(eventId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Requests a fresh snapshot of the event state (for reconnection/resync).
    /// </summary>
    /// <param name="eventId">The event ID to request snapshot for.</param>
    public async Task RequestSnapshot(Guid eventId)
    {
        var snapshot = await eventService.GetEventAsync(eventId);
        if (snapshot is null)
        {
            await Clients.Caller.SendAsync("Error", "EVENT_NOT_FOUND", "The specified event does not exist.");
            return;
        }

        // Re-add to group so reconnecting clients don't miss subsequent updates
        var groupName = GetGroupName(eventId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("EventUpdated", snapshot);
    }

    /// <summary>
    /// Gets the group name for an event.
    /// </summary>
    internal static string GetGroupName(Guid eventId) => $"event_{eventId}";
}
