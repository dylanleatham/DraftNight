using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles reopening finalized matches for corrections.
/// </summary>
internal static class MatchReopener
{
    /// <summary>
    /// Reopens a finalized match, reversing the result.
    /// </summary>
    public static EngineResult<EventState> Reopen(EventState state, int roundNumber, string matchId)
    {
        // Validate round number
        if (roundNumber < 1 || roundNumber > state.TotalRounds)
        {
            return EngineResult<EventState>.Fail(
                new InvalidRoundNumberError(roundNumber, state.TotalRounds));
        }

        // Find the match
        var roundMatches = state.GetRoundMatches(roundNumber);
        var matchIndex = roundMatches.FindIndex(m => m.Id == matchId);

        if (matchIndex < 0)
        {
            return EngineResult<EventState>.Fail(
                new MatchNotFoundError(roundNumber, matchId));
        }

        var match = roundMatches[matchIndex];

        // Check if the match is finalized
        if (!match.IsComplete)
        {
            return EngineResult<EventState>.Fail(
                new MatchNotFinalizedError(matchId));
        }

        // BYE matches cannot be reopened
        if (match.IsBye)
        {
            return EngineResult<EventState>.Fail(
                new CannotReopenByeError());
        }

        var winnerId = match.WinnerId!;
        var loserId = match.PlayerAId == winnerId ? match.PlayerBId! : match.PlayerAId;

        // Reverse the match result stats
        var winner = state.Players[winnerId];
        var loser = state.Players[loserId];

        var updatedWinner = winner
            .WithMatchWinsDecreased()
            .WithOpponentRemoved(loserId, roundNumber);

        var updatedLoser = loser
            .WithMatchLossesDecreased()
            .WithOpponentRemoved(winnerId, roundNumber);

        var updatedPlayers = state.Players
            .SetItem(winnerId, updatedWinner)
            .SetItem(loserId, updatedLoser);

        // Clear the winner from the match
        var updatedMatch = match.ClearWinner();
        var updatedRoundMatches = roundMatches.SetItem(matchIndex, updatedMatch);
        var updatedMatchesByRound = state.MatchesByRound.SetItem(roundNumber, updatedRoundMatches);

        return EngineResult<EventState>.Ok(state with
        {
            Players = updatedPlayers,
            MatchesByRound = updatedMatchesByRound
        });
    }
}
