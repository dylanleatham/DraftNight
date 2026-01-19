using DraftApp.Api.Data;
using DraftApp.Api.Data.Entities;
using DraftApp.Api.Data.Enums;
using DraftApp.Api.Data.Repositories;
using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;
using DraftApp.Engine.Engine;
using DraftApp.Engine.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftApp.Api.Services;

/// <summary>
/// Implementation of event service orchestrating repository, engine, and authorization.
/// </summary>
public class EventService(
    DraftAppDbContext context,
    IEventRepository repository,
    IAuthorizationService authService,
    IEventNotificationService notificationService) : IEventService
{
    public async Task<CreateEventResponse> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var eventId = Guid.NewGuid();
        var joinCode = authService.GenerateJoinCode();
        var hostToken = authService.GenerateToken();
        var hostPinHash = authService.HashPin(request.HostPin);

        var entity = new EventEntity
        {
            Id = eventId,
            Name = request.Name,
            Status = EventStatus.Setup,
            JoinCode = joinCode,
            PacksInBox = request.PacksInBox,
            PrizePacks = 0,
            Format = TournamentFormat.RoundRobin,
            TotalRounds = 0,
            PrizesAllocated = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            HostPinHash = hostPinHash,
            HostToken = hostToken
        };

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event",
            EntityId = eventId,
            CreatedAt = DateTime.UtcNow
        };

        context.Events.Add(entity);
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(ct);

        return new CreateEventResponse
        {
            EventId = eventId,
            JoinCode = joinCode,
            HostToken = hostToken
        };
    }

    public async Task<JoinEventResponse?> JoinEventAsync(JoinEventRequest request, CancellationToken ct = default)
    {
        var eventEntity = await context.Events
            .Include(e => e.Players)
            .FirstOrDefaultAsync(e => e.JoinCode == request.JoinCode, ct);

        if (eventEntity is null || eventEntity.Status != EventStatus.Setup)
        {
            return null;
        }

        if (eventEntity.Players.Count >= 8)
        {
            return null;
        }

        var playerId = Guid.NewGuid();
        var playerToken = authService.GenerateToken();
        var pinHash = authService.HashPin(request.PlayerPin);
        var seed = eventEntity.Players.Count + 1;

        var player = new PlayerEntity
        {
            Id = playerId,
            EventId = eventEntity.Id,
            Name = request.PlayerName,
            Seed = seed,
            MatchWins = 0,
            MatchLosses = 0,
            ByeReceived = false,
            IsDropped = false,
            OpponentsJson = "[]",
            LastPlayedRoundJson = "{}",
            PinHash = pinHash,
            PlayerToken = playerToken
        };

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            ActionType = AuditActionType.PlayerJoined,
            EntityType = "Player",
            EntityId = playerId,
            AfterJson = $"{{\"name\": \"{request.PlayerName}\", \"seed\": {seed}}}",
            CreatedAt = DateTime.UtcNow
        };

        eventEntity.Players.Add(player);
        eventEntity.Version++;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync(ct);

        // Broadcast update to connected clients
        var snapshot = MapToSnapshot(eventEntity);
        await notificationService.BroadcastEventUpdateAsync(eventEntity.Id, snapshot, ct);

        return new JoinEventResponse
        {
            EventId = eventEntity.Id,
            PlayerId = playerId,
            PlayerToken = playerToken
        };
    }

    public async Task<EventSnapshotResponse?> GetEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return null;
        }

        return MapToSnapshot(entity);
    }

    public async Task<Guid?> ValidateHostTokenAsync(string hostToken, CancellationToken ct = default)
    {
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.HostToken == hostToken, ct);

        return eventEntity?.Id;
    }

    public async Task<MutationResponse> StartEventAsync(Guid eventId, int expectedVersion, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = "Event not found" };
        }

        if (entity.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Version conflict" };
        }

        if (entity.Status != EventStatus.Setup)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Event already started" };
        }

        if (entity.Players.Count < 2)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Need at least 2 players" };
        }

        var players = entity.Players
            .OrderBy(p => p.Seed)
            .Select(p => (p.Id.ToString(), p.Name))
            .ToList();

        var initResult = TournamentEngine.InitializeEvent(eventId.ToString(), players, entity.PacksInBox);

        return await initResult.Match(
            async initState =>
            {
                var pairingResult = TournamentEngine.GenerateRoundPairings(initState, 1);

                return await pairingResult.Match(
                    async pairingState =>
                    {
                        var auditLog = new AuditLogEntity
                        {
                            Id = Guid.NewGuid(),
                            ActionType = AuditActionType.EventStarted,
                            EntityType = "Event",
                            EntityId = eventId,
                            AfterJson = $"{{\"playerCount\": {players.Count}, \"format\": \"{pairingState.Format}\", \"totalRounds\": {pairingState.TotalRounds}}}"
                        };

                        var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, expectedVersion, pairingState, auditLog, ct);

                        if (!success)
                        {
                            return new MutationResponse { Success = false, NewVersion = 0, Error = "Version conflict" };
                        }

                        // Broadcast update to connected clients
                        var updatedEntity = await repository.GetByIdAsync(eventId, ct);
                        if (updatedEntity is not null)
                        {
                            var snapshot = MapToSnapshot(updatedEntity);
                            await notificationService.BroadcastEventUpdateAsync(eventId, snapshot, ct);
                        }

                        return new MutationResponse { Success = true, NewVersion = newVersion };
                    },
                    error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }

    public async Task<MutationResponse> PublishPairingsAsync(Guid eventId, int roundNumber, int expectedVersion, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = "Event not found" };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Version conflict" };
        }

        var pairingResult = TournamentEngine.GenerateRoundPairings(state, roundNumber);

        return await pairingResult.Match(
            async pairingState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.PairingsGenerated,
                    EntityType = "Round",
                    AfterJson = $"{{\"roundNumber\": {roundNumber}}}"
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, expectedVersion, pairingState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = "Version conflict" };
                }

                // Broadcast update to connected clients
                var updatedEntity = await repository.GetByIdAsync(eventId, ct);
                if (updatedEntity is not null)
                {
                    var snapshot = MapToSnapshot(updatedEntity);
                    await notificationService.BroadcastEventUpdateAsync(eventId, snapshot, ct);
                }

                return new MutationResponse { Success = true, NewVersion = newVersion };
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }

    public async Task<MutationResponse> FinalizeMatchAsync(Guid eventId, Guid matchId, Guid winnerId, int expectedVersion, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = "Event not found" };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Version conflict" };
        }

        var match = entity.Rounds
            .SelectMany(r => r.Matches)
            .FirstOrDefault(m => m.Id == matchId);

        if (match is null)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Match not found" };
        }

        var finalizeResult = TournamentEngine.FinalizeMatch(state, match.RoundNumber, match.MatchCode, winnerId.ToString());

        return await finalizeResult.Match(
            async finalizedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.MatchFinalized,
                    EntityType = "Match",
                    EntityId = matchId,
                    AfterJson = $"{{\"winnerId\": \"{winnerId}\"}}"
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, expectedVersion, finalizedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = "Version conflict" };
                }

                // Broadcast update to connected clients
                var updatedEntity = await repository.GetByIdAsync(eventId, ct);
                if (updatedEntity is not null)
                {
                    var snapshot = MapToSnapshot(updatedEntity);
                    await notificationService.BroadcastEventUpdateAsync(eventId, snapshot, ct);
                }

                return new MutationResponse { Success = true, NewVersion = newVersion };
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }

    public async Task<MutationResponse> DropPlayerAsync(Guid eventId, Guid playerId, int expectedVersion, string? reason, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = "Event not found" };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Version conflict" };
        }

        var dropResult = TournamentEngine.DropPlayer(state, playerId.ToString());

        return await dropResult.Match(
            async droppedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.PlayerDropped,
                    EntityType = "Player",
                    EntityId = playerId,
                    Reason = reason
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, expectedVersion, droppedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = "Version conflict" };
                }

                // Broadcast update to connected clients
                var updatedEntity = await repository.GetByIdAsync(eventId, ct);
                if (updatedEntity is not null)
                {
                    var snapshot = MapToSnapshot(updatedEntity);
                    await notificationService.BroadcastEventUpdateAsync(eventId, snapshot, ct);
                }

                return new MutationResponse { Success = true, NewVersion = newVersion };
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }

    public async Task<MutationResponse> AllocatePrizesAsync(Guid eventId, int expectedVersion, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = "Event not found" };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Version conflict" };
        }

        var prizeResult = TournamentEngine.AllocatePrizes(state);

        return await prizeResult.Match(
            async prizeState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.PrizesAllocated,
                    EntityType = "Event",
                    EntityId = eventId
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, expectedVersion, prizeState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = "Version conflict" };
                }

                // Broadcast update to connected clients
                var updatedEntity = await repository.GetByIdAsync(eventId, ct);
                if (updatedEntity is not null)
                {
                    var snapshot = MapToSnapshot(updatedEntity);
                    await notificationService.BroadcastEventUpdateAsync(eventId, snapshot, ct);
                }

                return new MutationResponse { Success = true, NewVersion = newVersion };
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }

    public async Task<StandingsResponse?> GetStandingsAsync(Guid eventId, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return null;
        }

        var standings = entity.Players
            .OrderByDescending(p => p.MatchWins)
            .ThenBy(p => p.Seed)
            .ThenBy(p => p.Id)
            .Select((p, index) => new StandingEntry
            {
                Rank = index + 1,
                PlayerId = p.Id,
                PlayerName = p.Name,
                MatchWins = p.MatchWins,
                MatchLosses = p.MatchLosses,
                ByeReceived = p.ByeReceived,
                IsDropped = p.IsDropped
            })
            .ToList();

        return new StandingsResponse { Standings = standings };
    }

    public async Task<AuditLogResponse?> GetAuditLogAsync(Guid eventId, CancellationToken ct = default)
    {
        var logs = await context.AuditLogs
            .Where(a => a.EventId == eventId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogEntry
            {
                Id = a.Id,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Reason = a.Reason,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        if (logs.Count == 0)
        {
            var exists = await context.Events.AnyAsync(e => e.Id == eventId, ct);
            if (!exists)
            {
                return null;
            }
        }

        return new AuditLogResponse { Entries = logs };
    }

    public async Task<MutationResponse> ReopenMatchAsync(Guid eventId, Guid matchId, int expectedVersion, string reason, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = "Event not found" };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Version conflict" };
        }

        var match = entity.Rounds
            .SelectMany(r => r.Matches)
            .FirstOrDefault(m => m.Id == matchId);

        if (match is null)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Match not found" };
        }

        var reopenResult = TournamentEngine.ReopenMatch(state, match.RoundNumber, match.MatchCode);

        return await reopenResult.Match(
            async reopenedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.MatchReopened,
                    EntityType = "Match",
                    EntityId = matchId,
                    Reason = reason
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(eventId, expectedVersion, reopenedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = "Version conflict" };
                }

                // Broadcast update to connected clients
                var updatedEntity = await repository.GetByIdAsync(eventId, ct);
                if (updatedEntity is not null)
                {
                    var snapshot = MapToSnapshot(updatedEntity);
                    await notificationService.BroadcastEventUpdateAsync(eventId, snapshot, ct);
                }

                return new MutationResponse { Success = true, NewVersion = newVersion };
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }

    private static EventSnapshotResponse MapToSnapshot(EventEntity entity)
    {
        var players = entity.Players
            .OrderBy(p => p.Seed)
            .Select(p => new PlayerResponse
            {
                Id = p.Id,
                Name = p.Name,
                Seed = p.Seed,
                MatchWins = p.MatchWins,
                MatchLosses = p.MatchLosses,
                ByeReceived = p.ByeReceived,
                IsDropped = p.IsDropped
            })
            .ToList();

        var rounds = entity.Rounds
            .OrderBy(r => r.RoundNumber)
            .Select(r => new RoundResponse
            {
                RoundNumber = r.RoundNumber,
                Status = r.Status,
                Matches = r.Matches
                    .OrderBy(m => m.MatchCode)
                    .Select(m => new MatchResponse
                    {
                        Id = m.Id,
                        MatchCode = m.MatchCode,
                        PlayerAId = m.PlayerAId,
                        PlayerBId = m.PlayerBId,
                        WinnerId = m.WinnerId,
                        Status = m.Status,
                        IsBye = m.PlayerBId is null
                    })
                    .ToList()
            })
            .ToList();

        var prizeAllocations = entity.PrizeAllocations
            .Select(pa => new PrizeAllocationResponse
            {
                PlayerId = pa.PlayerId,
                PacksAwarded = pa.PacksAwarded
            })
            .ToList();

        var currentRound = entity.Rounds.Count > 0
            ? entity.Rounds.Max(r => r.RoundNumber)
            : 0;

        return new EventSnapshotResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Status = entity.Status,
            JoinCode = entity.JoinCode,
            PacksInBox = entity.PacksInBox,
            PrizePacks = entity.PrizePacks,
            Format = entity.Format,
            TotalRounds = entity.TotalRounds,
            CurrentRound = currentRound,
            PrizesAllocated = entity.PrizesAllocated,
            Version = entity.Version,
            Players = players,
            Rounds = rounds,
            PrizeAllocations = prizeAllocations
        };
    }
}
