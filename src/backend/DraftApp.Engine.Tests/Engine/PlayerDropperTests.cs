using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class PlayerDropperTests
{
    [Fact]
    public void Drop_ValidPlayer_MarksAsDropped()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var result = PlayerDropper.Drop(state, "p1");

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.True(newState.Players["p1"].IsDropped);
        Assert.False(newState.Players["p2"].IsDropped);
    }

    [Fact]
    public void Drop_PlayerNotFound_ReturnsError()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var result = PlayerDropper.Drop(state, "nonexistent");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<PlayerNotFoundError>(error.Error);
    }

    [Fact]
    public void Drop_AlreadyDropped_ReturnsError()
    {
        // Arrange
        var state = CreateState(4);
        var firstDrop = PlayerDropper.Drop(state, "p1");
        Assert.True(firstDrop.IsSuccess);

        // Act
        var result = PlayerDropper.Drop(firstDrop.GetValueOrThrow(), "p1");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<PlayerAlreadyDroppedError>(error.Error);
    }

    [Fact]
    public void Drop_TournamentComplete_ReturnsError()
    {
        // Arrange - complete a 2-player tournament
        var state = CreateState(2);
        state = CompleteAllRounds(state);

        // Act
        var result = PlayerDropper.Drop(state, "p1");

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<TournamentCompleteError>(error.Error);
    }

    [Fact]
    public void Drop_MultiplePlayers_IndependentDrops()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        state = PlayerDropper.Drop(state, "p1").GetValueOrThrow();
        state = PlayerDropper.Drop(state, "p2").GetValueOrThrow();

        // Assert
        Assert.True(state.Players["p1"].IsDropped);
        Assert.True(state.Players["p2"].IsDropped);
        Assert.False(state.Players["p3"].IsDropped);
        Assert.False(state.Players["p4"].IsDropped);
    }

    [Fact]
    public void Drop_PreservesOtherPlayerState()
    {
        // Arrange
        var state = CreateState(4);
        state = PairingGenerator.GenerateRound(state, 1).GetValueOrThrow();

        // Finalize a match to give playerA a win
        var match = state.GetRoundMatches(1)[0];
        var winnerId = match.PlayerAId;
        var loserId = match.PlayerBId!;
        state = MatchFinalizer.Finalize(state, 1, match.Id, winnerId).GetValueOrThrow();

        // Find a player not in the first match to drop
        var playerToDrop = state.Players.Keys.First(p => p != winnerId && p != loserId);

        // Act
        state = PlayerDropper.Drop(state, playerToDrop).GetValueOrThrow();

        // Assert - winner and loser stats should be unchanged
        Assert.Equal(1, state.Players[winnerId].MatchWins);
        Assert.Equal(1, state.Players[loserId].MatchLosses);
    }

    private static EventState CreateState(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        return EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
    }

    private static EventState CompleteAllRounds(EventState state)
    {
        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = PairingGenerator.GenerateRound(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                state = MatchFinalizer.Finalize(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }
        }

        return state;
    }
}
