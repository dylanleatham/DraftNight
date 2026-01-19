using System.Collections.Immutable;
using DraftApp.Api.Data.Entities;
using DraftApp.Api.Data.Enums;
using DraftApp.Api.Data.Mapping;
using DraftApp.Engine.Models;

namespace DraftApp.Api.Tests.Data;

/// <summary>
/// Tests for EventMapper bidirectional conversion.
/// </summary>
public class EventMapperTests
{
    [Fact]
    public void ToEngineState_WithAllFields_MapsCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();
        var roundId = Guid.NewGuid();

        var entity = new EventEntity
        {
            Id = eventId,
            Name = "Test Event",
            Status = EventStatus.Active,
            JoinCode = "ABC123",
            PacksInBox = 36,
            PrizePacks = 30,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3,
            PrizesAllocated = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            HostPinHash = "testPinHash",
            HostToken = "testHostToken",
            Players = new List<PlayerEntity>
            {
                new()
                {
                    Id = player1Id,
                    EventId = eventId,
                    Name = "Alice",
                    Seed = 1,
                    MatchWins = 1,
                    MatchLosses = 0,
                    ByeReceived = false,
                    IsDropped = false,
                    OpponentsJson = $"[\"{player2Id}\"]",
                    LastPlayedRoundJson = $"{{\"{player2Id}\": 1}}"
                },
                new()
                {
                    Id = player2Id,
                    EventId = eventId,
                    Name = "Bob",
                    Seed = 2,
                    MatchWins = 0,
                    MatchLosses = 1,
                    ByeReceived = false,
                    IsDropped = false,
                    OpponentsJson = $"[\"{player1Id}\"]",
                    LastPlayedRoundJson = $"{{\"{player1Id}\": 1}}"
                }
            },
            Rounds = new List<RoundEntity>
            {
                new()
                {
                    Id = roundId,
                    EventId = eventId,
                    RoundNumber = 1,
                    Status = RoundStatus.Closed,
                    PublishedAt = DateTime.UtcNow.AddHours(-1),
                    ClosedAt = DateTime.UtcNow,
                    Matches = new List<MatchEntity>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            RoundId = roundId,
                            MatchCode = "r1-m0",
                            RoundNumber = 1,
                            PlayerAId = player1Id,
                            PlayerBId = player2Id,
                            WinnerId = player1Id,
                            Status = MatchStatus.Final,
                            FinalizedAt = DateTime.UtcNow
                        }
                    }
                }
            },
            PrizeAllocations = new List<PrizeAllocationEntity>()
        };

        // Act
        var state = EventMapper.ToEngineState(entity);

        // Assert
        Assert.Equal(eventId.ToString(), state.EventId);
        Assert.Equal(36, state.PacksInBox);
        Assert.Equal(30, state.PrizePacks);
        Assert.Equal(TournamentFormat.RoundRobin, state.Format);
        Assert.Equal(3, state.TotalRounds);
        Assert.False(state.PrizesAllocated);
        Assert.Equal(2, state.Players.Count);

        var alice = state.Players[player1Id.ToString()];
        Assert.Equal("Alice", alice.Name);
        Assert.Equal(1, alice.Seed);
        Assert.Equal(1, alice.MatchWins);
        Assert.Equal(0, alice.MatchLosses);
        Assert.Contains(player2Id.ToString(), alice.Opponents);
        Assert.Equal(1, alice.LastPlayedRound[player2Id.ToString()]);

        Assert.Single(state.MatchesByRound);
        var round1Matches = state.GetRoundMatches(1);
        Assert.Single(round1Matches);
        Assert.Equal("r1-m0", round1Matches[0].Id);
        Assert.Equal(player1Id.ToString(), round1Matches[0].WinnerId);
    }

    [Fact]
    public void CreateEntityFromState_WithPlayers_CreatesCorrectEntity()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var players = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1),
            [player2Id.ToString()] = Player.Create(player2Id.ToString(), "Bob", 2)
        }.ToImmutableDictionary();

        var state = new EventState
        {
            EventId = eventId.ToString(),
            Players = players,
            PacksInBox = 36,
            PrizePacks = 30,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3
        };

        var timestamp = DateTime.UtcNow;

        // Act
        var entity = EventMapper.CreateEntityFromState(state, "Test Event", "ABC123", "hostPinHash", "hostToken", timestamp);

        // Assert
        Assert.Equal(eventId, entity.Id);
        Assert.Equal("Test Event", entity.Name);
        Assert.Equal("ABC123", entity.JoinCode);
        Assert.Equal(EventStatus.Setup, entity.Status);
        Assert.Equal(36, entity.PacksInBox);
        Assert.Equal(30, entity.PrizePacks);
        Assert.Equal(TournamentFormat.RoundRobin, entity.Format);
        Assert.Equal(3, entity.TotalRounds);
        Assert.Equal(1, entity.Version);
        Assert.Equal(2, entity.Players.Count);
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var players = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1)
                .WithWin()
                .WithOpponent(player2Id.ToString(), 1),
            [player2Id.ToString()] = Player.Create(player2Id.ToString(), "Bob", 2)
                .WithLoss()
                .WithOpponent(player1Id.ToString(), 1)
        }.ToImmutableDictionary();

        var matches = ImmutableList.Create(
            Match.Create("r1-m0", 1, player1Id.ToString(), player2Id.ToString())
                .WithWinner(player1Id.ToString()));

        var state = new EventState
        {
            EventId = eventId.ToString(),
            Players = players,
            PacksInBox = 36,
            PrizePacks = 30,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3,
            MatchesByRound = ImmutableDictionary<int, ImmutableList<Match>>.Empty
                .Add(1, matches)
        };

        var timestamp = DateTime.UtcNow;

        // Act - Create entity from state
        var entity = EventMapper.CreateEntityFromState(state, "Test", null, "hostPinHash", "hostToken", timestamp);

        // Need to add round and matches manually since CreateEntityFromState only creates players
        var roundEntity = new RoundEntity
        {
            Id = Guid.NewGuid(),
            EventId = entity.Id,
            RoundNumber = 1,
            Status = RoundStatus.Closed,
            PublishedAt = timestamp,
            ClosedAt = timestamp,
            Matches = new List<MatchEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    MatchCode = "r1-m0",
                    RoundNumber = 1,
                    PlayerAId = player1Id,
                    PlayerBId = player2Id,
                    WinnerId = player1Id,
                    Status = MatchStatus.Final,
                    FinalizedAt = timestamp
                }
            }
        };
        entity.Rounds.Add(roundEntity);

        // Convert back to engine state
        var roundTripped = EventMapper.ToEngineState(entity);

        // Assert
        Assert.Equal(state.EventId, roundTripped.EventId);
        Assert.Equal(state.PacksInBox, roundTripped.PacksInBox);
        Assert.Equal(state.PrizePacks, roundTripped.PrizePacks);
        Assert.Equal(state.Format, roundTripped.Format);
        Assert.Equal(state.TotalRounds, roundTripped.TotalRounds);

        var alice = roundTripped.Players[player1Id.ToString()];
        Assert.Equal(1, alice.MatchWins);
        Assert.Contains(player2Id.ToString(), alice.Opponents);

        var roundMatches = roundTripped.GetRoundMatches(1);
        Assert.Single(roundMatches);
        Assert.Equal(player1Id.ToString(), roundMatches[0].WinnerId);
    }

    [Fact]
    public void UpdateEntityFromState_UpdatesPlayerStats()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();

        var entity = new EventEntity
        {
            Id = eventId,
            Name = "Test",
            Status = EventStatus.Setup,
            PacksInBox = 36,
            PrizePacks = 30,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            HostPinHash = "testPinHash",
            HostToken = "testHostToken",
            Players = new List<PlayerEntity>
            {
                new()
                {
                    Id = player1Id,
                    EventId = eventId,
                    Name = "Alice",
                    Seed = 1,
                    MatchWins = 0,
                    MatchLosses = 0,
                    OpponentsJson = "[]",
                    LastPlayedRoundJson = "{}"
                }
            },
            Rounds = new List<RoundEntity>(),
            PrizeAllocations = new List<PrizeAllocationEntity>()
        };

        var updatedPlayers = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1)
                .WithWin()
                .WithWin()
        }.ToImmutableDictionary();

        var newState = new EventState
        {
            EventId = eventId.ToString(),
            Players = updatedPlayers,
            PacksInBox = 36,
            PrizePacks = 30,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3
        };

        // Act
        EventMapper.UpdateEntityFromState(entity, newState, DateTime.UtcNow);

        // Assert
        var player = entity.Players.First();
        Assert.Equal(2, player.MatchWins);
    }
}
