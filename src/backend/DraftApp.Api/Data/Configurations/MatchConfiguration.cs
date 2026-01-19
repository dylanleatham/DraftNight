using DraftApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DraftApp.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for MatchEntity.
/// </summary>
public class MatchConfiguration : IEntityTypeConfiguration<MatchEntity>
{
    public void Configure(EntityTypeBuilder<MatchEntity> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MatchCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.RoundNumber)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired();

        // Index for match code lookup within round
        builder.HasIndex(m => new { m.RoundId, m.MatchCode })
            .IsUnique();

        // Relationship to Round
        builder.HasOne(m => m.Round)
            .WithMany(r => r.Matches)
            .HasForeignKey(m => m.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to Player A (required)
        builder.HasOne(m => m.PlayerA)
            .WithMany(p => p.MatchesAsPlayerA)
            .HasForeignKey(m => m.PlayerAId)
            .OnDelete(DeleteBehavior.NoAction);

        // Relationship to Player B (optional for BYE/sit)
        builder.HasOne(m => m.PlayerB)
            .WithMany(p => p.MatchesAsPlayerB)
            .HasForeignKey(m => m.PlayerBId)
            .OnDelete(DeleteBehavior.NoAction);

        // Relationship to Winner (optional until finalized)
        builder.HasOne(m => m.Winner)
            .WithMany(p => p.MatchesWon)
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
