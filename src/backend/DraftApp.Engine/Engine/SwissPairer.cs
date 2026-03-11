using System.Collections.Immutable;
using DraftApp.Engine.Helpers;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Generates Swiss pairings with repeat-avoidance.
/// </summary>
internal static class SwissPairer
{
    /// <summary>
    /// Generates pairings for a Swiss round.
    /// </summary>
    /// <param name="state">Current event state with updated player standings.</param>
    /// <param name="roundNumber">Round number (1, 2, or 3).</param>
    /// <returns>List of matches for the round.</returns>
    public static ImmutableList<Match> GenerateRound(EventState state, int roundNumber)
    {
        var activePlayers = state.GetActivePlayers().ToList();

        if (roundNumber == 1)
        {
            return GenerateRound1(activePlayers, roundNumber);
        }

        return GenerateLaterRound(state, activePlayers, roundNumber);
    }

    /// <summary>
    /// Round 1: Pair adjacent seeds (1v2, 3v4, etc.).
    /// </summary>
    private static ImmutableList<Match> GenerateRound1(
        List<Player> players,
        int roundNumber)
    {
        var sorted = PlayerRanker.BySeed(players);
        var matches = ImmutableList.CreateBuilder<Match>();
        var matchIndex = 0;

        for (var i = 0; i < sorted.Count - 1; i += 2)
        {
            var matchId = $"r{roundNumber}-m{matchIndex}";
            matches.Add(Match.Create(matchId, roundNumber, sorted[i].Id, sorted[i + 1].Id));
            matchIndex++;
        }

        // Odd player gets BYE (last unpaired player)
        if (sorted.Count % 2 != 0)
        {
            var byePlayer = sorted[^1];
            var matchId = $"r{roundNumber}-m{matchIndex}";
            matches.Add(Match.CreateBye(matchId, roundNumber, byePlayer.Id));
        }

        return matches.ToImmutable();
    }

    /// <summary>
    /// Rounds 2+: Rank players, assign BYE if needed, then greedy pairing.
    /// </summary>
    private static ImmutableList<Match> GenerateLaterRound(
        EventState state,
        List<Player> activePlayers,
        int roundNumber)
    {
        // Step A: Rank players by MW desc, seed asc, id asc
        var ranked = PlayerRanker.Rank(activePlayers).ToList();
        var matches = ImmutableList.CreateBuilder<Match>();
        var matchIndex = 0;
        var paired = new HashSet<string>();

        // Step B: Assign BYE if N is odd
        if (ranked.Count % 2 != 0)
        {
            var byeRecipient = SelectByeRecipient(ranked, state);
            var matchId = $"r{roundNumber}-m{matchIndex}";
            matches.Add(Match.CreateBye(matchId, roundNumber, byeRecipient.Id));
            paired.Add(byeRecipient.Id);
            matchIndex++;
        }

        // Step C: Greedy pairing with repeat avoidance
        var unpaired = ranked.Where(p => !paired.Contains(p.Id)).ToList();

        while (unpaired.Count >= 2)
        {
            var player = unpaired[0];
            unpaired.RemoveAt(0);

            var opponent = SelectOpponent(player, unpaired, state, roundNumber);
            unpaired.Remove(opponent);

            var matchId = $"r{roundNumber}-m{matchIndex}";
            matches.Add(Match.Create(matchId, roundNumber, player.Id, opponent.Id));
            matchIndex++;
        }

        return matches.ToImmutable();
    }

    /// <summary>
    /// Selects the BYE recipient: lowest-ranked player without prior BYE,
    /// or absolute lowest-ranked if all have received BYE.
    /// </summary>
    private static Player SelectByeRecipient(List<Player> ranked, EventState state)
    {
        // Try to find lowest-ranked player who hasn't had a BYE
        for (var i = ranked.Count - 1; i >= 0; i--)
        {
            if (!ranked[i].ByeReceived)
            {
                return ranked[i];
            }
        }

        // All have received BYE, return absolute lowest-ranked
        return ranked[^1];
    }

    /// <summary>
    /// Selects the best opponent for a player using the pairing criteria.
    /// </summary>
    private static Player SelectOpponent(
        Player player,
        List<Player> candidates,
        EventState state,
        int roundNumber)
    {
        var currentPlayer = state.Players[player.Id];
        var playerOpponents = currentPlayer.Opponents.ToHashSet();

        // Separate into non-repeat and repeat candidates
        var nonRepeats = candidates.Where(c => !playerOpponents.Contains(c.Id)).ToList();
        var useNonRepeats = nonRepeats.Count > 0;
        var searchCandidates = useNonRepeats ? nonRepeats : candidates;

        // Score each candidate
        // RankIndex is computed as the candidate's position within the full unpaired pool (candidates),
        // not within the restricted searchCandidates subset (spec §8.2.2 Step C.3.2).
        var scored = searchCandidates
            .Select(c => new
            {
                Candidate = c,
                RankIndex = candidates.IndexOf(c) + 1,
                RecordGap = Math.Abs(player.MatchWins - c.MatchWins),
                RepeatAge = GetRepeatAge(currentPlayer, c.Id, roundNumber),
            });

        // For non-repeat candidates: sort by recordGap, rankIndex, seed, id (no repeatAge).
        // For repeat candidates (fallback): include repeatAge to prefer least-recent repeats.
        var bestCandidate = useNonRepeats
            ? scored
                .OrderBy(x => x.RecordGap)
                .ThenBy(x => x.RankIndex)
                .ThenBy(x => x.Candidate.Seed)
                .ThenBy(x => x.Candidate.Id)
                .First()
            : scored
                .OrderBy(x => x.RecordGap)
                .ThenBy(x => x.RankIndex)
                .ThenByDescending(x => x.RepeatAge)
                .ThenBy(x => x.Candidate.Seed)
                .ThenBy(x => x.Candidate.Id)
                .First();

        return bestCandidate.Candidate;
    }

    /// <summary>
    /// Gets the "age" of a repeat pairing (rounds since last played).
    /// Returns 0 if not a repeat, larger values mean older repeat.
    /// </summary>
    private static int GetRepeatAge(Player player, string opponentId, int currentRound)
    {
        if (!player.LastPlayedRound.TryGetValue(opponentId, out var lastRound))
        {
            return 0;
        }

        return currentRound - lastRound;
    }
}
