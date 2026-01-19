using DraftApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DraftApp.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for PlayerEntity.
/// </summary>
public class PlayerConfiguration : IEntityTypeConfiguration<PlayerEntity>
{
    public void Configure(EntityTypeBuilder<PlayerEntity> builder)
    {
        builder.ToTable("Players");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.OpponentsJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.LastPlayedRoundJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.PinHash)
            .HasMaxLength(200);

        builder.Property(p => p.PlayerToken)
            .HasMaxLength(100);

        builder.HasIndex(p => p.PlayerToken)
            .IsUnique()
            .HasFilter("[PlayerToken] IS NOT NULL");

        // Unique constraint: seed per event
        builder.HasIndex(p => new { p.EventId, p.Seed })
            .IsUnique();

        // Relationship to Event
        builder.HasOne(p => p.Event)
            .WithMany(e => e.Players)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
