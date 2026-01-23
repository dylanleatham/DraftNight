using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Engine;

/// <summary>
/// Handles swapping opponents between two matches in the same round.
/// </summary>
internal static class OpponentSwapper
{
    /// <summary>
    /// Swaps two players between their respective matches.
    /// </summary>
    /// <param name="state">Current event state.</param>
    /// <param name="roundNumber">Round number containing both matches.</param>
    /// <param name="matchId1">First match ID.</param>
    /// <param name="playerId1">Player to move from match 1 to match 2.</param>
    /// <param name="matchId2">Second match ID.</param>
    /// <param name="playerId2">Player to move from match 2 to match 1.</param>
    /// <returns>Updated event state or an error.</returns>
    public static EngineResult<EventState> Swap(
        EventState state,
        int roundNumber,
        string matchId1,
        string playerId1,
        string matchId2,
        string playerId2)
    {
        // Validate match IDs are different
        if (matchId1 == matchId2)
        {
            return EngineResult<EventState>.Fail(new SameMatchSwapError());
        }

        // Validate round number
        if (roundNumber < 1 || roundNumber > state.TotalRounds)
        {
            return EngineResult<EventState>.Fail(
                new InvalidRoundNumberError(roundNumber, state.TotalRounds));
        }

        // Get round matches
        var roundMatches = state.GetRoundMatches(roundNumber);
        if (roundMatches.Count == 0)
        {
            return EngineResult<EventState>.Fail(new RoundNotFoundError(roundNumber));
        }

        // Find match 1
        var match1Index = roundMatches.FindIndex(m => m.Id == matchId1);
        if (match1Index < 0)
        {
            return EngineResult<EventState>.Fail(new MatchNotFoundError(roundNumber, matchId1));
        }

        // Find match 2
        var match2Index = roundMatches.FindIndex(m => m.Id == matchId2);
        if (match2Index < 0)
        {
            return EngineResult<EventState>.Fail(new MatchNotFoundError(roundNumber, matchId2));
        }

        var match1 = roundMatches[match1Index];
        var match2 = roundMatches[match2Index];

        // Check neither match is a BYE or sit (check before finalized since BYEs are auto-finalized)
        if (match1.IsBye)
        {
            return EngineResult<EventState>.Fail(new CannotSwapByeMatchError(matchId1));
        }

        if (match2.IsBye)
        {
            return EngineResult<EventState>.Fail(new CannotSwapByeMatchError(matchId2));
        }

        // Check neither match is finalized
        if (match1.IsComplete)
        {
            return EngineResult<EventState>.Fail(new MatchNotOpenError(matchId1));
        }

        if (match2.IsComplete)
        {
            return EngineResult<EventState>.Fail(new MatchNotOpenError(matchId2));
        }

        // Validate player 1 is in match 1
        bool player1IsA = match1.PlayerAId == playerId1;
        bool player1IsB = match1.PlayerBId == playerId1;
        if (!player1IsA && !player1IsB)
        {
            return EngineResult<EventState>.Fail(new PlayerNotInMatchError(playerId1, matchId1));
        }

        // Validate player 2 is in match 2
        bool player2IsA = match2.PlayerAId == playerId2;
        bool player2IsB = match2.PlayerBId == playerId2;
        if (!player2IsA && !player2IsB)
        {
            return EngineResult<EventState>.Fail(new PlayerNotInMatchError(playerId2, matchId2));
        }

        // Check players are not dropped
        var player1 = state.Players[playerId1];
        if (player1.IsDropped)
        {
            return EngineResult<EventState>.Fail(new PlayerDroppedError(playerId1));
        }

        var player2 = state.Players[playerId2];
        if (player2.IsDropped)
        {
            return EngineResult<EventState>.Fail(new PlayerDroppedError(playerId2));
        }

        // Perform the swap:
        // - Player 1 moves from match 1 to match 2 (takes player 2's slot)
        // - Player 2 moves from match 2 to match 1 (takes player 1's slot)
        var updatedMatch1 = player1IsA
            ? match1.WithPlayerA(playerId2)
            : match1.WithPlayerB(playerId2);

        var updatedMatch2 = player2IsA
            ? match2.WithPlayerA(playerId1)
            : match2.WithPlayerB(playerId1);

        // Update round matches
        var updatedRoundMatches = roundMatches
            .SetItem(match1Index, updatedMatch1)
            .SetItem(match2Index, updatedMatch2);

        var updatedMatchesByRound = state.MatchesByRound.SetItem(roundNumber, updatedRoundMatches);

        return EngineResult<EventState>.Ok(state with
        {
            MatchesByRound = updatedMatchesByRound
        });
    }
}
