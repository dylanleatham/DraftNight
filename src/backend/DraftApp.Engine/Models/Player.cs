using System.Collections.Immutable;

namespace DraftApp.Engine.Models;

/// <summary>
/// Immutable player state record.
/// </summary>
public sealed record Player
{
    /// <summary>
    /// Gets the stable unique identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the seed position (1..N, lower is earlier).
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Gets the match wins count.
    /// </summary>
    public int MatchWins { get; init; } = 0;

    /// <summary>
    /// Gets the match losses count.
    /// </summary>
    public int MatchLosses { get; init; } = 0;

    /// <summary>
    /// Gets a value indicating whether the player has received a BYE (Swiss only).
    /// </summary>
    public bool ByeReceived { get; init; } = false;

    /// <summary>
    /// Gets the ordered list of opponent player IDs (excludes BYE).
    /// </summary>
    public ImmutableList<string> Opponents { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the map of opponent ID to the most recent round they played against.
    /// </summary>
    public ImmutableDictionary<string, int> LastPlayedRound { get; init; } =
        ImmutableDictionary<string, int>.Empty;

    /// <summary>
    /// Gets a value indicating whether the player has dropped from the tournament.
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
    /// Returns a new player with BYE flag cleared (for regenerating pairings).
    /// </summary>
    public Player WithByeCleared() => this with { ByeReceived = false };

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
    /// Returns a new player with a decremented match win count.
    /// </summary>
    public Player WithMatchWinsDecreased() => this with { MatchWins = Math.Max(0, MatchWins - 1) };

    /// <summary>
    /// Returns a new player with a decremented match loss count.
    /// </summary>
    public Player WithMatchLossesDecreased() => this with { MatchLosses = Math.Max(0, MatchLosses - 1) };

    /// <summary>
    /// Returns a new player with an opponent removed (for match reopen).
    /// </summary>
    public Player WithOpponentRemoved(string opponentId, int round)
    {
        // Only remove if the last played round matches
        if (LastPlayedRound.TryGetValue(opponentId, out var lastRound) && lastRound == round)
        {
            // Find and remove the last occurrence of this opponent
            var lastIndex = Opponents.LastIndexOf(opponentId);
            if (lastIndex >= 0)
            {
                return this with
                {
                    Opponents = Opponents.RemoveAt(lastIndex),
                    LastPlayedRound = LastPlayedRound.Remove(opponentId)
                };
            }
        }

        return this;
    }

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
