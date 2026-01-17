using System.Collections.Immutable;
using DraftApp.Engine.Engine;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class RoundRobinSchedulerTests
{
    [Fact]
    public void GenerateAllRounds_TwoPlayers_GeneratesOneRound()
    {
        // Arrange
        var state = CreateState(2);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert
        Assert.Single(rounds);
        Assert.True(rounds.ContainsKey(1));

        var round1 = rounds[1];
        Assert.Single(round1);
        Assert.Equal("p1", round1[0].PlayerAId);
        Assert.Equal("p2", round1[0].PlayerBId);
    }

    [Fact]
    public void GenerateAllRounds_ThreePlayers_GeneratesThreeRounds_WithSits()
    {
        // Arrange
        var state = CreateState(3);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert
        Assert.Equal(3, rounds.Count);

        // Each player should sit exactly once across all rounds
        var sitCounts = new Dictionary<string, int>();
        var matchCounts = new Dictionary<string, int>();

        foreach (var round in rounds.Values)
        {
            foreach (var match in round)
            {
                if (match.IsBye)
                {
                    // This is a sit (odd-player RR BYE)
                    sitCounts.TryGetValue(match.PlayerAId, out var sitCount);
                    sitCounts[match.PlayerAId] = sitCount + 1;

                    // Sit should have no winner
                    Assert.Null(match.WinnerId);
                }
                else
                {
                    // Regular match
                    matchCounts.TryGetValue(match.PlayerAId, out var countA);
                    matchCounts[match.PlayerAId] = countA + 1;

                    matchCounts.TryGetValue(match.PlayerBId!, out var countB);
                    matchCounts[match.PlayerBId!] = countB + 1;
                }
            }
        }

        // Each player sits exactly once
        Assert.Equal(3, sitCounts.Count);
        Assert.All(sitCounts.Values, count => Assert.Equal(1, count));

        // Each player has exactly 2 matches (plays every other player once)
        Assert.Equal(3, matchCounts.Count);
        Assert.All(matchCounts.Values, count => Assert.Equal(2, count));
    }

    [Fact]
    public void GenerateAllRounds_FourPlayers_GeneratesThreeRounds_NoSits()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert
        Assert.Equal(3, rounds.Count);

        // No sits (all matches are regular)
        foreach (var round in rounds.Values)
        {
            Assert.Equal(2, round.Count); // 4 players = 2 matches per round
            Assert.All(round, m => Assert.False(m.IsBye));
        }

        // Each player plays 3 matches total (once against each opponent)
        var matchCounts = CountMatches(rounds);
        Assert.All(matchCounts.Values, count => Assert.Equal(3, count));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    public void GenerateAllRounds_CorrectRoundCount(int playerCount, int expectedRounds)
    {
        // Arrange
        var state = CreateState(playerCount);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert
        Assert.Equal(expectedRounds, rounds.Count);
    }

    [Fact]
    public void GenerateAllRounds_NoRepeatOpponents()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert
        var pairings = new HashSet<(string, string)>();
        foreach (var round in rounds.Values)
        {
            foreach (var match in round.Where(m => !m.IsBye))
            {
                var pair = NormalizePair(match.PlayerAId, match.PlayerBId!);
                Assert.DoesNotContain(pair, pairings);
                pairings.Add(pair);
            }
        }
    }

    [Fact]
    public void GenerateAllRounds_MatchIds_AreDeterministic()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var rounds1 = RoundRobinScheduler.GenerateAllRounds(state);
        var rounds2 = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert - same inputs should produce same match IDs
        foreach (var roundNum in rounds1.Keys)
        {
            var round1 = rounds1[roundNum];
            var round2 = rounds2[roundNum];

            Assert.Equal(round1.Count, round2.Count);
            for (var i = 0; i < round1.Count; i++)
            {
                Assert.Equal(round1[i].Id, round2[i].Id);
                Assert.Equal(round1[i].PlayerAId, round2[i].PlayerAId);
                Assert.Equal(round1[i].PlayerBId, round2[i].PlayerBId);
            }
        }
    }

    [Fact]
    public void GenerateAllRounds_MatchIds_FollowNamingConvention()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert
        foreach (var (roundNum, matches) in rounds)
        {
            for (var i = 0; i < matches.Count; i++)
            {
                Assert.Equal($"r{roundNum}-m{i}", matches[i].Id);
            }
        }
    }

    [Fact]
    public void GenerateRound_ReturnsSpecificRound()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var round2 = RoundRobinScheduler.GenerateRound(state, 2);

        // Assert
        Assert.Equal(2, round2.Count);
        Assert.All(round2, m => Assert.Equal(2, m.Round));
    }

    [Fact]
    public void GenerateRound_InvalidRound_ReturnsEmpty()
    {
        // Arrange
        var state = CreateState(4);

        // Act
        var result = RoundRobinScheduler.GenerateRound(state, 99);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateAllRounds_ThreePlayers_CircleMethodPairings()
    {
        // This tests the specific circle method rotation for 3 players
        // With ghost BYE: [p1, p2, p3, BYE]
        // Round 1: p1-BYE(sit), p2-p3
        // Rotate to: [p1, BYE, p2, p3]
        // Round 2: p1-p3, BYE-p2(sit)
        // Rotate to: [p1, p3, BYE, p2]
        // Round 3: p1-p2, p3-BYE(sit)

        // Arrange
        var state = CreateState(3);

        // Act
        var rounds = RoundRobinScheduler.GenerateAllRounds(state);

        // Assert - verify complete round-robin coverage
        var opponents = new Dictionary<string, HashSet<string>>
        {
            ["p1"] = new(),
            ["p2"] = new(),
            ["p3"] = new()
        };

        foreach (var round in rounds.Values)
        {
            foreach (var match in round.Where(m => !m.IsBye))
            {
                opponents[match.PlayerAId].Add(match.PlayerBId!);
                opponents[match.PlayerBId!].Add(match.PlayerAId);
            }
        }

        // Each player should have played every other player exactly once
        Assert.Equal(new HashSet<string> { "p2", "p3" }, opponents["p1"]);
        Assert.Equal(new HashSet<string> { "p1", "p3" }, opponents["p2"]);
        Assert.Equal(new HashSet<string> { "p1", "p2" }, opponents["p3"]);
    }

    private static EventState CreateState(int playerCount)
    {
        var players = Enumerable.Range(1, playerCount)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();

        return EventInitializer.Initialize("evt-1", players, 36).GetValueOrThrow();
    }

    private static Dictionary<string, int> CountMatches(
        ImmutableDictionary<int, ImmutableList<Match>> rounds)
    {
        var counts = new Dictionary<string, int>();
        foreach (var round in rounds.Values)
        {
            foreach (var match in round.Where(m => !m.IsBye))
            {
                counts.TryGetValue(match.PlayerAId, out var countA);
                counts[match.PlayerAId] = countA + 1;

                counts.TryGetValue(match.PlayerBId!, out var countB);
                counts[match.PlayerBId!] = countB + 1;
            }
        }

        return counts;
    }

    private static (string First, string Second) NormalizePair(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal) < 0 ? (a, b) : (b, a);
}
