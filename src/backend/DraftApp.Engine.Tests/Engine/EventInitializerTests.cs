using DraftApp.Engine.Engine;
using DraftApp.Engine.Errors;
using DraftApp.Engine.Models;

namespace DraftApp.Engine.Tests.Engine;

public class EventInitializerTests
{
    [Theory]
    [InlineData(2, TournamentFormat.RoundRobin, 1)] // N=2, R=N-1=1
    [InlineData(3, TournamentFormat.RoundRobin, 3)] // N=3 odd, R=N=3
    [InlineData(4, TournamentFormat.RoundRobin, 3)] // N=4, R=N-1=3
    [InlineData(5, TournamentFormat.Swiss, 3)] // N=5 Swiss
    [InlineData(6, TournamentFormat.Swiss, 3)] // N=6 Swiss
    [InlineData(7, TournamentFormat.Swiss, 3)] // N=7 Swiss
    [InlineData(8, TournamentFormat.Swiss, 3)] // N=8 Swiss
    public void Initialize_ValidPlayerCounts_SelectsCorrectFormat(
        int playerCount, TournamentFormat expectedFormat, int expectedRounds)
    {
        // Arrange
        var players = CreatePlayers(playerCount);
        var packsInBox = 36;

        // Act
        var result = EventInitializer.Initialize("evt-1", players, packsInBox);

        // Assert
        Assert.True(result.IsSuccess);
        var state = result.GetValueOrThrow();
        Assert.Equal(expectedFormat, state.Format);
        Assert.Equal(expectedRounds, state.TotalRounds);
    }

    [Theory]
    [InlineData(2, 36, 30)] // 36 - 6 = 30
    [InlineData(4, 36, 24)] // 36 - 12 = 24
    [InlineData(8, 36, 12)] // 36 - 24 = 12
    [InlineData(8, 30, 6)] // 30 - 24 = 6
    [InlineData(2, 6, 0)] // 6 - 6 = 0 (exactly enough)
    public void Initialize_CalculatesPrizePacksCorrectly(
        int playerCount, int packsInBox, int expectedPrizePacks)
    {
        // Arrange
        var players = CreatePlayers(playerCount);

        // Act
        var result = EventInitializer.Initialize("evt-1", players, packsInBox);

        // Assert
        Assert.True(result.IsSuccess);
        var state = result.GetValueOrThrow();
        Assert.Equal(expectedPrizePacks, state.PrizePacks);
    }

    [Fact]
    public void Initialize_AssignsSeeds_InOrder()
    {
        // Arrange
        var players = new[]
        {
            ("alice", "Alice"),
            ("bob", "Bob"),
            ("charlie", "Charlie"),
            ("diana", "Diana")
        };

        // Act
        var result = EventInitializer.Initialize("evt-1", players, 36);

        // Assert
        Assert.True(result.IsSuccess);
        var state = result.GetValueOrThrow();
        Assert.Equal(1, state.Players["alice"].Seed);
        Assert.Equal(2, state.Players["bob"].Seed);
        Assert.Equal(3, state.Players["charlie"].Seed);
        Assert.Equal(4, state.Players["diana"].Seed);
    }

    [Fact]
    public void Initialize_InitializesPlayerState_ToDefaults()
    {
        // Arrange
        var players = CreatePlayers(4);

        // Act
        var result = EventInitializer.Initialize("evt-1", players, 36);

        // Assert
        Assert.True(result.IsSuccess);
        var state = result.GetValueOrThrow();

        foreach (var player in state.Players.Values)
        {
            Assert.Equal(0, player.MatchWins);
            Assert.Equal(0, player.MatchLosses);
            Assert.False(player.ByeReceived);
            Assert.False(player.IsDropped);
            Assert.Empty(player.Opponents);
            Assert.Empty(player.LastPlayedRound);
        }
    }

    [Fact]
    public void Initialize_InitializesEventState_ToDefaults()
    {
        // Arrange
        var players = CreatePlayers(4);

        // Act
        var result = EventInitializer.Initialize("evt-1", players, 36);

        // Assert
        Assert.True(result.IsSuccess);
        var state = result.GetValueOrThrow();
        Assert.Equal("evt-1", state.EventId);
        Assert.Equal(0, state.CurrentRound);
        Assert.False(state.IsComplete);
        Assert.False(state.PrizesAllocated);
        Assert.Empty(state.MatchesByRound);
        Assert.Empty(state.PrizeAllocations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(100)]
    public void Initialize_InvalidPlayerCount_ReturnsError(int playerCount)
    {
        // Arrange
        var players = CreatePlayers(playerCount);

        // Act
        var result = EventInitializer.Initialize("evt-1", players, 100);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        Assert.IsType<InvalidPlayerCountError>(error.Error);
    }

    [Fact]
    public void Initialize_DuplicatePlayerIds_ReturnsError()
    {
        // Arrange
        var players = new[]
        {
            ("p1", "Alice"),
            ("p2", "Bob"),
            ("p1", "Charlie") // Duplicate ID
        };

        // Act
        var result = EventInitializer.Initialize("evt-1", players, 36);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        var duplicateError = Assert.IsType<DuplicatePlayerIdError>(error.Error);
        Assert.Equal("p1", duplicateError.PlayerId);
    }

    [Theory]
    [InlineData(2, 5)] // Need 6, have 5
    [InlineData(4, 11)] // Need 12, have 11
    [InlineData(8, 23)] // Need 24, have 23
    public void Initialize_InsufficientPacks_ReturnsError(int playerCount, int packsInBox)
    {
        // Arrange
        var players = CreatePlayers(playerCount);

        // Act
        var result = EventInitializer.Initialize("evt-1", players, packsInBox);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.IsType<EngineResult<EventState>.Failure>(result);
        var packError = Assert.IsType<InsufficientPacksError>(error.Error);
        Assert.Equal(packsInBox, packError.PacksInBox);
        Assert.Equal(playerCount * 3, packError.DraftConsumed);
    }

    [Fact]
    public void Initialize_ZeroPrizePacks_Succeeds()
    {
        // Arrange - exactly enough packs for draft, none for prizes
        var players = CreatePlayers(4);
        var packsInBox = 12;  // 4 * 3 = 12

        // Act
        var result = EventInitializer.Initialize("evt-1", players, packsInBox);

        // Assert
        Assert.True(result.IsSuccess);
        var state = result.GetValueOrThrow();
        Assert.Equal(0, state.PrizePacks);
    }

    private static (string Id, string Name)[] CreatePlayers(int count) =>
        Enumerable.Range(1, count)
            .Select(i => ($"p{i}", $"Player {i}"))
            .ToArray();
}
