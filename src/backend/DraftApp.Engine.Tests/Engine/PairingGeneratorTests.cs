using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class PairingGeneratorTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void GenerateRound_RoundRobin_DelegatesToScheduler(int playerCount)
    {
        // Arrange
        var state = CreateState(playerCount);

        // Act
        var result = PairingGenerator.GenerateRound(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.True(newState.MatchesByRound.ContainsKey(1));
        Assert.Equal(TournamentFormat.RoundRobin, newState.Format);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void GenerateRound_Swiss_DelegatesToPairer(int playerCount)
    {
        // Arrange
        var state = CreateState(playerCount);

        // Act
        var result = PairingGenerator.GenerateRound(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.True(newState.MatchesByRound.ContainsKey(1));
        Assert.Equal(TournamentFormat.Swiss, newState.Format);
    }

    [Fact]
    public void GenerateRound_InvalidRound_ReturnsError()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var result = PairingGenerator.GenerateRound(state, 0);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<InvalidRoundNumberError>(error.Error);
    }

    [Fact]
    public void GenerateRound_ExceedsMaxRound_ReturnsError()
    {
        // Arrange
        var state = CreateState(4); // RR with 3 rounds

        // Act
        var result = PairingGenerator.GenerateRound(state, 10);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<InvalidRoundNumberError>(error.Error);
    }

    [Fact]
    public void GenerateRound_AlreadyGenerated_ReturnsError()
    {
        // Arrange
        var state = CreateState(4);
        var firstResult = PairingGenerator.GenerateRound(state, 1);
        Assert.True(firstResult.IsSuccess);

        // Act - try to generate round 1 again
        var result = PairingGenerator.GenerateRound(firstResult.GetValueOrThrow(), 1);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<RoundAlreadyGeneratedError>(error.Error);
    }

    [Fact]
    public void GenerateRound_PreviousIncomplete_ReturnsError()
    {
        // Arrange
        var state = CreateState(4);
        var round1Result = PairingGenerator.GenerateRound(state, 1);
        Assert.True(round1Result.IsSuccess);

        // Don't finalize any matches

        // Act - try to generate round 2
        var result = PairingGenerator.GenerateRound(round1Result.GetValueOrThrow(), 2);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<PreviousRoundIncompleteError>(error.Error);
    }

    [Fact]
    public void GenerateRound_PreviousComplete_Succeeds()
    {
        // Arrange
        var state = CreateState(4);
        state = PairingGenerator.GenerateRound(state, 1).GetValueOrThrow();
        state = FinalizeAllMatches(state, 1);

        // Act
        var result = PairingGenerator.GenerateRound(state, 2);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.True(newState.MatchesByRound.ContainsKey(2));
    }

    [Fact]
    public void GenerateRound_SwissBye_UpdatesPlayerState()
    {
        // Arrange - 5 players, Swiss format
        var state = CreateState(5);

        // Act
        var result = PairingGenerator.GenerateRound(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Find the BYE match
        var byeMatch = newState.GetRoundMatches(1).First(m => m.IsBye);
        var byePlayer = newState.Players[byeMatch.PlayerAId];

        // BYE player should have win + byeReceived
        Assert.Equal(1, byePlayer.MatchWins);
        Assert.True(byePlayer.ByeReceived);
    }

    [Fact]
    public void GenerateRound_RoundRobinSit_DoesNotAwardWin()
    {
        // Arrange - 3 players, RR format (will have sits)
        var state = CreateState(3);

        // Act
        var result = PairingGenerator.GenerateRound(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Find the sit match
        var sitMatch = newState.GetRoundMatches(1).FirstOrDefault(m => m.IsBye);
        if (sitMatch is not null)
        {
            var sitPlayer = newState.Players[sitMatch.PlayerAId];

            // Sit player should NOT have a win
            Assert.Equal(0, sitPlayer.MatchWins);
            Assert.False(sitPlayer.ByeReceived); // RR uses sits, not BYEs
        }
    }

    private static EventState CreateState(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        return EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
    }

    private static EventState FinalizeAllMatches(EventState state, int round)
    {
        foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
        {
            var winnerId = match.PlayerAId; // PlayerA always wins for simplicity
            var result = MatchFinalizer.Finalize(state, round, match.Id, winnerId);
            state = result.GetValueOrThrow();
        }

        return state;
    }
}
