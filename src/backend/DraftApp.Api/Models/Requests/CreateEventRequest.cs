using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to create a new tournament event.
/// </summary>
public sealed record CreateEventRequest
{
    /// <summary>
    /// Gets the event display name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total packs in the booster box.
    /// </summary>
    [Required]
    [Range(6, 48)]
    public required int PacksInBox { get; init; }

    /// <summary>
    /// Gets the host PIN for administrative access.
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string HostPin { get; init; }

    /// <summary>
    /// Gets the host's player name.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string HostName { get; init; }
}
