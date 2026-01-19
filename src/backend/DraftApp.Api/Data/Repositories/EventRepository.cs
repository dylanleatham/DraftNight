using DraftApp.Api.Data.Entities;
using DraftApp.Api.Data.Mapping;
using DraftApp.Engine.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftApp.Api.Data.Repositories;

/// <summary>
/// Repository implementation for event persistence.
/// </summary>
public class EventRepository(DraftAppDbContext context) : IEventRepository
{
    public async Task<EventEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Events
            .Include(e => e.Players)
            .Include(e => e.Rounds)
                .ThenInclude(r => r.Matches)
            .Include(e => e.PrizeAllocations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<EventEntity?> GetByJoinCodeAsync(string joinCode, CancellationToken ct = default)
    {
        return await context.Events
            .Include(e => e.Players)
            .Include(e => e.Rounds)
                .ThenInclude(r => r.Matches)
            .Include(e => e.PrizeAllocations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.JoinCode == joinCode, ct);
    }

    public async Task<EventEntity> CreateAsync(
        EventState state,
        string name,
        string? joinCode,
        string hostPinHash,
        string hostToken,
        AuditLogEntity auditLog,
        CancellationToken ct = default)
    {
        var timestamp = DateTime.UtcNow;
        var entity = EventMapper.CreateEntityFromState(state, name, joinCode, hostPinHash, hostToken, timestamp);

        auditLog.EventId = entity.Id;
        auditLog.CreatedAt = timestamp;

        context.Events.Add(entity);
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<EventState?> LoadEngineStateAsync(Guid eventId, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return null;
        }

        return EventMapper.ToEngineState(entity);
    }

    public async Task<(bool Success, int NewVersion)> PersistEngineStateAsync(
        Guid eventId,
        int expectedVersion,
        EventState newState,
        AuditLogEntity auditLog,
        CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(eventId, ct);
        if (entity is null)
        {
            return (false, 0);
        }

        // Optimistic concurrency check
        if (entity.Version != expectedVersion)
        {
            return (false, 0);
        }

        var timestamp = DateTime.UtcNow;

        // Update entity from new state
        EventMapper.UpdateEntityFromState(entity, newState, timestamp);

        // Increment version
        entity.Version = expectedVersion + 1;

        // Add audit log
        auditLog.EventId = eventId;
        auditLog.CreatedAt = timestamp;
        context.AuditLogs.Add(auditLog);

        try
        {
            await context.SaveChangesAsync(ct);
            return (true, entity.Version);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, 0);
        }
    }
}
