using DraftApp.Api.Data.Configurations;
using DraftApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DraftApp.Api.Data;

/// <summary>
/// Entity Framework Core database context for DraftApp.
/// </summary>
public class DraftAppDbContext : DbContext
{
    public DraftAppDbContext(DbContextOptions<DraftAppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Tournament events.
    /// </summary>
    public DbSet<EventEntity> Events => Set<EventEntity>();

    /// <summary>
    /// Players in tournaments.
    /// </summary>
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    /// <summary>
    /// Tournament rounds.
    /// </summary>
    public DbSet<RoundEntity> Rounds => Set<RoundEntity>();

    /// <summary>
    /// Match pairings and results.
    /// </summary>
    public DbSet<MatchEntity> Matches => Set<MatchEntity>();

    /// <summary>
    /// Prize allocations.
    /// </summary>
    public DbSet<PrizeAllocationEntity> PrizeAllocations => Set<PrizeAllocationEntity>();

    /// <summary>
    /// Audit logs.
    /// </summary>
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());
        modelBuilder.ApplyConfiguration(new RoundConfiguration());
        modelBuilder.ApplyConfiguration(new MatchConfiguration());
        modelBuilder.ApplyConfiguration(new PrizeAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
    }
}
