using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to swap opponents between two matches.
/// </summary>
public sealed record SwapOpponentsRequest
{
    /// <summary>
    /// Gets the first match ID.
    /// </summary>
    [Required]
    public required Guid MatchId1 { get; init; }

    /// <summary>
    /// Gets the player ID to move from match 1 to match 2.
    /// </summary>
    [Required]
    public required Guid PlayerId1 { get; init; }

    /// <summary>
    /// Gets the second match ID.
    /// </summary>
    [Required]
    public required Guid MatchId2 { get; init; }

    /// <summary>
    /// Gets the player ID to move from match 2 to match 1.
    /// </summary>
    [Required]
    public required Guid PlayerId2 { get; init; }

    /// <summary>
    /// Gets the expected event version for optimistic concurrency.
    /// </summary>
    [Required]
    public required int ExpectedVersion { get; init; }

    /// <summary>
    /// Gets the reason for swapping opponents (required for audit).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Reason { get; init; }
}
