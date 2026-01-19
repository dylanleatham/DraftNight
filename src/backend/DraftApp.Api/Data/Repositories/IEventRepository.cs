using DraftApp.Api.Data.Entities;
using DraftApp.Engine.Models;

namespace DraftApp.Api.Data.Repositories;

/// <summary>
/// Repository for event persistence operations.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Gets an event by ID with all related entities loaded.
    /// </summary>
    Task<EventEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets an event by join code with all related entities loaded.
    /// </summary>
    Task<EventEntity?> GetByJoinCodeAsync(string joinCode, CancellationToken ct = default);

    /// <summary>
    /// Creates a new event from an engine state.
    /// </summary>
    Task<EventEntity> CreateAsync(
        EventState state,
        string name,
        string? joinCode,
        string hostPinHash,
        string hostToken,
        AuditLogEntity auditLog,
        CancellationToken ct = default);

    /// <summary>
    /// Loads an event and converts it to engine state.
    /// </summary>
    Task<EventState?> LoadEngineStateAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Persists engine state changes atomically with version check and audit log.
    /// Returns (true, newVersion) on success, (false, 0) on version conflict.
    /// </summary>
    Task<(bool Success, int NewVersion)> PersistEngineStateAsync(
        Guid eventId,
        int expectedVersion,
        EventState newState,
        AuditLogEntity auditLog,
        CancellationToken ct = default);
}
