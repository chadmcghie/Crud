using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WindowConfiguration : IEntityTypeConfiguration<Window>
{
    public void Configure(EntityTypeBuilder<Window> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(1000);

        builder.Property(w => w.Width)
            .HasPrecision(10, 2);

        builder.Property(w => w.Height)
            .HasPrecision(10, 2);

        builder.Property(w => w.Area)
            .HasPrecision(10, 2);

        builder.Property(w => w.FrameType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.FrameDetails)
            .HasMaxLength(500);

        builder.Property(w => w.GlazingType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.GlazingDetails)
            .HasMaxLength(500);

        builder.Property(w => w.UValue)
            .HasPrecision(10, 4);

        builder.Property(w => w.SolarHeatGainCoefficient)
            .HasPrecision(5, 3);

        builder.Property(w => w.VisibleTransmittance)
            .HasPrecision(5, 3);

        builder.Property(w => w.AirLeakage)
            .HasPrecision(10, 4);

        builder.Property(w => w.EnergyStarRating)
            .HasMaxLength(50);

        builder.Property(w => w.NFRCRating)
            .HasMaxLength(50);

        builder.Property(w => w.Orientation)
            .HasMaxLength(50);

        builder.Property(w => w.Location)
            .HasMaxLength(100);

        builder.Property(w => w.InstallationType)
            .HasMaxLength(50);

        builder.Property(w => w.OperationType)
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

        builder.ToTable("Windows");
    }
}
