using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class MatchFinalizerTests
{
    [Fact]
    public void Finalize_ValidMatch_UpdatesPlayerStates()
    {
        // Arrange
        var state = CreateStateWithRound1();
        var match = state.GetRoundMatches(1)[0];
        var winnerId = match.PlayerAId;
        var loserId = match.PlayerBId!;

        // Act
        var result = MatchFinalizer.Finalize(state, 1, match.Id, winnerId);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Winner updated
        var winner = newState.Players[winnerId];
        Assert.Equal(1, winner.MatchWins);
        Assert.Equal(0, winner.MatchLosses);
        Assert.Contains(loserId, winner.Opponents);
        Assert.Equal(1, winner.LastPlayedRound[loserId]);

        // Loser updated
        var loser = newState.Players[loserId];
        Assert.Equal(0, loser.MatchWins);
        Assert.Equal(1, loser.MatchLosses);
        Assert.Contains(winnerId, loser.Opponents);
        Assert.Equal(1, loser.LastPlayedRound[winnerId]);

        // Match updated
        var updatedMatch = newState.GetRoundMatches(1)[0];
        Assert.Equal(winnerId, updatedMatch.WinnerId);
    }

    [Fact]
    public void Finalize_PlayerB_CanWin()
    {
        // Arrange
        var state = CreateStateWithRound1();
        var match = state.GetRoundMatches(1)[0];
        var playerAId = match.PlayerAId;
        var playerBId = match.PlayerBId!;

        // Act - player B wins
        var result = MatchFinalizer.Finalize(state, 1, match.Id, playerBId);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        Assert.Equal(1, newState.Players[playerBId].MatchWins);
        Assert.Equal(1, newState.Players[playerAId].MatchLosses);
    }

    [Fact]
    public void Finalize_InvalidRound_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1();

        // Act
        var result = MatchFinalizer.Finalize(state, 99, "r1-m0", "p1");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<InvalidRoundNumberError>(error.Error);
    }

    [Fact]
    public void Finalize_MatchNotFound_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1();

        // Act
        var result = MatchFinalizer.Finalize(state, 1, "nonexistent", "p1");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<MatchNotFoundError>(error.Error);
    }

    [Fact]
    public void Finalize_AlreadyFinalized_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1();
        var match = state.GetRoundMatches(1)[0];

        // Finalize once
        var firstResult = MatchFinalizer.Finalize(state, 1, match.Id, "p1");
        Assert.True(firstResult.IsSuccess);

        // Act - try to finalize again
        var result = MatchFinalizer.Finalize(firstResult.GetValueOrThrow(), 1, match.Id, "p2");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<MatchAlreadyFinalizedError>(error.Error);
    }

    [Fact]
    public void Finalize_InvalidWinner_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1();
        var match = state.GetRoundMatches(1)[0]; // p1 vs p2

        // Act - try to set p3 as winner (not in match)
        var result = MatchFinalizer.Finalize(state, 1, match.Id, "p3");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<InvalidWinnerError>(error.Error);
    }

    [Fact]
    public void Finalize_ByeMatch_ReturnsError()
    {
        // Arrange - 5 players (Swiss with BYE)
        var state = CreateSwissStateWithRound1(5);
        var byeMatch = state.GetRoundMatches(1).First(m => m.IsBye);

        // Act
        var result = MatchFinalizer.Finalize(state, 1, byeMatch.Id, byeMatch.PlayerAId);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<CannotFinalizeBYEMatchError>(error.Error);
    }

    [Fact]
    public void Finalize_MultipleMatches_IndependentUpdates()
    {
        // Arrange
        var state = CreateSwissStateWithRound1(8);
        var matches = state.GetRoundMatches(1);

        // Act - finalize all matches
        foreach (var match in matches)
        {
            var winnerId = match.PlayerAId; // First player wins all
            var result = MatchFinalizer.Finalize(state, 1, match.Id, winnerId);
            Assert.True(result.IsSuccess);
            state = result.GetValueOrThrow();
        }

        // Assert
        Assert.Equal(1, state.Players["p1"].MatchWins);
        Assert.Equal(1, state.Players["p3"].MatchWins);
        Assert.Equal(1, state.Players["p5"].MatchWins);
        Assert.Equal(1, state.Players["p7"].MatchWins);

        Assert.Equal(1, state.Players["p2"].MatchLosses);
        Assert.Equal(1, state.Players["p4"].MatchLosses);
        Assert.Equal(1, state.Players["p6"].MatchLosses);
        Assert.Equal(1, state.Players["p8"].MatchLosses);
    }

    private static EventState CreateStateWithRound1()
    {
        var players = Enumerable.Range(1, 4)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
        return PairingGenerator.GenerateRound(state, 1).GetValueOrThrow();
    }

    private static EventState CreateSwissStateWithRound1(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
        return PairingGenerator.GenerateRound(state, 1).GetValueOrThrow();
    }
}
