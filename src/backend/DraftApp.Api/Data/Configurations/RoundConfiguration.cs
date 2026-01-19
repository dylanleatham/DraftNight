using DraftApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DraftApp.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for RoundEntity.
/// </summary>
public class RoundConfiguration : IEntityTypeConfiguration<RoundEntity>
{
    public void Configure(EntityTypeBuilder<RoundEntity> builder)
    {
        builder.ToTable("Rounds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoundNumber)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired();

        // Unique constraint: round number per event
        builder.HasIndex(r => new { r.EventId, r.RoundNumber })
            .IsUnique();

        // Relationship to Event
        builder.HasOne(r => r.Event)
            .WithMany(e => e.Rounds)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
