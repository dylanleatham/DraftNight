using System.Collections.Immutable;

namespace DraftApp.Engine.Models;

/// <summary>
/// Immutable tournament event state.
/// </summary>
public sealed record EventState
{
    /// <summary>
    /// Gets the unique event identifier.
    /// </summary>
    public required string EventId { get; init; }

    /// <summary>
    /// Gets players indexed by ID.
    /// </summary>
    public required ImmutableDictionary<string, Player> Players { get; init; }

    /// <summary>
    /// Gets total packs in the booster box.
    /// </summary>
    public required int PacksInBox { get; init; }

    /// <summary>
    /// Gets available prize packs (P = B - 3N).
    /// </summary>
    public required int PrizePacks { get; init; }

    /// <summary>
    /// Gets tournament format (RoundRobin or Swiss).
    /// </summary>
    public required TournamentFormat Format { get; init; }

    /// <summary>
    /// Gets total number of rounds.
    /// </summary>
    public required int TotalRounds { get; init; }

    /// <summary>
    /// Gets matches grouped by round number.
    /// </summary>
    public ImmutableDictionary<int, ImmutableList<Match>> MatchesByRound { get; init; } =
        ImmutableDictionary<int, ImmutableList<Match>>.Empty;

    /// <summary>
    /// Gets prize allocations by player ID.
    /// </summary>
    public ImmutableDictionary<string, int> PrizeAllocations { get; init; } =
        ImmutableDictionary<string, int>.Empty;

    /// <summary>
    /// Gets a value indicating whether prizes have been allocated.
    /// </summary>
    public bool PrizesAllocated { get; init; }

    /// <summary>
    /// Gets the number of players.
    /// </summary>
    public int PlayerCount => Players.Count;

    /// <summary>
    /// Gets the current round number (highest round with pairings, or 0 if none).
    /// </summary>
    public int CurrentRound => MatchesByRound.Count > 0 ? MatchesByRound.Keys.Max() : 0;

    /// <summary>
    /// Gets a value indicating whether the tournament is complete.
    /// </summary>
    public bool IsComplete => CurrentRound == TotalRounds && IsRoundComplete(TotalRounds);

    /// <summary>
    /// Gets all players in seed order.
    /// </summary>
    public IEnumerable<Player> GetPlayersInSeedOrder() =>
        Players.Values.OrderBy(p => p.Seed).ThenBy(p => p.Id);

    /// <summary>
    /// Gets all active (non-dropped) players.
    /// </summary>
    public IEnumerable<Player> GetActivePlayers() =>
        Players.Values.Where(p => !p.IsDropped);

    /// <summary>
    /// Returns true if the specified round is complete.
    /// </summary>
    public bool IsRoundComplete(int round)
    {
        if (!MatchesByRound.TryGetValue(round, out var matches))
        {
            return false;
        }

        return matches.All(m => m.IsComplete || (m.IsBye && m.WinnerId is null));
    }

    /// <summary>
    /// Gets matches for the specified round.
    /// </summary>
    public ImmutableList<Match> GetRoundMatches(int round) =>
        MatchesByRound.TryGetValue(round, out var matches)
            ? matches
            : ImmutableList<Match>.Empty;

    /// <summary>
    /// Gets all winners for the specified round.
    /// </summary>
    public IEnumerable<string> GetRoundWinners(int round) =>
        GetRoundMatches(round)
            .Where(m => m.WinnerId is not null)
            .Select(m => m.WinnerId!);

    /// <summary>
    /// Returns a new state with updated players.
    /// </summary>
    public EventState WithPlayers(ImmutableDictionary<string, Player> players) =>
        this with { Players = players };

    /// <summary>
    /// Returns a new state with an updated player.
    /// </summary>
    public EventState WithPlayer(Player player) =>
        this with { Players = Players.SetItem(player.Id, player) };

    /// <summary>
    /// Returns a new state with matches added for a round.
    /// </summary>
    public EventState WithRoundMatches(int round, ImmutableList<Match> matches) =>
        this with { MatchesByRound = MatchesByRound.SetItem(round, matches) };

    /// <summary>
    /// Returns a new state with an updated match.
    /// </summary>
    public EventState WithMatch(int round, Match match)
    {
        var roundMatches = GetRoundMatches(round);
        var index = roundMatches.FindIndex(m => m.Id == match.Id);
        if (index < 0)
        {
            return this;
        }

        var updatedMatches = roundMatches.SetItem(index, match);
        return this with { MatchesByRound = MatchesByRound.SetItem(round, updatedMatches) };
    }

    /// <summary>
    /// Returns a new state with prize allocations set.
    /// </summary>
    public EventState WithPrizeAllocations(ImmutableDictionary<string, int> allocations) =>
        this with { PrizeAllocations = allocations, PrizesAllocated = true };
}
