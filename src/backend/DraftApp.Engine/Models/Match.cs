namespace DraftApp.Engine.Models;

/// <summary>
/// Immutable match pairing/result record.
/// </summary>
public sealed record Match
{
    /// <summary>
    /// Gets the unique match identifier (format: r{round}-m{index}).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the round number (1-indexed).
    /// </summary>
    public required int Round { get; init; }

    /// <summary>
    /// Gets the first player ID.
    /// </summary>
    public required string PlayerAId { get; init; }

    /// <summary>
    /// Gets the second player ID, or null for a BYE match (Swiss only).
    /// </summary>
    public string? PlayerBId { get; init; }

    /// <summary>
    /// Gets the winner ID if the match is complete, null otherwise.
    /// </summary>
    public string? WinnerId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a BYE match (no opponent).
    /// </summary>
    public bool IsBye => PlayerBId is null;

    /// <summary>
    /// Gets a value indicating whether the match has a result.
    /// </summary>
    public bool IsComplete => WinnerId is not null;

    /// <summary>
    /// Returns a new match with the specified winner.
    /// </summary>
    public Match WithWinner(string winnerId) => this with { WinnerId = winnerId };

    /// <summary>
    /// Returns a new match with the winner cleared (for reopening).
    /// </summary>
    public Match ClearWinner() => this with { WinnerId = null };

    /// <summary>
    /// Returns a new match with a different player A.
    /// </summary>
    public Match WithPlayerA(string playerId) => this with { PlayerAId = playerId };

    /// <summary>
    /// Returns a new match with a different player B.
    /// </summary>
    public Match WithPlayerB(string playerId) => this with { PlayerBId = playerId };

    /// <summary>
    /// Creates a regular match between two players.
    /// </summary>
    public static Match Create(string id, int round, string playerAId, string playerBId) => new()
    {
        Id = id,
        Round = round,
        PlayerAId = playerAId,
        PlayerBId = playerBId
    };

    /// <summary>
    /// Creates a BYE match for the specified player (Swiss only).
    /// </summary>
    public static Match CreateBye(string id, int round, string playerId) => new()
    {
        Id = id,
        Round = round,
        PlayerAId = playerId,
        PlayerBId = null,
        WinnerId = playerId // BYE matches are auto-finalized with the player as winner
    };

    /// <summary>
    /// Creates a "sit" match for round-robin with odd players.
    /// This represents a player sitting out, NOT a win.
    /// </summary>
    public static Match CreateSit(string id, int round, string playerId) => new()
    {
        Id = id,
        Round = round,
        PlayerAId = playerId,
        PlayerBId = null,
        WinnerId = null // Sit matches have no winner
    };
}
