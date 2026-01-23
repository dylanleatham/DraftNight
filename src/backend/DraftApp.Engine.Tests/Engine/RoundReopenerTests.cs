using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class RoundReopenerTests
{
    [Fact]
    public void Reopen_SingleFinalizedMatch_ReversesStats()
    {
        // Arrange - round-robin 4 players: match 0 (p1 vs p4), match 1 (p2 vs p3)
        var state = CreateStateWithRound1(4);
        var match = state.GetRoundMatches(1)[0]; // p1 vs p4

        // Finalize the match - p1 wins
        var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, "p1");
        Assert.True(finalizeResult.IsSuccess);
        state = finalizeResult.GetValueOrThrow();

        Assert.Equal(1, state.Players["p1"].MatchWins);
        Assert.Equal(1, state.Players["p4"].MatchLosses);

        // Act
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        Assert.Equal(0, newState.Players["p1"].MatchWins);
        Assert.Equal(0, newState.Players["p4"].MatchLosses);

        var reopenedMatch = newState.GetRoundMatches(1)[0];
        Assert.Null(reopenedMatch.WinnerId);
    }

    [Fact]
    public void Reopen_AllMatchesFinalized_ReversesAllStats()
    {
        // Arrange
        var state = CreateStateWithRound1(4);
        var matches = state.GetRoundMatches(1);

        // Finalize all matches - player A wins each
        foreach (var match in matches)
        {
            var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, match.PlayerAId);
            Assert.True(finalizeResult.IsSuccess);
            state = finalizeResult.GetValueOrThrow();
        }

        // Act
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // All players should have reset stats
        foreach (var player in newState.Players.Values)
        {
            Assert.Equal(0, player.MatchWins);
            Assert.Equal(0, player.MatchLosses);
            Assert.Empty(player.Opponents);
        }

        // All matches should be open
        foreach (var match in newState.GetRoundMatches(1))
        {
            Assert.Null(match.WinnerId);
        }
    }

    [Fact]
    public void Reopen_SwissWithBye_PreservesByeMatchResult()
    {
        // Arrange - 5 players (Swiss with BYE)
        var state = CreateStateWithRound1(5);
        var matches = state.GetRoundMatches(1);
        var byeMatch = matches.First(m => m.IsBye);
        var regularMatches = matches.Where(m => !m.IsBye).ToList();

        // BYE match should already be finalized with the player as winner
        Assert.NotNull(byeMatch.WinnerId);
        var byePlayerId = byeMatch.PlayerAId;
        Assert.Equal(1, state.Players[byePlayerId].MatchWins);

        // Finalize the regular matches
        foreach (var match in regularMatches)
        {
            var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, match.PlayerAId);
            Assert.True(finalizeResult.IsSuccess);
            state = finalizeResult.GetValueOrThrow();
        }

        // Act
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // BYE match should still be finalized
        var reopenedByeMatch = newState.GetRoundMatches(1).First(m => m.IsBye);
        Assert.Equal(byePlayerId, reopenedByeMatch.WinnerId);

        // BYE player should still have their BYE win
        Assert.Equal(1, newState.Players[byePlayerId].MatchWins);

        // Regular matches should be reopened
        var reopenedRegularMatches = newState.GetRoundMatches(1).Where(m => !m.IsBye).ToList();
        foreach (var match in reopenedRegularMatches)
        {
            Assert.Null(match.WinnerId);
        }
    }

    [Fact]
    public void Reopen_RoundNotFound_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);

        // Act - try to reopen a round that doesn't exist
        var result = RoundReopener.Reopen(state, 2);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<RoundNotFoundError>(error.Error);
    }

    [Fact]
    public void Reopen_SubsequentRoundExists_ReturnsError()
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

        // Act - try to reopen round 1
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<SubsequentRoundsExistError>(error.Error);
    }

    [Fact]
    public void Reopen_NoFinalizedMatches_ReturnsError()
    {
        // Arrange
        var state = CreateStateWithRound1(4);

        // Act - try to reopen without any finalized matches
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<NoFinalizedMatchesError>(error.Error);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Reopen_AllPlayerCounts_Works(int playerCount)
    {
        // Arrange
        var state = CreateStateWithRound1(playerCount);
        var matches = state.GetRoundMatches(1).Where(m => !m.IsBye).ToList();

        // Finalize at least one match
        var match = matches[0];
        var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, match.PlayerAId);
        Assert.True(finalizeResult.IsSuccess);
        state = finalizeResult.GetValueOrThrow();

        // Act
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reopen_Determinism_SameInputsSameOutput()
    {
        // Arrange
        var state1 = CreateStateWithRound1(4);
        var state2 = CreateStateWithRound1(4);

        // Finalize all matches in both states
        foreach (var match in state1.GetRoundMatches(1))
        {
            var finalizeResult = MatchFinalizer.Finalize(state1, 1, match.Id, match.PlayerAId);
            state1 = finalizeResult.GetValueOrThrow();
        }

        foreach (var match in state2.GetRoundMatches(1))
        {
            var finalizeResult = MatchFinalizer.Finalize(state2, 1, match.Id, match.PlayerAId);
            state2 = finalizeResult.GetValueOrThrow();
        }

        // Act
        var result1 = RoundReopener.Reopen(state1, 1);
        var result2 = RoundReopener.Reopen(state2, 1);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);

        var newState1 = result1.GetValueOrThrow();
        var newState2 = result2.GetValueOrThrow();

        // Check all players have same stats
        foreach (var playerId in newState1.Players.Keys)
        {
            Assert.Equal(newState1.Players[playerId].MatchWins, newState2.Players[playerId].MatchWins);
            Assert.Equal(newState1.Players[playerId].MatchLosses, newState2.Players[playerId].MatchLosses);
        }
    }

    [Fact]
    public void Reopen_RemovesOpponentHistory()
    {
        // Arrange - round-robin 4 players: match 0 (p1 vs p4)
        var state = CreateStateWithRound1(4);
        var match = state.GetRoundMatches(1)[0]; // p1 vs p4

        // Finalize the match - p1 wins
        var finalizeResult = MatchFinalizer.Finalize(state, 1, match.Id, "p1");
        Assert.True(finalizeResult.IsSuccess);
        state = finalizeResult.GetValueOrThrow();

        // Verify opponent history was added
        Assert.Contains("p4", state.Players["p1"].Opponents);
        Assert.Contains("p1", state.Players["p4"].Opponents);

        // Act
        var result = RoundReopener.Reopen(state, 1);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Opponent history should be removed
        Assert.DoesNotContain("p4", newState.Players["p1"].Opponents);
        Assert.DoesNotContain("p1", newState.Players["p4"].Opponents);
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
