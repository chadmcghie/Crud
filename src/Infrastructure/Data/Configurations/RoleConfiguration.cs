using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        // Add unique index on Name
        builder.HasIndex(r => r.Name)
            .IsUnique();

        // Configure concurrency token
        // Note: SQLite doesn't support IsRowVersion() the same way as SQL Server
        // For SQLite compatibility, we'll configure it as a regular byte array
        builder.Property(r => r.RowVersion)
            .HasColumnType("BLOB")
            .IsRequired(false)
            .IsConcurrencyToken();

        builder.ToTable("Roles");
    }
}
