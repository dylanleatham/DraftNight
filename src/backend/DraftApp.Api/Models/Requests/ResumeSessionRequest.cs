using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to reclaim a seat on a new device (or after clearing browser data) using the
/// name and PIN chosen when joining.
/// </summary>
public sealed record ResumeSessionRequest
{
    /// <summary>
    /// Gets the join code for the event.
    /// </summary>
    [Required]
    [StringLength(10, MinimumLength = 4)]
    public required string JoinCode { get; init; }

    /// <summary>
    /// Gets the player display name used when joining.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string PlayerName { get; init; }

    /// <summary>
    /// Gets the PIN chosen when joining (the host PIN for the host's seat).
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 4)]
    public required string Pin { get; init; }
}
