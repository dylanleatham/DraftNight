using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class OpponentSwapperTests
{
    [Fact]
    public void Swap_ValidSwap_SwapsPlayers()
    {
        // Arrange - 4 players round-robin: match 0 (p1 vs p4), match 1 (p2 vs p3)
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);
        var match1 = matches[0]; // p1 vs p4
        var match2 = matches[1]; // p2 vs p3

        // Act - swap p4 and p2
        var result = OpponentSwapper.Swap(state, 1, match1.Id, "p4", match2.Id, "p2");

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        var updatedMatches = newState.GetRoundMatches(1);

        // Match 1 should now be p1 vs p2
        var updatedMatch1 = updatedMatches.First(m => m.Id == match1.Id);
        Assert.Equal("p1", updatedMatch1.PlayerAId);
        Assert.Equal("p2", updatedMatch1.PlayerBId);

        // Match 2 should now be p4 vs p3
        var updatedMatch2 = updatedMatches.First(m => m.Id == match2.Id);
        Assert.Equal("p4", updatedMatch2.PlayerAId);
        Assert.Equal("p3", updatedMatch2.PlayerBId);
    }

    [Fact]
    public void Swap_SwapPlayerA_Works()
    {
        // Arrange - 4 players round-robin: match 0 (p1 vs p4), match 1 (p2 vs p3)
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);
        var match1 = matches[0]; // p1 vs p4
        var match2 = matches[1]; // p2 vs p3

        // Act - swap p1 and p2 (both are player A in their matches)
        var result = OpponentSwapper.Swap(state, 1, match1.Id, "p1", match2.Id, "p2");

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        var updatedMatches = newState.GetRoundMatches(1);

        var updatedMatch1 = updatedMatches.First(m => m.Id == match1.Id);
        Assert.Equal("p2", updatedMatch1.PlayerAId);
        Assert.Equal("p4", updatedMatch1.PlayerBId);

        var updatedMatch2 = updatedMatches.First(m => m.Id == match2.Id);
        Assert.Equal("p1", updatedMatch2.PlayerAId);
        Assert.Equal("p3", updatedMatch2.PlayerBId);
    }

    [Fact]
    public void Swap_SameMatch_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);
        var match = state.GetRoundMatches(1)[0];

        // Act
        var result = OpponentSwapper.Swap(state, 1, match.Id, "p1", match.Id, "p4");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<SameMatchSwapError>(error.Error);
    }

    [Fact]
    public void Swap_InvalidRound_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);

        // Act
        var result = OpponentSwapper.Swap(state, 99, matches[0].Id, "p1", matches[1].Id, "p2");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<InvalidRoundNumberError>(error.Error);
    }

    [Fact]
    public void Swap_MatchNotFound_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);
        var match1 = state.GetRoundMatches(1)[0];

        // Act
        var result = OpponentSwapper.Swap(state, 1, match1.Id, "p1", "nonexistent", "p2");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<MatchNotFoundError>(error.Error);
    }

    [Fact]
    public void Swap_FinalizedMatch_ReturnsError()
    {
        // Arrange - round-robin 4 players: match 0 (p1 vs p4), match 1 (p2 vs p3)
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);
        var match1 = matches[0]; // p1 vs p4

        // Finalize match 1
        var finalizeResult = MatchFinalizer.Finalize(state, 1, match1.Id, "p1");
        Assert.True(finalizeResult.IsSuccess);
        state = finalizeResult.GetValueOrThrow();

        // Act - try to swap with finalized match
        var result = OpponentSwapper.Swap(state, 1, match1.Id, "p4", matches[1].Id, "p2");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<MatchNotOpenError>(error.Error);
    }

    [Fact]
    public void Swap_ByeMatch_ReturnsError()
    {
        // Arrange - 5 players (Swiss with BYE)
        var state = CreateStateWithRound1(5);
        var matches = state.GetRoundMatches(1);
        var byeMatch = matches.First(m => m.IsBye);
        var regularMatch = matches.First(m => !m.IsBye);

        // Act
        var result = OpponentSwapper.Swap(
            state,
            1,
            byeMatch.Id,
            byeMatch.PlayerAId,
            regularMatch.Id,
            regularMatch.PlayerAId);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<CannotSwapByeMatchError>(error.Error);
    }

    [Fact]
    public void Swap_PlayerNotInMatch_ReturnsError()
    {
        // Arrange - round-robin 4 players: match 0 (p1 vs p4), match 1 (p2 vs p3)
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);

        // Act - p3 is not in match 0 (match 0 is p1 vs p4)
        var result = OpponentSwapper.Swap(state, 1, matches[0].Id, "p3", matches[1].Id, "p2");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<PlayerNotInMatchError>(error.Error);
    }

    [Fact]
    public void Swap_DroppedPlayer_ReturnsError()
    {
        // Arrange - round-robin 4 players: match 0 (p1 vs p4), match 1 (p2 vs p3)
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);

        // Drop player 4
        var dropResult = PlayerDropper.Drop(state, "p4");
        Assert.True(dropResult.IsSuccess);
        state = dropResult.GetValueOrThrow();

        // Act - try to swap dropped player
        var result = OpponentSwapper.Swap(state, 1, matches[0].Id, "p4", matches[1].Id, "p2");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<PlayerDroppedError>(error.Error);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void Swap_AllEvenPlayerCounts_Works(int playerCount)
    {
        // Arrange
        var state = CreateStateWithRound1(playerCount);
        var matches = state.GetRoundMatches(1).Where(m => !m.IsBye).ToList();

        if (matches.Count < 2)
        {
            return; // Need at least 2 non-BYE matches to swap
        }

        var match1 = matches[0];
        var match2 = matches[1];

        // Act - swap player B from match 1 with player A from match 2
        var result = OpponentSwapper.Swap(
            state,
            1,
            match1.Id,
            match1.PlayerBId!,
            match2.Id,
            match2.PlayerAId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Swap_Determinism_SameInputsSameOutput()
    {
        // Arrange
        var state1 = CreateStateWithRound1(4);
        var state2 = CreateStateWithRound1(4);

        var matches1 = state1.GetRoundMatches(1);
        var matches2 = state2.GetRoundMatches(1);

        // Act - swap p4 (match 0 player B) with p2 (match 1 player A)
        var result1 = OpponentSwapper.Swap(state1, 1, matches1[0].Id, "p4", matches1[1].Id, "p2");
        var result2 = OpponentSwapper.Swap(state2, 1, matches2[0].Id, "p4", matches2[1].Id, "p2");

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);

        var newState1 = result1.GetValueOrThrow();
        var newState2 = result2.GetValueOrThrow();

        var newMatches1 = newState1.GetRoundMatches(1);
        var newMatches2 = newState2.GetRoundMatches(1);

        for (int i = 0; i < newMatches1.Count; i++)
        {
            Assert.Equal(newMatches1[i].PlayerAId, newMatches2[i].PlayerAId);
            Assert.Equal(newMatches1[i].PlayerBId, newMatches2[i].PlayerBId);
        }
    }

    private static EventState CreateStateWithRound1(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
        return PairingGenerator.GenerateRound(state, 1).GetValueOrThrow();
    }
}
