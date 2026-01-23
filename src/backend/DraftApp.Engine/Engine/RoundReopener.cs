using System.Collections.Immutable;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles reopening all finalized matches in a round.
/// </summary>
internal static class RoundReopener
{
    /// <summary>
    /// Reopens all finalized matches in a round, reversing their results.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number to reopen.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> Reopen(EventState state, int roundNumber)
    {
        // Validate round exists
        if (!state.MatchesByRound.ContainsKey(roundNumber))
        {
            return EngineResult<EventState>.Fail(new RoundNotFoundError(roundNumber));
        }

        // Validate this is the current round (no subsequent rounds)
        if (state.CurrentRound > roundNumber)
        {
            return EngineResult<EventState>.Fail(new SubsequentRoundsExistError(roundNumber));
        }

        var roundMatches = state.GetRoundMatches(roundNumber);

        // Find all finalized non-BYE matches
        var finalizedMatches = roundMatches.Where(m => m.IsComplete && !m.IsBye).ToList();

        if (finalizedMatches.Count == 0)
        {
            return EngineResult<EventState>.Fail(new NoFinalizedMatchesError(roundNumber));
        }

        var updatedPlayers = state.Players;
        var updatedMatches = roundMatches;

        foreach (var match in finalizedMatches)
        {
            var winnerId = match.WinnerId!;
            var loserId = match.PlayerAId == winnerId ? match.PlayerBId! : match.PlayerAId;

            // Reverse the match result stats
            var winner = updatedPlayers[winnerId];
            var loser = updatedPlayers[loserId];

            var updatedWinner = winner
                .WithMatchWinsDecreased()
                .WithOpponentRemoved(loserId, roundNumber);

            var updatedLoser = loser
                .WithMatchLossesDecreased()
                .WithOpponentRemoved(winnerId, roundNumber);

            updatedPlayers = updatedPlayers
                .SetItem(winnerId, updatedWinner)
                .SetItem(loserId, updatedLoser);

            // Clear the winner from the match
            var matchIndex = updatedMatches.FindIndex(m => m.Id == match.Id);
            var clearedMatch = match.ClearWinner();
            updatedMatches = updatedMatches.SetItem(matchIndex, clearedMatch);
        }

        var updatedMatchesByRound = state.MatchesByRound.SetItem(roundNumber, updatedMatches);

        return EngineResult<EventState>.Ok(state with
        {
            Players = updatedPlayers,
            MatchesByRound = updatedMatchesByRound
        });
    }
}
