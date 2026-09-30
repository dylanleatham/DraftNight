using System.Text.Json;
using DraftApp.Api.Data;
using DraftApp.Api.Data.Entities;
using DraftApp.Api.Data.Enums;
using DraftApp.Api.Data.Repositories;
using DraftApp.Api.Models;
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

        // Generate player credentials for the host
        var playerId = Guid.NewGuid();
        var playerToken = authService.GenerateToken();

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
            HostToken = authService.HashToken(hostToken)
        };

        // Create the host as the first player (seed 1)
        var hostPlayer = new PlayerEntity
        {
            Id = playerId,
            EventId = eventId,
            Name = request.HostName,
            Seed = 1,
            MatchWins = 0,
            MatchLosses = 0,
            ByeReceived = false,
            IsDropped = false,
            OpponentsJson = "[]",
            LastPlayedRoundJson = "{}",
            PinHash = hostPinHash,
            PlayerToken = authService.HashToken(playerToken)
        };

        var eventAuditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ActionType = AuditActionType.EventCreated,
            EntityType = "Event",
            EntityId = eventId,
            CreatedAt = DateTime.UtcNow
        };

        var playerAuditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ActionType = AuditActionType.PlayerJoined,
            EntityType = "Player",
            EntityId = playerId,
            AfterJson = JsonSerializer.Serialize(new { name = request.HostName, seed = 1 }),
            CreatedAt = DateTime.UtcNow
        };

        context.Events.Add(entity);
        context.Players.Add(hostPlayer);
        context.AuditLogs.Add(eventAuditLog);
        context.AuditLogs.Add(playerAuditLog);
        await context.SaveChangesAsync(ct);

        return new CreateEventResponse
        {
            EventId = eventId,
            JoinCode = joinCode,
            HostToken = hostToken,
            PlayerId = playerId,
            PlayerToken = playerToken
        };
    }

    public async Task<JoinEventResult> JoinEventAsync(JoinEventRequest request, CancellationToken ct = default)
    {
        var eventEntity = await context.Events
            .Include(e => e.Players)
            .FirstOrDefaultAsync(e => e.JoinCode == request.JoinCode, ct);

        if (eventEntity is null || eventEntity.Status != EventStatus.Setup)
        {
            return JoinEventResult.NotFound();
        }

        if (eventEntity.Players.Count >= 8)
        {
            return JoinEventResult.LobbyFull();
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
            PlayerToken = authService.HashToken(playerToken)
        };

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            ActionType = AuditActionType.PlayerJoined,
            EntityType = "Player",
            EntityId = playerId,
            AfterJson = JsonSerializer.Serialize(new { name = request.PlayerName, seed }),
            CreatedAt = DateTime.UtcNow
        };

        // Add player directly to context to avoid collection tracking issues
        context.Players.Add(player);
        context.AuditLogs.Add(auditLog);

        if (!await SaveWithVersionBumpAsync(eventEntity, eventEntity.Version, ct))
        {
            return JoinEventResult.Conflict();
        }

        // Reload the event with updated data for broadcast
        var updatedEntity = await repository.GetByIdAsync(eventEntity.Id, ct);
        if (updatedEntity is not null)
        {
            var snapshot = MapToSnapshot(updatedEntity);
            await notificationService.BroadcastEventUpdateAsync(eventEntity.Id, snapshot, ct);
        }

        return JoinEventResult.Success(new JoinEventResponse
        {
            EventId = eventEntity.Id,
            PlayerId = playerId,
            PlayerToken = playerToken
        });
    }

    public async Task<ResumeSessionResponse?> ResumeSessionAsync(ResumeSessionRequest request, CancellationToken ct = default)
    {
        var joinCode = request.JoinCode.Trim().ToUpperInvariant();
        var eventEntity = await context.Events
            .Include(e => e.Players)
            .FirstOrDefaultAsync(e => e.JoinCode == joinCode, ct);

        if (eventEntity is null)
        {
            return null;
        }

        // Names aren't unique, so try the PIN against every seat with that name.
        // Dropped players keep their seat so they can still follow the event.
        var name = request.PlayerName.Trim();
        var player = eventEntity.Players
            .Where(p => p.PinHash is not null && string.Equals(p.Name.Trim(), name, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(p => authService.VerifyPin(request.Pin, p.PinHash!));

        if (player is null)
        {
            return null;
        }

        // Only token hashes are stored, so issue fresh tokens; old devices are signed out.
        var playerToken = authService.GenerateToken();
        player.PlayerToken = authService.HashToken(playerToken);

        string? hostToken = null;
        if (IsHostSeat(eventEntity, player))
        {
            // Tokens aren't part of the versioned tournament state, so update the column
            // directly rather than through the tracked entity and its Version check.
            hostToken = authService.GenerateToken();
            var hostTokenHash = authService.HashToken(hostToken);
            await context.Events
                .Where(e => e.Id == eventEntity.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(e => e.HostToken, hostTokenHash), ct);
        }

        await context.SaveChangesAsync(ct);

        return new ResumeSessionResponse
        {
            EventId = eventEntity.Id,
            JoinCode = joinCode,
            PlayerId = player.Id,
            PlayerToken = playerToken,
            HostToken = hostToken
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
        var hostTokenHash = authService.HashToken(hostToken);
        var eventEntity = await context.Events
            .FirstOrDefaultAsync(e => e.HostToken == hostTokenHash, ct);

        return eventEntity?.Id;
    }

    public async Task<MutationResponse> StartEventAsync(Guid eventId, int expectedVersion, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        if (entity.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
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

                        var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, pairingState, auditLog, ct);

                        if (!success)
                        {
                            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
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

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, pairingState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
                }

                // Broadcast update to connected clients - reload to get updated data
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
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
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

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, finalizedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
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

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, droppedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
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

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, prizeState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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
            .OrderBy(p => p.IsDropped)
            .ThenByDescending(p => p.MatchWins)
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
        var rows = await context.AuditLogs
            .Where(a => a.EventId == eventId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var logs = rows.Select(a =>
        {
            var details = AuditDetails.Parse(a.AfterJson);
            return new AuditLogEntry
            {
                Id = a.Id,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Reason = a.Reason,
                CreatedAt = a.CreatedAt,
                RoundNumber = details.RoundNumber,
                WinnerId = details.WinnerId,
                PlayerName = details.PlayerName
            };
        }).ToList();

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
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
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

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, reopenedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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

    public async Task<MutationResponse> SwapOpponentsAsync(
        Guid eventId,
        int roundNumber,
        Guid matchId1,
        Guid playerId1,
        Guid matchId2,
        Guid playerId2,
        int expectedVersion,
        string reason,
        CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
        }

        // Find the match codes from match IDs
        var match1 = entity.Rounds
            .Where(r => r.RoundNumber == roundNumber)
            .SelectMany(r => r.Matches)
            .FirstOrDefault(m => m.Id == matchId1);

        var match2 = entity.Rounds
            .Where(r => r.RoundNumber == roundNumber)
            .SelectMany(r => r.Matches)
            .FirstOrDefault(m => m.Id == matchId2);

        if (match1 is null)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Match 1 not found" };
        }

        if (match2 is null)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Match 2 not found" };
        }

        var swapResult = TournamentEngine.SwapOpponents(
            state,
            roundNumber,
            match1.MatchCode,
            playerId1.ToString(),
            match2.MatchCode,
            playerId2.ToString());

        return await swapResult.Match(
            async swappedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.OpponentsSwapped,
                    EntityType = "Round",
                    AfterJson = $"{{\"roundNumber\": {roundNumber}, \"match1Id\": \"{matchId1}\", \"player1Id\": \"{playerId1}\", \"match2Id\": \"{matchId2}\", \"player2Id\": \"{playerId2}\"}}",
                    Reason = reason
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, swappedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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

    public async Task<MutationResponse> ReopenRoundAsync(Guid eventId, int roundNumber, int expectedVersion, string reason, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
        }

        var reopenResult = TournamentEngine.ReopenRound(state, roundNumber);

        return await reopenResult.Match(
            async reopenedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.RoundReopened,
                    EntityType = "Round",
                    AfterJson = $"{{\"roundNumber\": {roundNumber}}}",
                    Reason = reason
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, reopenedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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

    public async Task<MutationResponse> RegeneratePairingsAsync(Guid eventId, int roundNumber, int expectedVersion, string reason, CancellationToken ct = default)
    {
        var state = await repository.LoadEngineStateAsync(eventId, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity!.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
        }

        var regenerateResult = TournamentEngine.RegeneratePairings(state, roundNumber);

        return await regenerateResult.Match(
            async regeneratedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.PairingsRegenerated,
                    EntityType = "Round",
                    AfterJson = $"{{\"roundNumber\": {roundNumber}}}",
                    Reason = reason
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, regeneratedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
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

    public async Task<MutationResponse> CancelEventAsync(Guid eventId, int expectedVersion, string? reason = null, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        if (entity.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
        }

        if (entity.Status == EventStatus.Archived)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Event already cancelled" };
        }

        // Update event status to Archived
        entity.Status = EventStatus.Archived;
        entity.JoinCode = null; // Clear join code so it can't be joined

        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ActionType = AuditActionType.EventCancelled,
            EntityType = "Event",
            EntityId = eventId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        context.AuditLogs.Add(auditLog);
        if (!await SaveWithVersionBumpAsync(entity, expectedVersion, ct))
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
        }

        // Broadcast cancellation to all connected clients
        await notificationService.BroadcastEventCancelledAsync(eventId, ct);

        return new MutationResponse { Success = true, NewVersion = entity.Version };
    }

    public async Task<Guid?> ValidatePlayerTokenForMatchAsync(string playerToken, Guid eventId, Guid matchId, CancellationToken ct = default)
    {
        // Find the player by token in this event
        var playerTokenHash = authService.HashToken(playerToken);
        var player = await context.Players
            .FirstOrDefaultAsync(p => p.PlayerToken == playerTokenHash && p.EventId == eventId, ct);

        if (player is null)
        {
            return null;
        }

        // Find the match and verify player is a participant
        var match = await context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match is null)
        {
            return null;
        }

        // Check if player is PlayerA or PlayerB in this match
        if (match.PlayerAId == player.Id || match.PlayerBId == player.Id)
        {
            return player.Id;
        }

        return null;
    }

    public async Task<Guid?> ValidatePlayerTokenAsync(string playerToken, Guid eventId, CancellationToken ct = default)
    {
        var playerTokenHash = authService.HashToken(playerToken);
        var player = await context.Players
            .FirstOrDefaultAsync(p => p.PlayerToken == playerTokenHash && p.EventId == eventId, ct);

        return player?.Id;
    }

    public async Task<MutationResponse> LeaveEventAsync(Guid eventId, Guid playerId, int expectedVersion, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.EventNotFound };
        }

        if (entity.Version != expectedVersion)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = MutationErrors.VersionConflict };
        }

        var player = entity.Players.FirstOrDefault(p => p.Id == playerId);
        if (player is null)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Player not found" };
        }

        // Host (seed 1) cannot leave - they should cancel instead
        if (player.Seed == 1)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Host cannot leave their own event. Use cancel instead." };
        }

        if (entity.Status == EventStatus.Setup)
        {
            // During Setup: Remove player completely from the database
            return await LeaveEventDuringSetupAsync(entity, player, expectedVersion, ct);
        }
        else if (entity.Status == EventStatus.Active)
        {
            // During Active: Use existing drop logic
            return await LeaveEventDuringActiveAsync(entity, player, expectedVersion, ct);
        }
        else
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Cannot leave event in current status" };
        }
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

    /// <summary>
    /// The host's seat is created with the host PIN hash itself. Each hash embeds a random
    /// salt, so a player who picks the same PIN as the host still gets a different string.
    /// </summary>
    private static bool IsHostSeat(EventEntity eventEntity, PlayerEntity player) =>
        player.PinHash is not null && string.Equals(player.PinHash, eventEntity.HostPinHash, StringComparison.Ordinal);

    /// <summary>
    /// Saves pending changes together with an optimistic-concurrency bump of the event version.
    /// The single SaveChanges call is atomic, and EF adds "WHERE Version = expectedVersion" to the
    /// event update, so a concurrent writer causes the whole save to fail rather than interleave.
    /// </summary>
    /// <returns>False if another writer changed the event first.</returns>
    private async Task<bool> SaveWithVersionBumpAsync(EventEntity entity, int expectedVersion, CancellationToken ct)
    {
        context.Entry(entity).Property(e => e.Version).OriginalValue = expectedVersion;
        entity.Version = expectedVersion + 1;
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private async Task<MutationResponse> LeaveEventDuringSetupAsync(
        Data.Entities.EventEntity entity,
        Data.Entities.PlayerEntity player,
        int expectedVersion,
        CancellationToken ct)
    {
        var playerSeed = player.Seed;
        var playerName = player.Name;

        // Remove the player from the database
        context.Players.Remove(player);

        // Reorder remaining players' seeds to be contiguous
        var remainingPlayers = entity.Players
            .Where(p => p.Id != player.Id && p.Seed > playerSeed)
            .OrderBy(p => p.Seed)
            .ToList();

        foreach (var p in remainingPlayers)
        {
            p.Seed -= 1;
        }

        // Create audit log entry
        var auditLog = new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            EventId = entity.Id,
            ActionType = AuditActionType.PlayerLeft,
            EntityType = "Player",
            EntityId = player.Id,
            AfterJson = JsonSerializer.Serialize(new { name = playerName, phase = "Setup" }),
            CreatedAt = DateTime.UtcNow
        };

        context.AuditLogs.Add(auditLog);
        if (!await SaveWithVersionBumpAsync(entity, expectedVersion, ct))
        {
            return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
        }

        // Broadcast update to connected clients
        var updatedEntity = await repository.GetByIdAsync(entity.Id, ct);
        if (updatedEntity is not null)
        {
            var snapshot = MapToSnapshot(updatedEntity);
            await notificationService.BroadcastEventUpdateAsync(entity.Id, snapshot, ct);
        }

        return new MutationResponse { Success = true, NewVersion = entity.Version };
    }

    private async Task<MutationResponse> LeaveEventDuringActiveAsync(
        Data.Entities.EventEntity entity,
        Data.Entities.PlayerEntity player,
        int expectedVersion,
        CancellationToken ct)
    {
        // Use existing drop logic via the engine
        var state = await repository.LoadEngineStateAsync(entity.Id, ct);
        if (state is null)
        {
            return new MutationResponse { Success = false, NewVersion = entity.Version, Error = "Failed to load engine state" };
        }

        var dropResult = TournamentEngine.DropPlayer(state, player.Id.ToString());

        return await dropResult.Match(
            async droppedState =>
            {
                var auditLog = new AuditLogEntity
                {
                    Id = Guid.NewGuid(),
                    ActionType = AuditActionType.PlayerLeft,
                    EntityType = "Player",
                    EntityId = player.Id,
                    AfterJson = JsonSerializer.Serialize(new { name = player.Name, phase = "Active" })
                };

                var (success, newVersion) = await repository.PersistEngineStateAsync(entity, expectedVersion, droppedState, auditLog, ct);

                if (!success)
                {
                    return new MutationResponse { Success = false, NewVersion = 0, Error = MutationErrors.VersionConflict };
                }

                // Broadcast update to connected clients
                var updatedEntity = await repository.GetByIdAsync(entity.Id, ct);
                if (updatedEntity is not null)
                {
                    var snapshot = MapToSnapshot(updatedEntity);
                    await notificationService.BroadcastEventUpdateAsync(entity.Id, snapshot, ct);
                }

                return new MutationResponse { Success = true, NewVersion = newVersion };
            },
            error => Task.FromResult(new MutationResponse { Success = false, NewVersion = entity.Version, Error = error.Message }));
    }
}
