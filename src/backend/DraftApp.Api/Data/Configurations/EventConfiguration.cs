using DraftApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DraftApp.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for EventEntity.
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.JoinCode)
            .HasMaxLength(10);

        builder.HasIndex(e => e.JoinCode)
            .IsUnique()
            .HasFilter("[JoinCode] IS NOT NULL");

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.Format)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsConcurrencyToken();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.HostPinHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.HostToken)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.HostToken)
            .IsUnique();

        // Relationships configured in child entities
    }
}
