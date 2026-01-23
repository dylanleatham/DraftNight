using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class PairingRegeneratorTests
{
    [Fact]
    public void Regenerate_NoFinalizedMatches_GeneratesNewPairings()
    {
        // Arrange
        var state = CreateStateWithRound1(4);
        var originalMatches = state.GetRoundMatches(1);

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        var newMatches = newState.GetRoundMatches(1);

        // Should still have same number of matches
        Assert.Equal(originalMatches.Count, newMatches.Count);

        // Match IDs are deterministic based on round number
        // So they will be the same format (r1-m0, r1-m1, etc.)
        Assert.True(newMatches.All(m => m.Id.StartsWith("r1-m")));
    }

    [Fact]
    public void Regenerate_SwissWithBye_ReversesByePlayerStats()
    {
        // Arrange - 5 players (Swiss with BYE)
        var state = CreateStateWithRound1(5);
        var byeMatch = state.GetRoundMatches(1).First(m => m.IsBye);
        var byePlayerId = byeMatch.PlayerAId;

        // BYE player starts with 1 win and byeReceived = true
        Assert.Equal(1, state.Players[byePlayerId].MatchWins);
        Assert.True(state.Players[byePlayerId].ByeReceived);

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // After regeneration, there should be a new BYE player
        var newByeMatch = newState.GetRoundMatches(1).First(m => m.IsBye);

        // The new BYE player should have 1 win and byeReceived = true
        var newByePlayerId = newByeMatch.PlayerAId;
        Assert.Equal(1, newState.Players[newByePlayerId].MatchWins);
        Assert.True(newState.Players[newByePlayerId].ByeReceived);
    }

    [Fact]
    public void Regenerate_NotCurrentRound_ReturnsError()
    {
        // Arrange - Generate round 1 and 2
        var state = CreateStateWithRound1(4);
        var round1Matches = state.GetRoundMatches(1);

        // Finalize all round 1 matches
        foreach (var match in round1Matches)
        {
            var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, match.PlayerAId);
            Assert.True(finalizeResult.IsSuccess);
            state = finalizeResult.GetValueOrThrow();
        }

        // Generate round 2
        var round2Result = PairingGenerator.GenerateRound(state, 2);
        Assert.True(round2Result.IsSuccess);
        state = round2Result.GetValueOrThrow();

        // Act - try to regenerate round 1 (not current)
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<CanOnlyRegenerateCurrentRoundError>(error.Error);
    }

    [Fact]
    public void Regenerate_RoundNotFound_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);

        // Act - try to regenerate a round that doesn't exist
        var result = PairingRegenerator.Regenerate(state, 2);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<RoundNotFoundError>(error.Error);
    }

    [Fact]
    public void Regenerate_FinalizedNonByeMatch_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);
        var match = state.GetRoundMatches(1)[0];

        // Finalize one match
        var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, match.PlayerAId);
        Assert.True(finalizeResult.IsSuccess);
        state = finalizeResult.GetValueOrThrow();

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<CannotRegenerateWithFinalizedMatchesError>(error.Error);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Regenerate_AllPlayerCounts_Works(int playerCount)
    {
        // Arrange
        var state = CreateStateWithRound1(playerCount);

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.NotEmpty(newState.GetRoundMatches(1));
    }

    [Fact]
    public void Regenerate_RoundRobinWithSit_PreservesFormat()
    {
        // Arrange - 3 players (Round-robin with sit)
        var state = CreateStateWithRound1(3);
        var originalFormat = state.Format;
        var originalTotalRounds = state.TotalRounds;

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        Assert.Equal(originalFormat, newState.Format);
        Assert.Equal(originalTotalRounds, newState.TotalRounds);
    }

    [Fact]
    public void Regenerate_MaintainsPlayerCount()
    {
        // Arrange
        var state = CreateStateWithRound1(6);
        var originalPlayerCount = state.PlayerCount;

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.Equal(originalPlayerCount, newState.PlayerCount);
    }

    [Fact]
    public void Regenerate_SwissWithBye_ByePlayerHasCorrectStats()
    {
        // Arrange - 5 players (Swiss with BYE)
        var state = CreateStateWithRound1(5);

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        var newByeMatch = newState.GetRoundMatches(1).First(m => m.IsBye);

        // A BYE match should exist
        Assert.NotNull(newByeMatch);

        // BYE player should have byeReceived = true and 1 win
        Assert.True(newState.Players[newByeMatch.PlayerAId].ByeReceived);
        Assert.Equal(1, newState.Players[newByeMatch.PlayerAId].MatchWins);
    }

    [Fact]
    public void Regenerate_Determinism_SameInputsAfterRegenerate()
    {
        // Arrange
        var state1 = CreateStateWithRound1(4);
        var state2 = CreateStateWithRound1(4);

        // Act
        var result1 = PairingRegenerator.Regenerate(state1, 1);
        var result2 = PairingRegenerator.Regenerate(state2, 1);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);

        var newState1 = result1.GetValueOrThrow();
        var newState2 = result2.GetValueOrThrow();

        // Both regenerations should produce same pairings
        var matches1 = newState1.GetRoundMatches(1);
        var matches2 = newState2.GetRoundMatches(1);

        Assert.Equal(matches1.Count, matches2.Count);

        for (int i = 0; i < matches1.Count; i++)
        {
            Assert.Equal(matches1[i].PlayerAId, matches2[i].PlayerAId);
            Assert.Equal(matches1[i].PlayerBId, matches2[i].PlayerBId);
        }
    }

    [Fact]
    public void Regenerate_ClearsOldByePlayerFlag()
    {
        // Arrange - 5 players (Swiss with BYE)
        var state = CreateStateWithRound1(5);
        var originalByeMatch = state.GetRoundMatches(1).First(m => m.IsBye);
        var originalByePlayerId = originalByeMatch.PlayerAId;

        // Verify original BYE player has the flag set
        Assert.True(state.Players[originalByePlayerId].ByeReceived);
        Assert.Equal(1, state.Players[originalByePlayerId].MatchWins);

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Check that only one player has byeReceived = true
        var playersWithBye = newState.Players.Values.Where(p => p.ByeReceived).ToList();
        Assert.Single(playersWithBye);
    }

    [Fact]
    public void Regenerate_MatchesHaveSameRound()
    {
        // Arrange
        var state = CreateStateWithRound1(6);

        // Act
        var result = PairingRegenerator.Regenerate(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        var matches = newState.GetRoundMatches(1);

        // All matches should be for round 1
        Assert.All(matches, m => Assert.Equal(1, m.Round));
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
