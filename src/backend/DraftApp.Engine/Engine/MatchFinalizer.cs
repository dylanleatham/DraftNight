using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles match result finalization.
/// </summary>
internal static class MatchFinalizer
{
    /// <summary>
    /// Finalizes a match result and updates player states.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="winnerId">Winner's player ID.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> Finalize(
        EventState state,
        int roundNumber,
        string matchId,
        string winnerId)
    {
        // Validate round number
        if (roundNumber < 1 || roundNumber > state.TotalRounds)
        {
            return EngineResult<EventState>.Fail(
                new InvalidRoundNumberError(roundNumber, state.TotalRounds));
        }

        // Find the match
        var roundMatches = state.GetRoundMatches(roundNumber);
        var match = roundMatches.FirstOrDefault(m => m.Id == matchId);

        if (match is null)
        {
            return EngineResult<EventState>.Fail(
                new MatchNotFoundError(roundNumber, matchId));
        }

        // Check if it's a BYE match
        if (match.IsBye)
        {
            return EngineResult<EventState>.Fail(
                new CannotFinalizeBYEMatchError(matchId));
        }

        // Check if already finalized
        if (match.IsComplete)
        {
            return EngineResult<EventState>.Fail(
                new MatchAlreadyFinalizedError(matchId));
        }

        // Validate winner is a participant
        if (winnerId != match.PlayerAId && winnerId != match.PlayerBId)
        {
            return EngineResult<EventState>.Fail(
                new InvalidWinnerError(matchId, winnerId));
        }

        // Determine loser
        var loserId = winnerId == match.PlayerAId ? match.PlayerBId! : match.PlayerAId;

        // Update match
        var updatedMatch = match.WithWinner(winnerId);
        state = state.WithMatch(roundNumber, updatedMatch);

        // Update winner
        var winner = state.Players[winnerId];
        winner = winner.WithWin().WithOpponent(loserId, roundNumber);
        state = state.WithPlayer(winner);

        // Update loser
        var loser = state.Players[loserId];
        loser = loser.WithLoss().WithOpponent(winnerId, roundNumber);
        state = state.WithPlayer(loser);

        return EngineResult<EventState>.Ok(state);
    }
}
