namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a prize allocation.
/// </summary>
public class PrizeAllocationEntity
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
    /// Foreign key to the player.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Number of packs awarded.
    /// </summary>
    public int PacksAwarded { get; set; }

    // Navigation properties

    /// <summary>
    /// The event this allocation belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;

    /// <summary>
    /// The player receiving the prize.
    /// </summary>
    public PlayerEntity Player { get; set; } = null!;
}
