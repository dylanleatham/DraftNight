using System.Collections.Immutable;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Helpers;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles prize pack allocation using later-round-first distribution.
/// </summary>
internal static class PrizeAllocator
{
    /// <summary>
    /// Allocates prize packs to players based on round wins.
    /// Later rounds are prioritized when packs are insufficient.
    /// </summary>
    /// <param name="state">Completed tournament state.</param>
    /// <returns>Updated event state with prize allocations or an error.</returns>
    public static EngineResult<EventState> Allocate(EventState state)
    {
        // Check if tournament is complete
        if (!state.IsComplete)
        {
            return EngineResult<EventState>.Fail(new TournamentNotCompleteError());
        }

        // Check if prizes already allocated
        if (state.PrizesAllocated)
        {
            return EngineResult<EventState>.Fail(new PrizesAlreadyAllocatedError());
        }

        var allocations = ImmutableDictionary.CreateBuilder<string, int>();

        // Initialize all players to 0
        foreach (var playerId in state.Players.Keys)
        {
            allocations[playerId] = 0;
        }

        var packsRemaining = state.PrizePacks;

        // Process rounds from last to first (later rounds first)
        for (var round = state.TotalRounds; round >= 1 && packsRemaining > 0; round--)
        {
            var roundWinners = GetRoundWinners(state, round);

            if (roundWinners.Count == 0)
            {
                continue;
            }

            if (roundWinners.Count <= packsRemaining)
            {
                // Enough packs for all winners
                foreach (var winnerId in roundWinners)
                {
                    allocations[winnerId] += 1;
                }

                packsRemaining -= roundWinners.Count;
            }
            else
            {
                // Not enough packs - use tie-break
                var rankedWinners = RankWinnersForRound(state, roundWinners, round);

                for (var i = 0; i < packsRemaining; i++)
                {
                    allocations[rankedWinners[i]] += 1;
                }

                packsRemaining = 0;
            }
        }

        var newState = state.WithPrizeAllocations(allocations.ToImmutable());
        return EngineResult<EventState>.Ok(newState);
    }

    /// <summary>
    /// Gets the list of player IDs who won in the specified round.
    /// Includes Swiss BYE winners.
    /// </summary>
    private static List<string> GetRoundWinners(EventState state, int round)
    {
        var winners = new List<string>();

        foreach (var match in state.GetRoundMatches(round))
        {
            // For Swiss BYE: WinnerId is set (auto-win)
            // For RR sit: WinnerId is null (no win)
            // For regular match: WinnerId is set if complete
            if (match.WinnerId is not null)
            {
                winners.Add(match.WinnerId);
            }
        }

        return winners;
    }

    /// <summary>
    /// Ranks winners for tie-breaking within a round.
    /// Uses standings after that round: MW desc, seed asc, id asc.
    /// </summary>
    private static List<string> RankWinnersForRound(EventState state, List<string> winnerIds, int round)
    {
        // Compute match wins through round r only (not final standings)
        var winsThrough = ComputeWinsThroughRound(state, round);

        // Create temporary player records with wins-through-round-r for ranking
        var winners = winnerIds
            .Select(id =>
            {
                var player = state.Players[id];
                var mw = winsThrough.GetValueOrDefault(id, 0);
                return player with { MatchWins = mw };
            })
            .ToList();

        return PlayerRanker.Rank(winners)
            .Select(p => p.Id)
            .ToList();
    }

    /// <summary>
    /// Computes match wins for each player counting only rounds 1 through r.
    /// </summary>
    private static Dictionary<string, int> ComputeWinsThroughRound(EventState state, int throughRound)
    {
        var wins = new Dictionary<string, int>();

        for (var r = 1; r <= throughRound; r++)
        {
            foreach (var match in state.GetRoundMatches(r))
            {
                if (match.WinnerId is not null)
                {
                    wins.TryGetValue(match.WinnerId, out var current);
                    wins[match.WinnerId] = current + 1;
                }
            }
        }

        return wins;
    }
}
