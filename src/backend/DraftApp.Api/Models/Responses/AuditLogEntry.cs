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
}
