using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to create a new tournament event.
/// </summary>
public sealed record CreateEventRequest
{
    /// <summary>
    /// Event display name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Total packs in the booster box.
    /// </summary>
    [Required]
    [Range(6, 48)]
    public required int PacksInBox { get; init; }

    /// <summary>
    /// Host PIN for administrative access.
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string HostPin { get; init; }
}
