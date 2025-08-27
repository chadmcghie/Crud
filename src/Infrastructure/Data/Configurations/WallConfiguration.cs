using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WallConfiguration : IEntityTypeConfiguration<Wall>
{
    public void Configure(EntityTypeBuilder<Wall> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(1000);

        builder.Property(w => w.Length)
            .HasPrecision(10, 2);

        builder.Property(w => w.Height)
            .HasPrecision(10, 2);

        builder.Property(w => w.Thickness)
            .HasPrecision(10, 2);

        builder.Property(w => w.AssemblyType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.AssemblyDetails)
            .HasMaxLength(1000);

        builder.Property(w => w.RValue)
            .HasPrecision(10, 2);

        builder.Property(w => w.UValue)
            .HasPrecision(10, 4);

        builder.Property(w => w.MaterialLayers)
            .HasMaxLength(2000);

        builder.Property(w => w.Orientation)
            .HasMaxLength(50);

        builder.Property(w => w.Location)
            .HasMaxLength(100);

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(w => w.UpdatedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUniversalTime() : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null);

        builder.ToTable("Walls");
    }
}