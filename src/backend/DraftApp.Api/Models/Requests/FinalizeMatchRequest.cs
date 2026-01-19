using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Request to finalize a match result.
/// </summary>
public sealed record FinalizeMatchRequest
{
    /// <summary>
    /// The winner's player ID.
    /// </summary>
    [Required]
    public required Guid WinnerId { get; init; }

    /// <summary>
    /// Expected event version for optimistic concurrency.
    /// </summary>
    [Required]
    public required int ExpectedVersion { get; init; }
}
