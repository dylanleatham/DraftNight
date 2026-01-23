using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to regenerate pairings for a round.
/// </summary>
public sealed record RegeneratePairingsRequest
{
    /// <summary>
    /// Expected event version for optimistic concurrency.
    /// </summary>
    [Required]
    public required int ExpectedVersion { get; init; }

    /// <summary>
    /// Reason for regenerating pairings (required for audit).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Reason { get; init; }
}
