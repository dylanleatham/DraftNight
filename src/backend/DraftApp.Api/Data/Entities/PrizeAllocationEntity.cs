namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a prize allocation.
/// </summary>
public class PrizeAllocationEntity
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
    /// Gets or sets the foreign key to the player.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// Gets or sets the number of packs awarded.
    /// </summary>
    public int PacksAwarded { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the event this allocation belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets the player receiving the prize.
    /// </summary>
    public PlayerEntity Player { get; set; } = null!;
}
