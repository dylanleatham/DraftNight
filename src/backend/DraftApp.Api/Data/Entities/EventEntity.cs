using DraftApp.Api.Data.Enums;
using DraftApp.Engine.Models;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a tournament event.
/// </summary>
public class EventEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the current event status.
    /// </summary>
    public EventStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the join code for players to join the event.
    /// </summary>
    public string? JoinCode { get; set; }

    /// <summary>
    /// Gets or sets the total packs in the booster box.
    /// </summary>
    public int PacksInBox { get; set; }

    /// <summary>
    /// Gets or sets the available prize packs (P = B - 3N).
    /// </summary>
    public int PrizePacks { get; set; }

    /// <summary>
    /// Gets or sets the tournament format (RoundRobin or Swiss).
    /// </summary>
    public TournamentFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the total number of rounds.
    /// </summary>
    public int TotalRounds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether prizes have been allocated.
    /// </summary>
    public bool PrizesAllocated { get; set; }

    /// <summary>
    /// Gets or sets the optimistic concurrency version token.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets when the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the event was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the salted hash of the host PIN.
    /// </summary>
    public required string HostPinHash { get; set; }

    /// <summary>
    /// Gets or sets the secret token for host authorization.
    /// </summary>
    public required string HostToken { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the players in this event.
    /// </summary>
    public ICollection<PlayerEntity> Players { get; set; } = new List<PlayerEntity>();

    /// <summary>
    /// Gets or sets the rounds in this event.
    /// </summary>
    public ICollection<RoundEntity> Rounds { get; set; } = new List<RoundEntity>();

    /// <summary>
    /// Gets or sets the prize allocations for this event.
    /// </summary>
    public ICollection<PrizeAllocationEntity> PrizeAllocations { get; set; } = new List<PrizeAllocationEntity>();

    /// <summary>
    /// Gets or sets the audit logs for this event.
    /// </summary>
    public ICollection<AuditLogEntity> AuditLogs { get; set; } = new List<AuditLogEntity>();
}
