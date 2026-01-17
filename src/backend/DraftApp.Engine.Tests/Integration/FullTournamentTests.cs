using DraftApp.Engine.Engine;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Integration;

public class FullTournamentTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void FullTournament_AllPlayerCounts_CompletesSuccessfully(int playerCount)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act
        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Run all rounds
        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                state = TournamentEngine.FinalizeMatch(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }

            Assert.True(
                state.IsRoundComplete(round),
                $"Round {round} should be complete");
        }

        // Allocate prizes
        state = TournamentEngine.AllocatePrizes(state).GetValueOrThrow();

        // Assert
        Assert.True(state.IsComplete);
        Assert.True(state.PrizesAllocated);
        Assert.Equal(state.TotalRounds, state.CurrentRound);
    }

    [Theory]
    [InlineData(2, TournamentFormat.RoundRobin, 1)]
    [InlineData(3, TournamentFormat.RoundRobin, 3)]
    [InlineData(4, TournamentFormat.RoundRobin, 3)]
    [InlineData(5, TournamentFormat.Swiss, 3)]
    [InlineData(6, TournamentFormat.Swiss, 3)]
    [InlineData(7, TournamentFormat.Swiss, 3)]
    [InlineData(8, TournamentFormat.Swiss, 3)]
    public void Initialize_CorrectFormatAndRounds(
        int playerCount, TournamentFormat expectedFormat, int expectedRounds)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act
        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Assert
        Assert.Equal(expectedFormat, state.Format);
        Assert.Equal(expectedRounds, state.TotalRounds);
    }

    [Fact]
    public void SwissBye_CountsAsWin_ForStandingsAndPrizes()
    {
        // Arrange - 5 players Swiss
        var players = Enumerable.Range(1, 5)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Act - Run round 1
        state = TournamentEngine.GenerateRoundPairings(state, 1).GetValueOrThrow();

        // Find the BYE match
        var byeMatch = state.GetRoundMatches(1).First(m => m.IsBye);
        var byePlayerId = byeMatch.PlayerAId;

        // Assert BYE is auto-complete with winner
        Assert.Equal(byePlayerId, byeMatch.WinnerId);

        // BYE player should have 1 win
        Assert.Equal(1, state.Players[byePlayerId].MatchWins);
        Assert.True(state.Players[byePlayerId].ByeReceived);
    }

    [Fact]
    public void RoundRobinSit_DoesNotCountAsWin()
    {
        // Arrange - 3 players RR (will have sits)
        var players = Enumerable.Range(1, 3)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Act - Generate round 1
        state = TournamentEngine.GenerateRoundPairings(state, 1).GetValueOrThrow();

        // Find the sit match (IsBye but no winner)
        var sitMatch = state.GetRoundMatches(1).FirstOrDefault(m => m.IsBye);

        if (sitMatch is not null)
        {
            // Assert sit has no winner
            Assert.Null(sitMatch.WinnerId);

            // Sit player should have 0 wins
            Assert.Equal(0, state.Players[sitMatch.PlayerAId].MatchWins);
        }
    }

    [Fact]
    public void DropPlayer_ExcludedFromFuturePairings()
    {
        // Arrange - 6 players Swiss
        var players = Enumerable.Range(1, 6)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();
        state = TournamentEngine.GenerateRoundPairings(state, 1).GetValueOrThrow();

        // Complete round 1
        foreach (var match in state.GetRoundMatches(1).Where(m => !m.IsBye && !m.IsComplete))
        {
            state = TournamentEngine.FinalizeMatch(state, 1, match.Id, match.PlayerAId)
                .GetValueOrThrow();
        }

        // Act - Drop player p6
        state = TournamentEngine.DropPlayer(state, "p6").GetValueOrThrow();

        // Generate round 2
        state = TournamentEngine.GenerateRoundPairings(state, 2).GetValueOrThrow();

        // Assert - p6 should not be in any round 2 matches
        var round2Matches = state.GetRoundMatches(2);
        foreach (var match in round2Matches)
        {
            Assert.NotEqual("p6", match.PlayerAId);
            Assert.NotEqual("p6", match.PlayerBId);
        }
    }

    [Fact]
    public void PrizeAllocation_LaterRoundsFirst_LimitedPacks()
    {
        // Arrange - 8 players Swiss, 30 packs = 6 prize packs
        var players = Enumerable.Range(1, 8)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 30).GetValueOrThrow();
        Assert.Equal(6, state.PrizePacks);

        // Complete all rounds
        for (var round = 1; round <= 3; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                state = TournamentEngine.FinalizeMatch(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }
        }

        // Act
        state = TournamentEngine.AllocatePrizes(state).GetValueOrThrow();

        // Assert
        var totalAllocated = state.PrizeAllocations.Values.Sum();
        Assert.Equal(6, totalAllocated);

        // All 4 round 3 winners should have at least 1 pack
        var round3Winners = state.GetRoundWinners(3).ToHashSet();
        Assert.Equal(4, round3Winners.Count);
        foreach (var winnerId in round3Winners)
        {
            Assert.True(state.PrizeAllocations[winnerId] >= 1);
        }
    }

    [Fact]
    public void NoRepeats_InRoundRobin()
    {
        // Arrange - 4 players RR
        var players = Enumerable.Range(1, 4)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Act - Generate all rounds
        var allPairings = new HashSet<(string, string)>();
        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye))
            {
                var pair = NormalizePair(match.PlayerAId, match.PlayerBId!);

                // Assert no duplicates
                Assert.DoesNotContain(pair, allPairings);
                allPairings.Add(pair);
            }

            // Complete round
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                state = TournamentEngine.FinalizeMatch(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }
        }

        // Should have exactly C(4,2) = 6 unique pairings
        Assert.Equal(6, allPairings.Count);
    }

    [Fact]
    public void Swiss_PrefersNonRepeatOpponents()
    {
        // Arrange - 6 players Swiss
        var players = Enumerable.Range(1, 6)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Track pairings across rounds
        var pairingsByRound = new Dictionary<int, HashSet<(string, string)>>();

        for (var round = 1; round <= 3; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();

            var roundPairings = new HashSet<(string, string)>();
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye))
            {
                roundPairings.Add(NormalizePair(match.PlayerAId, match.PlayerBId!));
            }

            pairingsByRound[round] = roundPairings;

            // Complete round
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                state = TournamentEngine.FinalizeMatch(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }
        }

        // Count repeats
        var allPairings = pairingsByRound.Values.SelectMany(p => p).ToList();
        var uniquePairings = allPairings.ToHashSet();
        var repeatCount = allPairings.Count - uniquePairings.Count;

        // With 6 players and 3 rounds, should have very few or no repeats
        Assert.True(
            repeatCount <= 1,
            $"Expected at most 1 repeat pairing, got {repeatCount}");
    }

    private static (string First, string Second) NormalizePair(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal) < 0 ? (a, b) : (b, a);
}
