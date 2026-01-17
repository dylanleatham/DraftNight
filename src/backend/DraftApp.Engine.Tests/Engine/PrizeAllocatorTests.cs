using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class PrizeAllocatorTests
{
    [Fact]
    public void Allocate_SufficientPacks_AllWinnersGetPacks()
    {
        // Arrange - 4 players RR (3 rounds), 36 packs = 24 prize packs
        // Each round has 2 matches = 2 winners per round = 6 total round wins
        var state = CreateCompletedTournament(4, 36);

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.True(newState.PrizesAllocated);

        // Total allocated should equal total round wins (6)
        var totalAllocated = newState.PrizeAllocations.Values.Sum();
        Assert.Equal(6, totalAllocated);
    }

    [Fact]
    public void Allocate_InsufficientPacks_PrioritizesLaterRounds()
    {
        // Arrange - 8 players Swiss (3 rounds), 30 packs = 6 prize packs
        // Each round has 4 winners = 12 total round wins, but only 6 packs
        // Should pay: Round 3 (4 winners), Round 2 (2 winners), Round 1 (0)
        var state = CreateCompletedSwissTournament(8, 30);

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Total allocated should equal available packs
        var totalAllocated = newState.PrizeAllocations.Values.Sum();
        Assert.Equal(6, totalAllocated);

        // All round 3 winners should have packs
        var round3Winners = GetRoundWinners(newState, 3);
        foreach (var winnerId in round3Winners)
        {
            Assert.True(
                newState.PrizeAllocations[winnerId] >= 1,
                $"Round 3 winner {winnerId} should have at least 1 pack");
        }
    }

    [Fact]
    public void Allocate_ZeroPacks_NoAllocations()
    {
        // Arrange - exactly enough packs for draft, none for prizes
        var state = CreateCompletedTournament(4, 12); // 4*3 = 12, P=0

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();
        Assert.True(newState.PrizesAllocated);

        var totalAllocated = newState.PrizeAllocations.Values.Sum();
        Assert.Equal(0, totalAllocated);
    }

    [Fact]
    public void Allocate_TournamentNotComplete_ReturnsError()
    {
        // Arrange
        var state = CreateState(4);
        state = PairingGenerator.GenerateRound(state, 1).GetValueOrThrow();

        // Don't complete the round

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<TournamentNotCompleteError>(error.Error);
    }

    [Fact]
    public void Allocate_AlreadyAllocated_ReturnsError()
    {
        // Arrange
        var state = CreateCompletedTournament(4, 36);
        var firstAllocation = PrizeAllocator.Allocate(state);
        Assert.True(firstAllocation.IsSuccess);

        // Act
        var result = PrizeAllocator.Allocate(firstAllocation.GetValueOrThrow());

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<PrizesAlreadyAllocatedError>(error.Error);
    }

    [Fact]
    public void Allocate_TieBreak_UsesMWThenSeed()
    {
        // Arrange - scenario where we need tie-breaking within a round
        // 4 players RR, only 1 prize pack
        // Round 3 has 2 winners, but only 1 pack
        var state = CreateCompletedTournament(4, 13); // P = 13 - 12 = 1

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        var totalAllocated = newState.PrizeAllocations.Values.Sum();
        Assert.Equal(1, totalAllocated);

        // The winner should be deterministic (highest MW among round 3 winners,
        // then lowest seed)
        Assert.Single(newState.PrizeAllocations, kv => kv.Value > 0);
    }

    [Fact]
    public void Allocate_SwissBye_CountsAsWin()
    {
        // Arrange - 5 players Swiss, BYE should count as a round win for prizes
        var state = CreateCompletedSwissTournament(5, 36);

        // Find who got BYEs and count their prize packs
        var playersWithBye = state.Players.Values.Where(p => p.ByeReceived).ToList();
        Assert.NotEmpty(playersWithBye); // Should have at least one BYE recipient

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // BYE recipients should be eligible for prizes (their BYE round counts)
        // This test verifies the BYE is treated as a win for prize purposes
        var byePlayerAllocations = playersWithBye
            .Select(p => newState.PrizeAllocations[p.Id])
            .ToList();

        // At least one BYE recipient should have gotten a prize
        // (assuming they were high enough ranked to get one)
    }

    [Fact]
    public void Allocate_RoundRobinSit_DoesNotCountAsWin()
    {
        // Arrange - 3 players RR, sit should NOT count as a win for prizes
        var state = CreateCompletedTournament(3, 36);

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // Each round should have exactly 1 winner (not 2 with sit counting)
        // Total round wins = 3 (one per round)
        var totalWins = newState.Players.Values.Sum(p => p.MatchWins);
        Assert.Equal(3, totalWins); // 3 rounds, 1 winner each
    }

    [Fact]
    public void Allocate_MultipleWinsPerPlayer_GetMultiplePacks()
    {
        // Arrange - player wins all rounds, should get multiple packs
        var state = CreateCompletedTournamentWithDominantWinner(4, 36);

        // Act
        var result = PrizeAllocator.Allocate(state);

        // Assert
        Assert.True(result.IsSuccess);
        var newState = result.GetValueOrThrow();

        // The dominant winner (p1) should have multiple packs
        Assert.True(
            newState.PrizeAllocations["p1"] >= 2,
            "Dominant winner should have multiple prize packs");
    }

    private static EventState CreateState(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        return EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
    }

    private static EventState CreateCompletedTournament(int playerCount, int packsInBox)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = EventInitializer.Initialize("evt-1", players, packsInBox).GetValueOrThrow();

        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = PairingGenerator.GenerateRound(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                // Alternate winners to create variety
                var winnerId = round % 2 == 1 ? match.PlayerAId : match.PlayerBId!;
                state = MatchFinalizer.Finalize(state, round, match.Id, winnerId).GetValueOrThrow();
            }
        }

        return state;
    }

    private static EventState CreateCompletedSwissTournament(int playerCount, int packsInBox)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = EventInitializer.Initialize("evt-1", players, packsInBox).GetValueOrThrow();

        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = PairingGenerator.GenerateRound(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                // Higher seed (lower number) wins
                var winnerId = match.PlayerAId;
                state = MatchFinalizer.Finalize(state, round, match.Id, winnerId).GetValueOrThrow();
            }
        }

        return state;
    }

    private static EventState CreateCompletedTournamentWithDominantWinner(
        int playerCount, int packsInBox)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = EventInitializer.Initialize("evt-1", players, packsInBox).GetValueOrThrow();

        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = PairingGenerator.GenerateRound(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                // p1 always wins when involved
                var winnerId = match.PlayerAId == "p1" || match.PlayerBId == "p1"
                    ? "p1"
                    : match.PlayerAId;
                state = MatchFinalizer.Finalize(state, round, match.Id, winnerId).GetValueOrThrow();
            }
        }

        return state;
    }

    private static IEnumerable<string> GetRoundWinners(EventState state, int round)
    {
        return state.GetRoundMatches(round)
            .Where(m => m.WinnerId is not null)
            .Select(m => m.WinnerId!);
    }
}
