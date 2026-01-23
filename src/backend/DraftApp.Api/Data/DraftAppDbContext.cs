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
    /// Gets the tournament events.
    /// </summary>
    public DbSet<EventEntity> Events => Set<EventEntity>();

    /// <summary>
    /// Gets the players in tournaments.
    /// </summary>
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    /// <summary>
    /// Gets the tournament rounds.
    /// </summary>
    public DbSet<RoundEntity> Rounds => Set<RoundEntity>();

    /// <summary>
    /// Gets the match pairings and results.
    /// </summary>
    public DbSet<MatchEntity> Matches => Set<MatchEntity>();

    /// <summary>
    /// Gets the prize allocations.
    /// </summary>
    public DbSet<PrizeAllocationEntity> PrizeAllocations => Set<PrizeAllocationEntity>();

    /// <summary>
    /// Gets the audit logs.
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
