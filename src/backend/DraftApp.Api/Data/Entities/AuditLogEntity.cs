using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Immutable audit log entry for tracking changes.
/// </summary>
public class AuditLogEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the type of action performed.
    /// </summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the type of entity affected.
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the affected entity, if applicable.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of state before the change.
    /// </summary>
    public string? BeforeJson { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of state after the change.
    /// </summary>
    public string? AfterJson { get; set; }

    /// <summary>
    /// Gets or sets the reason or description of the change.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets when the audit entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the event this log entry belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;
}
