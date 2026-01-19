using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Immutable audit log entry for tracking changes.
/// </summary>
public class AuditLogEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Type of action performed.
    /// </summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>
    /// Type of entity affected.
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// ID of the affected entity, if applicable.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// JSON representation of state before the change.
    /// </summary>
    public string? BeforeJson { get; set; }

    /// <summary>
    /// JSON representation of state after the change.
    /// </summary>
    public string? AfterJson { get; set; }

    /// <summary>
    /// Reason or description of the change.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// When the audit entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The event this log entry belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;
}
