using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a tournament round.
/// </summary>
public class RoundEntity
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
    /// Round number (1-indexed).
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Current round status.
    /// </summary>
    public RoundStatus Status { get; set; }

    /// <summary>
    /// When pairings were published.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// When the round was closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The event this round belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;

    /// <summary>
    /// Matches in this round.
    /// </summary>
    public ICollection<MatchEntity> Matches { get; set; } = new List<MatchEntity>();
}
