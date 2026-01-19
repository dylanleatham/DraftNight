using DraftApp.Api.Data.Enums;

namespace DraftApp.Api.Data.Entities;

/// <summary>
/// Persistent entity for a match pairing/result.
/// </summary>
public class MatchEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the round.
    /// </summary>
    public Guid RoundId { get; set; }

    /// <summary>
    /// Match code in engine format (e.g., "r1-m0").
    /// </summary>
    public required string MatchCode { get; set; }

    /// <summary>
    /// Round number (denormalized for queries).
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Foreign key to Player A.
    /// </summary>
    public Guid PlayerAId { get; set; }

    /// <summary>
    /// Foreign key to Player B, null for BYE/sit.
    /// </summary>
    public Guid? PlayerBId { get; set; }

    /// <summary>
    /// Foreign key to winner, null if not finalized.
    /// </summary>
    public Guid? WinnerId { get; set; }

    /// <summary>
    /// Current match status.
    /// </summary>
    public MatchStatus Status { get; set; }

    /// <summary>
    /// When the match was finalized.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// The round this match belongs to.
    /// </summary>
    public RoundEntity Round { get; set; } = null!;

    /// <summary>
    /// Player A.
    /// </summary>
    public PlayerEntity PlayerA { get; set; } = null!;

    /// <summary>
    /// Player B (null for BYE/sit).
    /// </summary>
    public PlayerEntity? PlayerB { get; set; }

    /// <summary>
    /// Winner (null if not finalized).
    /// </summary>
    public PlayerEntity? Winner { get; set; }
}
