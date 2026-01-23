using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to join an event as a player.
/// </summary>
public sealed record JoinEventRequest
{
    /// <summary>
    /// Gets the join code for the event.
    /// </summary>
    [Required]
    [StringLength(10, MinimumLength = 4)]
    public required string JoinCode { get; init; }

    /// <summary>
    /// Gets the player display name.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string PlayerName { get; init; }

    /// <summary>
    /// Gets the player PIN for editing their own data.
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string PlayerPin { get; init; }
}
