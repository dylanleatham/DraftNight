using System.Collections.Immutable;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles regenerating pairings for the current round.
/// </summary>
internal static class PairingRegenerator
{
    /// <summary>
    /// Regenerates pairings for the current round, deleting existing pairings
    /// and creating new ones.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number to regenerate.</param>
    /// <returns>Updated event state with new pairings or an error.</returns>
    public static EngineResult<EventState> Regenerate(EventState state, int roundNumber)
    {
        // Validate round exists
        if (!state.MatchesByRound.ContainsKey(roundNumber))
        {
            return EngineResult<EventState>.Fail(new RoundNotFoundError(roundNumber));
        }

        // Validate this is the current round
        if (state.CurrentRound != roundNumber)
        {
            return EngineResult<EventState>.Fail(
                new CanOnlyRegenerateCurrentRoundError(roundNumber, state.CurrentRound));
        }

        var roundMatches = state.GetRoundMatches(roundNumber);

        // Check no non-BYE matches are finalized
        var hasFinalizedNonBye = roundMatches.Any(m => m.IsComplete && !m.IsBye);
        if (hasFinalizedNonBye)
        {
            return EngineResult<EventState>.Fail(
                new CannotRegenerateWithFinalizedMatchesError(roundNumber));
        }

        // If Swiss with BYE, reverse the BYE player's stats
        var updatedPlayers = state.Players;
        var byeMatches = roundMatches.Where(m => m.IsBye && m.IsComplete).ToList();

        foreach (var byeMatch in byeMatches)
        {
            var byePlayer = updatedPlayers[byeMatch.PlayerAId];
            var updatedByePlayer = byePlayer
                .WithMatchWinsDecreased()
                .WithByeCleared();
            updatedPlayers = updatedPlayers.SetItem(byePlayer.Id, updatedByePlayer);
        }

        // Remove the round from MatchesByRound
        var updatedMatchesByRound = state.MatchesByRound.Remove(roundNumber);

        var stateWithoutRound = state with
        {
            Players = updatedPlayers,
            MatchesByRound = updatedMatchesByRound
        };

        // Generate new pairings using the standard pairing generator
        return PairingGenerator.GenerateRound(stateWithoutRound, roundNumber);
    }
}
