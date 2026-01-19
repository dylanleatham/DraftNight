using System.ComponentModel.DataAnnotations;

namespace DraftApp.Api.Models.Requests;

/// <summary>
/// Base request for host actions requiring version and optional reason.
/// </summary>
public sealed record HostActionRequest
{
    /// <summary>
    /// Expected event version for optimistic concurrency.
    /// </summary>
    [Required]
    public required int ExpectedVersion { get; init; }

    /// <summary>
    /// Optional reason for audit log.
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; init; }
}
