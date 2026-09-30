using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Models.Responses;

/// <summary>
/// A single audit log entry.
/// </summary>
public sealed record AuditLogEntry
{
    public required Guid Id { get; init; }

    public required AuditActionType ActionType { get; init; }

    public required string EntityType { get; init; }

    public required Guid? EntityId { get; init; }

    public required string? Reason { get; init; }

    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the round the action applied to, for round-level actions.
    /// </summary>
    public int? RoundNumber { get; init; }

    /// <summary>
    /// Gets the winner recorded at the time, for match results.
    /// </summary>
    public Guid? WinnerId { get; init; }

    /// <summary>
    /// Gets the player's name as recorded at the time (players who left may no longer be in the event).
    /// </summary>
    public string? PlayerName { get; init; }
}
