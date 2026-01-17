using System.Collections.Immutable;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Orchestrates pairing generation, delegating to the appropriate scheduler.
/// </summary>
internal static class PairingGenerator
{
    /// <summary>
    /// Generates pairings for a specific round.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number to generate pairings for.</param>
    /// <returns>Updated event state with pairings or an error.</returns>
    public static EngineResult<EventState> GenerateRound(EventState state, int roundNumber)
    {
        // Validate round number
        if (roundNumber < 1 || roundNumber > state.TotalRounds)
        {
            return EngineResult<EventState>.Fail(
                new InvalidRoundNumberError(roundNumber, state.TotalRounds));
        }

        // Check if round already generated
        if (state.MatchesByRound.ContainsKey(roundNumber))
        {
            return EngineResult<EventState>.Fail(
                new RoundAlreadyGeneratedError(roundNumber));
        }

        // Check if previous round is complete (except for round 1)
        if (roundNumber > 1 && !state.IsRoundComplete(roundNumber - 1))
        {
            return EngineResult<EventState>.Fail(
                new PreviousRoundIncompleteError(roundNumber - 1));
        }

        // Generate pairings based on format
        ImmutableList<Match> matches;
        EventState updatedState = state;

        if (state.Format == TournamentFormat.RoundRobin)
        {
            matches = RoundRobinScheduler.GenerateRound(state, roundNumber);
        }
        else
        {
            matches = SwissPairer.GenerateRound(state, roundNumber);

            // For Swiss BYE matches, the player state needs to be updated
            foreach (var match in matches.Where(m => m.IsBye))
            {
                var player = updatedState.Players[match.PlayerAId];
                updatedState = updatedState.WithPlayer(
                    player.WithWin().WithByeReceived());
            }
        }

        // Add matches to state
        updatedState = updatedState.WithRoundMatches(roundNumber, matches);

        return EngineResult<EventState>.Ok(updatedState);
    }
}
