using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request for a player to leave an event.
/// </summary>
public sealed record LeaveEventRequest
{
    /// <summary>
    /// Gets the expected event version for optimistic concurrency.
    /// </summary>
    [Required]
    public required int ExpectedVersion { get; init; }
}
