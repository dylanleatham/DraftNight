using System.Text.Json;
using DraftApp.Engine.Engine;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Golden;

/// <summary>
/// Golden tests verify determinism - same inputs always produce same outputs.
/// </summary>
public class GoldenTestRunner
{
    [Theory]
    [InlineData(2, TournamentFormat.RoundRobin, 1)]
    [InlineData(3, TournamentFormat.RoundRobin, 3)]
    [InlineData(4, TournamentFormat.RoundRobin, 3)]
    [InlineData(5, TournamentFormat.Swiss, 3)]
    [InlineData(6, TournamentFormat.Swiss, 3)]
    [InlineData(7, TournamentFormat.Swiss, 3)]
    [InlineData(8, TournamentFormat.Swiss, 3)]
    public void DeterminismTest_SameInputsProduceSameOutputs(
        int playerCount,
        TournamentFormat expectedFormat,
        int expectedRounds)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act - Run tournament twice with identical inputs and results
        var state1 = RunCompleteTournament(players, 36);
        var state2 = RunCompleteTournament(players, 36);

        // Assert - Format and structure
        Assert.Equal(expectedFormat, state1.Format);
        Assert.Equal(expectedRounds, state1.TotalRounds);

        // Assert - Both runs produce identical results
        Assert.Equal(state1.Format, state2.Format);
        Assert.Equal(state1.TotalRounds, state2.TotalRounds);
        Assert.Equal(state1.PrizePacks, state2.PrizePacks);

        // Verify all rounds match
        for (var round = 1; round <= state1.TotalRounds; round++)
        {
            var matches1 = state1.GetRoundMatches(round);
            var matches2 = state2.GetRoundMatches(round);

            Assert.Equal(matches1.Count, matches2.Count);

            for (var i = 0; i < matches1.Count; i++)
            {
                Assert.Equal(matches1[i].Id, matches2[i].Id);
                Assert.Equal(matches1[i].PlayerAId, matches2[i].PlayerAId);
                Assert.Equal(matches1[i].PlayerBId, matches2[i].PlayerBId);
                Assert.Equal(matches1[i].IsBye, matches2[i].IsBye);
                Assert.Equal(matches1[i].WinnerId, matches2[i].WinnerId);
            }
        }

        // Verify player states match
        foreach (var playerId in state1.Players.Keys)
        {
            var p1 = state1.Players[playerId];
            var p2 = state2.Players[playerId];

            Assert.Equal(p1.MatchWins, p2.MatchWins);
            Assert.Equal(p1.MatchLosses, p2.MatchLosses);
            Assert.Equal(p1.ByeReceived, p2.ByeReceived);
        }

        // Verify prize allocations match
        Assert.Equal(state1.PrizeAllocations, state2.PrizeAllocations);
    }

    [Theory]
    [InlineData(8, 30, 6)] // Limited prizes scenario
    [InlineData(8, 36, 12)] // Full prizes scenario
    public void PrizeAllocation_LimitedPacks_PrioritizesLaterRounds(int playerCount, int packsInBox, int expectedPrizePacks)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act
        var state = RunCompleteTournament(players, packsInBox);

        // Assert
        Assert.Equal(expectedPrizePacks, state.PrizePacks);
        Assert.True(state.PrizesAllocated);

        var totalAllocated = state.PrizeAllocations.Values.Sum();
        Assert.True(totalAllocated <= expectedPrizePacks);

        // All round 3 winners should get at least 1 pack if packs >= winners
        var round3Winners = state.GetRoundWinners(3).ToList();
        if (expectedPrizePacks >= round3Winners.Count)
        {
            foreach (var winnerId in round3Winners)
            {
                Assert.True(
                    state.PrizeAllocations[winnerId] >= 1,
                    $"Round 3 winner {winnerId} should have at least 1 pack");
            }
        }
    }

    [Fact]
    public void RoundRobin_NoRepeats_CompleteSchedule()
    {
        // Arrange
        var players = Enumerable.Range(1, 4)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act
        var state = RunCompleteTournament(players, 36);

        // Assert - verify complete round-robin (each player plays every other exactly once)
        var pairings = new HashSet<(string, string)>();
        for (var round = 1; round <= state.TotalRounds; round++)
        {
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye))
            {
                var pair = NormalizePair(match.PlayerAId, match.PlayerBId!);
                Assert.DoesNotContain(pair, pairings);
                pairings.Add(pair);
            }
        }

        // 4 players = C(4,2) = 6 unique pairings
        Assert.Equal(6, pairings.Count);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    public void Swiss_OddPlayers_ByeFairness(int playerCount)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act
        var state = RunCompleteTournament(players, 36);

        // Assert - at least 3 different players should get BYEs over 3 rounds
        // (since we try to avoid repeat BYEs)
        var byeRecipients = new HashSet<string>();
        for (var round = 1; round <= 3; round++)
        {
            var byeMatch = state.GetRoundMatches(round).FirstOrDefault(m => m.IsBye);
            if (byeMatch != null)
            {
                byeRecipients.Add(byeMatch.PlayerAId);
            }
        }

        // With 3 rounds and odd players, should have 3 BYEs distributed among different players
        Assert.Equal(3, byeRecipients.Count);
    }

    private static EventState RunCompleteTournament((string Id, string Name)[] players, int packsInBox)
    {
        var state = TournamentEngine.InitializeEvent("evt-1", players, packsInBox).GetValueOrThrow();

        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();

            // Deterministic results: playerA always wins (or BYE auto-wins)
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                state = TournamentEngine.FinalizeMatch(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }
        }

        return TournamentEngine.AllocatePrizes(state).GetValueOrThrow();
    }

    private static (string First, string Second) NormalizePair(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal) < 0 ? (a, b) : (b, a);
}
