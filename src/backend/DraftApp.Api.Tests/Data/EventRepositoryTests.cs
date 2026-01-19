using System.Collections.Immutable;
using DraftApp.Api.Data;
using DraftApp.Api.Data.Entities;
using DraftApp.Api.Data.Enums;
using DraftApp.Api.Data.Repositories;
using DraftApp.Engine.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftApp.Api.Tests.Data;

/// <summary>
/// Tests for EventRepository.
/// </summary>
public class EventRepositoryTests : IDisposable
{
    private readonly DraftAppDbContext context;
    private readonly EventRepository repository;

    public EventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DraftAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new DraftAppDbContext(options);
        context.Database.EnsureCreated();
        repository = new EventRepository(context);
    }

    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateAsync_CreatesEventWithPlayers()
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

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        // Act
        var result = await repository.CreateAsync(state, "Test Event", "ABC123", "hostPinHash", "hostToken", auditLog);

        // Assert
        Assert.Equal(eventId, result.Id);
        Assert.Equal("Test Event", result.Name);
        Assert.Equal("ABC123", result.JoinCode);
        Assert.Equal(2, result.Players.Count);
        Assert.Equal(1, result.Version);

        // Verify audit log was created
        var logs = await context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(AuditActionType.EventCreated, logs[0].ActionType);
    }

    [Fact]
    public async Task GetByIdAsync_LoadsAllRelatedEntities()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();

        var players = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1)
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

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        await repository.CreateAsync(state, "Test", null, "hostPinHash", "hostToken", auditLog);

        // Act
        var result = await repository.GetByIdAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
        Assert.Single(result.Players);
        Assert.Equal("Alice", result.Players.First().Name);
    }

    [Fact]
    public async Task GetByJoinCodeAsync_FindsCorrectEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var state = new EventState
        {
            EventId = eventId.ToString(),
            Players = ImmutableDictionary<string, Player>.Empty,
            PacksInBox = 36,
            PrizePacks = 36,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 0
        };

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        await repository.CreateAsync(state, "Test", "UNIQUE42", "hostPinHash", "hostToken", auditLog);

        // Act
        var result = await repository.GetByJoinCodeAsync("UNIQUE42");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
    }

    [Fact]
    public async Task LoadEngineStateAsync_ReturnsCorrectState()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();

        var players = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1)
        }.ToImmutableDictionary();

        var originalState = new EventState
        {
            EventId = eventId.ToString(),
            Players = players,
            PacksInBox = 36,
            PrizePacks = 33,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3
        };

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        await repository.CreateAsync(originalState, "Test", null, "hostPinHash", "hostToken", auditLog);

        // Act
        var loadedState = await repository.LoadEngineStateAsync(eventId);

        // Assert
        Assert.NotNull(loadedState);
        Assert.Equal(eventId.ToString(), loadedState.EventId);
        Assert.Equal(36, loadedState.PacksInBox);
        Assert.Equal(33, loadedState.PrizePacks);
        Assert.Single(loadedState.Players);
    }

    [Fact]
    public async Task PersistEngineStateAsync_Success_IncrementsVersion()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var player1Id = Guid.NewGuid();

        var players = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1)
        }.ToImmutableDictionary();

        var state = new EventState
        {
            EventId = eventId.ToString(),
            Players = players,
            PacksInBox = 36,
            PrizePacks = 33,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 3
        };

        var createLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        await repository.CreateAsync(state, "Test", null, "hostPinHash", "hostToken", createLog);

        // Update player
        var updatedPlayers = new Dictionary<string, Player>
        {
            [player1Id.ToString()] = Player.Create(player1Id.ToString(), "Alice", 1).WithWin()
        }.ToImmutableDictionary();

        var newState = state with { Players = updatedPlayers };

        var updateLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.MatchFinalized,
            EntityType = "Match"
        };

        // Act
        var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, 1, newState, updateLog);

        // Assert
        Assert.True(success);
        Assert.Equal(2, newVersion);

        var entity = await repository.GetByIdAsync(eventId);
        Assert.NotNull(entity);
        Assert.Equal(2, entity.Version);
        Assert.Equal(1, entity.Players.First().MatchWins);
    }

    [Fact]
    public async Task PersistEngineStateAsync_VersionConflict_ReturnsFalse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var state = new EventState
        {
            EventId = eventId.ToString(),
            Players = ImmutableDictionary<string, Player>.Empty,
            PacksInBox = 36,
            PrizePacks = 36,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 0
        };

        var createLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        await repository.CreateAsync(state, "Test", null, "hostPinHash", "hostToken", createLog);

        var updateLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.MatchFinalized,
            EntityType = "Match"
        };

        // Act - Try to persist with wrong version (expecting 5 when actual is 1)
        var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, 5, state, updateLog);

        // Assert
        Assert.False(success);
        Assert.Equal(0, newVersion);
    }

    [Fact]
    public async Task PersistEngineStateAsync_CreatesAuditLog()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var state = new EventState
        {
            EventId = eventId.ToString(),
            Players = ImmutableDictionary<string, Player>.Empty,
            PacksInBox = 36,
            PrizePacks = 36,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 0
        };

        var createLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event"
        };

        await repository.CreateAsync(state, "Test", null, "hostPinHash", "hostToken", createLog);

        var updateLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.PairingsGenerated,
            EntityType = "Round",
            Reason = "Round 1 pairings"
        };

        // Act
        await repository.PersistEngineStateAsync(eventId, 1, state, updateLog);

        // Assert
        var logs = await context.AuditLogs.Where(l => l.EventId == eventId).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.ActionType == AuditActionType.EventCreated);
        Assert.Contains(logs, l => l.ActionType == AuditActionType.PairingsGenerated);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadEngineStateAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await repository.LoadEngineStateAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}
