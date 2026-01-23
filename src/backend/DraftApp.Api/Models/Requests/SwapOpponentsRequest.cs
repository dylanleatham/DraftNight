using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to swap opponents between two matches.
/// </summary>
public sealed record SwapOpponentsRequest
{
    /// <summary>
    /// First match ID.
    /// </summary>
    [Required]
    public required Guid MatchId1 { get; init; }

    /// <summary>
    /// Player ID to move from match 1 to match 2.
    /// </summary>
    [Required]
    public required Guid PlayerId1 { get; init; }

    /// <summary>
    /// Second match ID.
    /// </summary>
    [Required]
    public required Guid MatchId2 { get; init; }

    /// <summary>
    /// Player ID to move from match 2 to match 1.
    /// </summary>
    [Required]
    public required Guid PlayerId2 { get; init; }

    /// <summary>
    /// Expected event version for optimistic concurrency.
    /// </summary>
    [Required]
    public required int ExpectedVersion { get; init; }

    /// <summary>
    /// Reason for swapping opponents (required for audit).
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Reason { get; init; }
}
