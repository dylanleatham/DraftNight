namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a player in a tournament.
/// </summary>
public class PlayerEntity
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
    /// Gets or sets the player display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the seed position (1..N, lower is earlier).
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Gets or sets the match wins count.
    /// </summary>
    public int MatchWins { get; set; }

    /// <summary>
    /// Gets or sets the match losses count.
    /// </summary>
    public int MatchLosses { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the player has received a BYE (Swiss only).
    /// </summary>
    public bool ByeReceived { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the player has dropped from the tournament.
    /// </summary>
    public bool IsDropped { get; set; }

    /// <summary>
    /// Gets or sets the JSON array of opponent player IDs in order.
    /// </summary>
    public string OpponentsJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the JSON dictionary mapping opponent ID to last played round.
    /// </summary>
    public string LastPlayedRoundJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the salted hash of the player PIN.
    /// </summary>
    public string? PinHash { get; set; }

    /// <summary>
    /// Gets or sets the secret token for player authorization.
    /// </summary>
    public string? PlayerToken { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the event this player belongs to.
    /// </summary>
    public EventEntity Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets the matches where this player is Player A.
    /// </summary>
    public ICollection<MatchEntity> MatchesAsPlayerA { get; set; } = new List<MatchEntity>();

    /// <summary>
    /// Gets or sets the matches where this player is Player B.
    /// </summary>
    public ICollection<MatchEntity> MatchesAsPlayerB { get; set; } = new List<MatchEntity>();

    /// <summary>
    /// Gets or sets the matches where this player won.
    /// </summary>
    public ICollection<MatchEntity> MatchesWon { get; set; } = new List<MatchEntity>();

    /// <summary>
    /// Gets or sets the prize allocation for this player, if any.
    /// </summary>
    public PrizeAllocationEntity? PrizeAllocation { get; set; }
}
