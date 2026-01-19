using DraftApp.Api.Data.Enums;
using DraftApp.Engine.Models;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a tournament event.
/// </summary>
public class EventEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Current event status.
    /// </summary>
    public EventStatus Status { get; set; }

    /// <summary>
    /// Join code for players to join the event.
    /// </summary>
    public string? JoinCode { get; set; }

    /// <summary>
    /// Total packs in the booster box.
    /// </summary>
    public int PacksInBox { get; set; }

    /// <summary>
    /// Available prize packs (P = B - 3N).
    /// </summary>
    public int PrizePacks { get; set; }

    /// <summary>
    /// Tournament format (RoundRobin or Swiss).
    /// </summary>
    public TournamentFormat Format { get; set; }

    /// <summary>
    /// Total number of rounds.
    /// </summary>
    public int TotalRounds { get; set; }

    /// <summary>
    /// Whether prizes have been allocated.
    /// </summary>
    public bool PrizesAllocated { get; set; }

    /// <summary>
    /// Optimistic concurrency version token.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// When the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the event was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Salted hash of the host PIN.
    /// </summary>
    public required string HostPinHash { get; set; }

    /// <summary>
    /// Secret token for host authorization.
    /// </summary>
    public required string HostToken { get; set; }

    // Navigation properties

    /// <summary>
    /// Players in this event.
    /// </summary>
    public ICollection<PlayerEntity> Players { get; set; } = new List<PlayerEntity>();

    /// <summary>
    /// Rounds in this event.
    /// </summary>
    public ICollection<RoundEntity> Rounds { get; set; } = new List<RoundEntity>();

    /// <summary>
    /// Prize allocations for this event.
    /// </summary>
    public ICollection<PrizeAllocationEntity> PrizeAllocations { get; set; } = new List<PrizeAllocationEntity>();

    /// <summary>
    /// Audit logs for this event.
    /// </summary>
    public ICollection<AuditLogEntity> AuditLogs { get; set; } = new List<AuditLogEntity>();
}
