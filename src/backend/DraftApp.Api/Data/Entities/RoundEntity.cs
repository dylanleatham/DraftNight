using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a tournament round.
/// </summary>
public class RoundEntity
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
    /// Gets or sets the round number (1-indexed).
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Gets or sets the current round status.
    /// </summary>
    public RoundStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when pairings were published.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets when the round was closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the event this round belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets the matches in this round.
    /// </summary>
    public ICollection<MatchEntity> Matches { get; set; } = new List<MatchEntity>();
}
