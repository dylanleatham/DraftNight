using DraftApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DraftApp.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for PrizeAllocationEntity.
/// </summary>
public class PrizeAllocationConfiguration : IEntityTypeConfiguration<PrizeAllocationEntity>
{
    public void Configure(EntityTypeBuilder<PrizeAllocationEntity> builder)
    {
        builder.ToTable("PrizeAllocations");

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.PacksAwarded)
            .IsRequired();

        // Unique constraint: one allocation per player per event
        builder.HasIndex(pa => new { pa.EventId, pa.PlayerId })
            .IsUnique();

        // Relationship to Event
        builder.HasOne(pa => pa.Event)
            .WithMany(e => e.PrizeAllocations)
            .HasForeignKey(pa => pa.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to Player
        builder.HasOne(pa => pa.Player)
            .WithOne(p => p.PrizeAllocation)
            .HasForeignKey<PrizeAllocationEntity>(pa => pa.PlayerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
