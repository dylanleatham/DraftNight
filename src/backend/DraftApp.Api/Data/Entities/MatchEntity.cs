using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a match pairing/result.
/// </summary>
public class MatchEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the round.
    /// </summary>
    public Guid RoundId { get; set; }

    /// <summary>
    /// Gets or sets the match code in engine format (e.g., "r1-m0").
    /// </summary>
    public required string MatchCode { get; set; }

    /// <summary>
    /// Gets or sets the round number (denormalized for queries).
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to Player A.
    /// </summary>
    public Guid PlayerAId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to Player B, null for BYE/sit.
    /// </summary>
    public Guid? PlayerBId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to winner, null if not finalized.
    /// </summary>
    public Guid? WinnerId { get; set; }

    /// <summary>
    /// Gets or sets the current match status.
    /// </summary>
    public MatchStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when the match was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the round this match belongs to.
    /// </summary>
    public RoundEntity Round { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Player A.
    /// </summary>
    public PlayerEntity PlayerA { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Player B (null for BYE/sit).
    /// </summary>
    public PlayerEntity? PlayerB { get; set; }

    /// <summary>
    /// Gets or sets the Winner (null if not finalized).
    /// </summary>
    public PlayerEntity? Winner { get; set; }
}
