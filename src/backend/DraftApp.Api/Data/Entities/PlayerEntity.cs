namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a player in a tournament.
/// </summary>
public class PlayerEntity
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
    /// Player display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Seed position (1..N, lower is earlier).
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Match wins count.
    /// </summary>
    public int MatchWins { get; set; }

    /// <summary>
    /// Match losses count.
    /// </summary>
    public int MatchLosses { get; set; }

    /// <summary>
    /// Whether the player has received a BYE (Swiss only).
    /// </summary>
    public bool ByeReceived { get; set; }

    /// <summary>
    /// Whether the player has dropped from the tournament.
    /// </summary>
    public bool IsDropped { get; set; }

    /// <summary>
    /// JSON array of opponent player IDs in order.
    /// </summary>
    public string OpponentsJson { get; set; } = "[]";

    /// <summary>
    /// JSON dictionary mapping opponent ID to last played round.
    /// </summary>
    public string LastPlayedRoundJson { get; set; } = "{}";

    /// <summary>
    /// Salted hash of the player PIN.
    /// </summary>
    public string? PinHash { get; set; }

    /// <summary>
    /// Secret token for player authorization.
    /// </summary>
    public string? PlayerToken { get; set; }

    // Navigation properties

    /// <summary>
    /// The event this player belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;

    /// <summary>
    /// Matches where this player is Player A.
    /// </summary>
    public ICollection<MatchEntity> MatchesAsPlayerA { get; set; } = new List<MatchEntity>();

    /// <summary>
    /// Matches where this player is Player B.
    /// </summary>
    public ICollection<MatchEntity> MatchesAsPlayerB { get; set; } = new List<MatchEntity>();

    /// <summary>
    /// Matches where this player won.
    /// </summary>
    public ICollection<MatchEntity> MatchesWon { get; set; } = new List<MatchEntity>();

    /// <summary>
    /// Prize allocation for this player, if any.
    /// </summary>
    public PrizeAllocationEntity? PrizeAllocation { get; set; }
}
