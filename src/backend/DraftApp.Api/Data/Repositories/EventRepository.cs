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
        EventEntity entity,
        int expectedVersion,
        EventState newState,
        AuditLogEntity auditLog,
        CancellationToken ct = default)
    {
        // Track existing round/match IDs before update
        var existingRoundIds = entity.Rounds.Select(r => r.Id).ToHashSet();
        var existingMatchIds = entity.Rounds.SelectMany(r => r.Matches).Select(m => m.Id).ToHashSet();
        var existingPrizeIds = entity.PrizeAllocations.Select(p => p.Id).ToHashSet();

        var timestamp = DateTime.UtcNow;

        // Update entity from new state
        EventMapper.UpdateEntityFromState(entity, newState, timestamp);

        // Explicitly mark new entities as Added (EF doesn't auto-detect entities with pre-set GUIDs)
        foreach (var round in entity.Rounds.Where(r => !existingRoundIds.Contains(r.Id)))
        {
            context.Entry(round).State = EntityState.Added;
        }

        foreach (var match in entity.Rounds.SelectMany(r => r.Matches).Where(m => !existingMatchIds.Contains(m.Id)))
        {
            context.Entry(match).State = EntityState.Added;
        }

        foreach (var prize in entity.PrizeAllocations.Where(p => !existingPrizeIds.Contains(p.Id)))
        {
            context.Entry(prize).State = EntityState.Added;
        }

        // Compare-and-swap: EF's concurrency check uses the original value, so pin it to the
        // caller's expected version rather than whatever version happened to be loaded.
        context.Entry(entity).Property(e => e.Version).OriginalValue = expectedVersion;
        entity.Version = expectedVersion + 1;

        // Add audit log
        auditLog.EventId = entity.Id;
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
