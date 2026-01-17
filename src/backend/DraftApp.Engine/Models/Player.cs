using System.Collections.Immutable;

namespace DraftApp.Engine.Models;

/// <summary>
/// Immutable player state record.
/// </summary>
public sealed record Player
{
    /// <summary>
    /// Stable unique identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Seed position (1..N, lower is earlier).
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Match wins count.
    /// </summary>
    public int MatchWins { get; init; } = 0;

    /// <summary>
    /// Match losses count.
    /// </summary>
    public int MatchLosses { get; init; } = 0;

    /// <summary>
    /// Whether the player has received a BYE (Swiss only).
    /// </summary>
    public bool ByeReceived { get; init; } = false;

    /// <summary>
    /// Ordered list of opponent player IDs (excludes BYE).
    /// </summary>
    public ImmutableList<string> Opponents { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Maps opponent ID to the most recent round they played against.
    /// </summary>
    public ImmutableDictionary<string, int> LastPlayedRound { get; init; } =
        ImmutableDictionary<string, int>.Empty;

    /// <summary>
    /// Whether the player has dropped from the tournament.
    /// </summary>
    public bool IsDropped { get; init; } = false;

    /// <summary>
    /// Returns a new player with an incremented match win count.
    /// </summary>
    public Player WithWin() => this with { MatchWins = MatchWins + 1 };

    /// <summary>
    /// Returns a new player with an incremented match loss count.
    /// </summary>
    public Player WithLoss() => this with { MatchLosses = MatchLosses + 1 };

    /// <summary>
    /// Returns a new player marked as having received a BYE.
    /// </summary>
    public Player WithByeReceived() => this with { ByeReceived = true };

    /// <summary>
    /// Returns a new player with an opponent added.
    /// </summary>
    public Player WithOpponent(string opponentId, int round) => this with
    {
        Opponents = Opponents.Add(opponentId),
        LastPlayedRound = LastPlayedRound.SetItem(opponentId, round)
    };

    /// <summary>
    /// Returns a new player marked as dropped.
    /// </summary>
    public Player WithDropped() => this with { IsDropped = true };

    /// <summary>
    /// Creates a new player with the specified ID, name, and seed.
    /// </summary>
    public static Player Create(string id, string name, int seed) => new()
    {
        Id = id,
        Name = name,
        Seed = seed
    };
}
