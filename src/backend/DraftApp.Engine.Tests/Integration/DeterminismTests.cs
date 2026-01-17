using DraftApp.Engine.Engine;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Integration;

public class DeterminismTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void SameInputs_ProduceSameOutputs(int playerCount)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act - Run the same tournament twice
        var state1 = RunTournament(players, 36);
        var state2 = RunTournament(players, 36);

        // Assert - All outputs should be identical
        Assert.Equal(state1.Format, state2.Format);
        Assert.Equal(state1.TotalRounds, state2.TotalRounds);
        Assert.Equal(state1.PrizePacks, state2.PrizePacks);

        // Compare matches
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
                Assert.Equal(matches1[i].WinnerId, matches2[i].WinnerId);
            }
        }

        // Compare player states
        foreach (var playerId in state1.Players.Keys)
        {
            var p1 = state1.Players[playerId];
            var p2 = state2.Players[playerId];

            Assert.Equal(p1.MatchWins, p2.MatchWins);
            Assert.Equal(p1.MatchLosses, p2.MatchLosses);
            Assert.Equal(p1.ByeReceived, p2.ByeReceived);
            Assert.Equal(p1.Opponents, p2.Opponents);
        }

        // Compare prize allocations
        Assert.Equal(state1.PrizeAllocations, state2.PrizeAllocations);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void PairingGeneration_IsDeterministic(int playerCount)
    {
        // Arrange
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        var state = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();

        // Act - Generate round 1 multiple times
        var round1a = TournamentEngine.GenerateRoundPairings(state, 1).GetValueOrThrow();
        var round1b = TournamentEngine.GenerateRoundPairings(state, 1).GetValueOrThrow();

        // Assert - Should be identical
        var matchesA = round1a.GetRoundMatches(1);
        var matchesB = round1b.GetRoundMatches(1);

        Assert.Equal(matchesA.Count, matchesB.Count);
        for (var i = 0; i < matchesA.Count; i++)
        {
            Assert.Equal(matchesA[i].Id, matchesB[i].Id);
            Assert.Equal(matchesA[i].PlayerAId, matchesB[i].PlayerAId);
            Assert.Equal(matchesA[i].PlayerBId, matchesB[i].PlayerBId);
        }
    }

    [Fact]
    public void TieBreaking_IsDeterministic()
    {
        // Arrange - Create scenario where tie-breaking is needed
        var players = new[]
        {
            ("p1", "Alice"),
            ("p2", "Bob"),
            ("p3", "Charlie"),
            ("p4", "Diana")
        };

        // Act - Run tournament multiple times with same results
        var results1 = RunTournamentWithSpecificResults(players, 36);
        var results2 = RunTournamentWithSpecificResults(players, 36);

        // Assert - Prize allocations should be identical
        Assert.Equal(results1.PrizeAllocations, results2.PrizeAllocations);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    public void ByeAssignment_IsDeterministic(int playerCount)
    {
        // Arrange - odd player count for BYE scenarios
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act - Run round 1 multiple times
        var state1 = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();
        state1 = TournamentEngine.GenerateRoundPairings(state1, 1).GetValueOrThrow();

        var state2 = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();
        state2 = TournamentEngine.GenerateRoundPairings(state2, 1).GetValueOrThrow();

        // Assert - BYE recipient should be the same
        var bye1 = state1.GetRoundMatches(1).First(m => m.IsBye);
        var bye2 = state2.GetRoundMatches(1).First(m => m.IsBye);

        Assert.Equal(bye1.PlayerAId, bye2.PlayerAId);
    }

    [Fact]
    public void MatchIds_AreDeterministic()
    {
        // Arrange
        var players = Enumerable.Range(1, 6)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        // Act
        var state1 = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();
        state1 = TournamentEngine.GenerateRoundPairings(state1, 1).GetValueOrThrow();

        var state2 = TournamentEngine.InitializeEvent("evt-1", players, 36).GetValueOrThrow();
        state2 = TournamentEngine.GenerateRoundPairings(state2, 1).GetValueOrThrow();

        // Assert - All match IDs should match
        var ids1 = state1.GetRoundMatches(1).Select(m => m.Id).ToList();
        var ids2 = state2.GetRoundMatches(1).Select(m => m.Id).ToList();

        Assert.Equal(ids1, ids2);

        // Verify they follow the expected format
        for (var i = 0; i < ids1.Count; i++)
        {
            Assert.Equal($"r1-m{i}", ids1[i]);
        }
    }

    private static EventState RunTournament((string Id, string Name)[] players, int packsInBox)
    {
        var state = TournamentEngine.InitializeEvent("evt-1", players, packsInBox).GetValueOrThrow();

        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();

            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                // Deterministic result: lower seed (playerA) always wins
                state = TournamentEngine.FinalizeMatch(state, round, match.Id, match.PlayerAId)
                    .GetValueOrThrow();
            }
        }

        return TournamentEngine.AllocatePrizes(state).GetValueOrThrow();
    }

    private static EventState RunTournamentWithSpecificResults(
        (string Id, string Name)[] players, int packsInBox)
    {
        var state = TournamentEngine.InitializeEvent("evt-1", players, packsInBox).GetValueOrThrow();

        // Use a fixed pattern: alternate winners
        for (var round = 1; round <= state.TotalRounds; round++)
        {
            state = TournamentEngine.GenerateRoundPairings(state, round).GetValueOrThrow();

            var matchIndex = 0;
            foreach (var match in state.GetRoundMatches(round).Where(m => !m.IsBye && !m.IsComplete))
            {
                // Alternate between playerA and playerB winning
                var winnerId = (round + matchIndex) % 2 == 0
                    ? match.PlayerAId
                    : match.PlayerBId!;

                state = TournamentEngine.FinalizeMatch(state, round, match.Id, winnerId)
                    .GetValueOrThrow();
                matchIndex++;
            }
        }

        return TournamentEngine.AllocatePrizes(state).GetValueOrThrow();
    }
}
