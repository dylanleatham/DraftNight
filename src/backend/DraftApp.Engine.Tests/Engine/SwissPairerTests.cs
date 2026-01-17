using System.Collections.Immutable;
using DraftApp.Engine.Engine;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class SwissPairerTests
{
    [Fact]
    public void GenerateRound1_SixPlayers_PairsAdjacentSeeds()
    {
        // Arrange
        var state = CreateSwissState(6);

        // Act
        var matches = SwissPairer.GenerateRound(state, 1);

        // Assert
        Assert.Equal(3, matches.Count);
        Assert.All(matches, m => Assert.False(m.IsBye));

        // 1v2, 3v4, 5v6
        Assert.Equal("p1", matches[0].PlayerAId);
        Assert.Equal("p2", matches[0].PlayerBId);
        Assert.Equal("p3", matches[1].PlayerAId);
        Assert.Equal("p4", matches[1].PlayerBId);
        Assert.Equal("p5", matches[2].PlayerAId);
        Assert.Equal("p6", matches[2].PlayerBId);
    }

    [Fact]
    public void GenerateRound1_EightPlayers_PairsAdjacentSeeds()
    {
        // Arrange
        var state = CreateSwissState(8);

        // Act
        var matches = SwissPairer.GenerateRound(state, 1);

        // Assert
        Assert.Equal(4, matches.Count);
        Assert.All(matches, m => Assert.False(m.IsBye));

        // 1v2, 3v4, 5v6, 7v8
        Assert.Equal("p1", matches[0].PlayerAId);
        Assert.Equal("p2", matches[0].PlayerBId);
        Assert.Equal("p3", matches[1].PlayerAId);
        Assert.Equal("p4", matches[1].PlayerBId);
        Assert.Equal("p5", matches[2].PlayerAId);
        Assert.Equal("p6", matches[2].PlayerBId);
        Assert.Equal("p7", matches[3].PlayerAId);
        Assert.Equal("p8", matches[3].PlayerBId);
    }

    [Fact]
    public void GenerateRound1_FivePlayers_LastPlayerGetsBye()
    {
        // Arrange
        var state = CreateSwissState(5);

        // Act
        var matches = SwissPairer.GenerateRound(state, 1);

        // Assert
        Assert.Equal(3, matches.Count);

        // 2 regular matches + 1 BYE
        Assert.Equal(2, matches.Count(m => !m.IsBye));
        Assert.Single(matches, m => m.IsBye);

        // Last unpaired (seed 5) gets BYE
        var byeMatch = matches.First(m => m.IsBye);
        Assert.Equal("p5", byeMatch.PlayerAId);
        Assert.Equal("p5", byeMatch.WinnerId); // BYE auto-wins
    }

    [Fact]
    public void GenerateRound1_SevenPlayers_LastPlayerGetsBye()
    {
        // Arrange
        var state = CreateSwissState(7);

        // Act
        var matches = SwissPairer.GenerateRound(state, 1);

        // Assert
        Assert.Equal(4, matches.Count);

        var byeMatch = matches.First(m => m.IsBye);
        Assert.Equal("p7", byeMatch.PlayerAId);
        Assert.Equal("p7", byeMatch.WinnerId);
    }

    [Fact]
    public void GenerateRound2_RanksPlayersByMW()
    {
        // Arrange - simulate round 1 where seeds 1,3,5 won
        var state = CreateSwissState(6);
        state = SimulateRound1Results(state, ["p1", "p3", "p5"]);

        // Act
        var matches = SwissPairer.GenerateRound(state, 2);

        // Assert - winners should be paired together, losers together
        Assert.Equal(3, matches.Count);

        // Top-ranked players (1-0) should pair: p1 vs p3 or p5
        var firstMatch = matches[0];
        var winners = new HashSet<string?> { "p1", "p3", "p5" };
        Assert.Contains(firstMatch.PlayerAId, winners);
        Assert.Contains(firstMatch.PlayerBId, winners);
    }

    [Fact]
    public void GenerateRound_AvoidRepeatOpponents()
    {
        // Arrange - 6 players, round 1 complete
        var state = CreateSwissState(6);
        state = SimulateRound1Results(state, ["p1", "p3", "p5"]);

        // Act
        var round2Matches = SwissPairer.GenerateRound(state, 2);

        // Assert - no repeats from round 1
        // Round 1 pairings: p1-p2, p3-p4, p5-p6
        foreach (var match in round2Matches.Where(m => !m.IsBye))
        {
            var pair = (match.PlayerAId, match.PlayerBId);

            // Should not be any of the round 1 pairings
            Assert.False(
                (pair == ("p1", "p2") || pair == ("p2", "p1")) ||
                (pair == ("p3", "p4") || pair == ("p4", "p3")) ||
                (pair == ("p5", "p6") || pair == ("p6", "p5")),
                $"Unexpected repeat pairing: {pair}");
        }
    }

    [Fact]
    public void GenerateRound_OddPlayers_ByeToLowestRankedWithoutPriorBye()
    {
        // Arrange - 5 players, round 1 complete (p5 had BYE)
        var state = CreateSwissState(5);
        state = SimulateRound1Results(state, ["p1", "p3"]); // p5 had BYE in round 1

        // p5 already has BYE, so next lowest without BYE should get it
        // After round 1: p1=1-0, p3=1-0, p5=1-0(BYE), p2=0-1, p4=0-1
        // Ranked: p1, p3, p5, p2, p4 (by MW desc, seed asc)
        // p4 is lowest-ranked without BYE

        // Act
        var round2Matches = SwissPairer.GenerateRound(state, 2);

        // Assert
        var byeMatch = round2Matches.FirstOrDefault(m => m.IsBye);
        Assert.NotNull(byeMatch);

        // BYE should go to p4 (lowest ranked without prior BYE) or p2
        // Both are 0-1, p2 has lower seed, so p4 is lowest ranked
        Assert.Equal("p4", byeMatch.PlayerAId);
    }

    [Fact]
    public void GenerateRound_ByeFairness_AllHaveBye_LowestRankedGetsIt()
    {
        // Arrange - 5 players, both rounds complete, now everyone has had BYE once
        // This is a hypothetical scenario for testing
        var state = CreateSwissState(5);

        // Manually set all players to have received BYE
        var playersWithBye = state.Players.Values
            .Select(p => p.WithByeReceived())
            .ToImmutableDictionary(p => p.Id);
        state = state.WithPlayers(playersWithBye);

        // Act
        var matches = SwissPairer.GenerateRound(state, 2);

        // Assert - BYE goes to absolute lowest ranked (p5 by seed)
        var byeMatch = matches.First(m => m.IsBye);
        Assert.Equal("p5", byeMatch.PlayerAId);
    }

    [Fact]
    public void GenerateRound_MatchIds_AreDeterministic()
    {
        // Arrange
        var state = CreateSwissState(6);

        // Act
        var round1a = SwissPairer.GenerateRound(state, 1);
        var round1b = SwissPairer.GenerateRound(state, 1);

        // Assert
        Assert.Equal(round1a.Count, round1b.Count);
        for (var i = 0; i < round1a.Count; i++)
        {
            Assert.Equal(round1a[i].Id, round1b[i].Id);
            Assert.Equal(round1a[i].PlayerAId, round1b[i].PlayerAId);
            Assert.Equal(round1a[i].PlayerBId, round1b[i].PlayerBId);
        }
    }

    [Fact]
    public void GenerateRound_MatchIds_FollowNamingConvention()
    {
        // Arrange
        var state = CreateSwissState(6);

        // Act
        var matches = SwissPairer.GenerateRound(state, 1);

        // Assert
        for (var i = 0; i < matches.Count; i++)
        {
            Assert.Equal($"r1-m{i}", matches[i].Id);
        }
    }

    [Fact]
    public void GenerateRound_AllPlayersCount()
    {
        // Every player should be in exactly one match per round

        // Arrange
        var state = CreateSwissState(8);

        // Act
        var matches = SwissPairer.GenerateRound(state, 1);

        // Assert
        var playerCounts = new Dictionary<string, int>();
        foreach (var match in matches)
        {
            playerCounts.TryGetValue(match.PlayerAId, out var countA);
            playerCounts[match.PlayerAId] = countA + 1;

            if (match.PlayerBId is not null)
            {
                playerCounts.TryGetValue(match.PlayerBId, out var countB);
                playerCounts[match.PlayerBId] = countB + 1;
            }
        }

        Assert.Equal(8, playerCounts.Count);
        Assert.All(playerCounts.Values, c => Assert.Equal(1, c));
    }

    [Fact]
    public void GenerateRound2_PrefersSameRecord()
    {
        // Arrange - 8 players, round 1 complete
        // Winners (1-0): p1, p3, p5, p7
        // Losers (0-1): p2, p4, p6, p8
        var state = CreateSwissState(8);
        state = SimulateRound1Results(state, ["p1", "p3", "p5", "p7"]);

        // Act
        var round2Matches = SwissPairer.GenerateRound(state, 2);

        // Assert - winners should pair with winners, losers with losers
        Assert.Equal(4, round2Matches.Count);

        var winners = new HashSet<string> { "p1", "p3", "p5", "p7" };
        var losers = new HashSet<string> { "p2", "p4", "p6", "p8" };

        foreach (var match in round2Matches)
        {
            var aIsWinner = winners.Contains(match.PlayerAId);
            var bIsWinner = winners.Contains(match.PlayerBId!);

            // Both should be winners or both should be losers
            Assert.True(
                aIsWinner == bIsWinner,
                $"Mixed record pairing: {match.PlayerAId} vs {match.PlayerBId}");
        }
    }

    private static EventState CreateSwissState(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        return EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
    }

    private static EventState SimulateRound1Results(EventState state, string[] winners)
    {
        // Generate round 1 pairings
        var round1Matches = SwissPairer.GenerateRound(state, 1);
        state = state.WithRoundMatches(1, round1Matches);

        // Apply results
        var winnersSet = winners.ToHashSet();

        foreach (var match in round1Matches)
        {
            if (match.IsBye)
            {
                // BYE already has winner set, update player state
                var byePlayer = state.Players[match.PlayerAId];
                state = state.WithPlayer(byePlayer.WithWin().WithByeReceived());
            }
            else
            {
                var winnerId = winnersSet.Contains(match.PlayerAId)
                    ? match.PlayerAId
                    : match.PlayerBId!;
                var loserId = winnerId == match.PlayerAId
                    ? match.PlayerBId!
                    : match.PlayerAId;

                // Update match result
                var updatedMatch = match.WithWinner(winnerId);
                state = state.WithMatch(1, updatedMatch);

                // Update player states
                var winner = state.Players[winnerId];
                var loser = state.Players[loserId];

                state = state.WithPlayer(winner.WithWin().WithOpponent(loserId, 1));
                state = state.WithPlayer(loser.WithLoss().WithOpponent(winnerId, 1));
            }
        }

        return state;
    }
}
