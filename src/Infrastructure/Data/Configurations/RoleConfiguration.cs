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
        // Configure RowVersion with database-specific type
        // EF Core will use appropriate type based on provider (BLOB for SQLite, VARBINARY for SQL Server)
        builder.Property(r => r.RowVersion)
            .IsRequired(false)
            .IsConcurrencyToken();

        builder.ToTable("Roles");
    }
}
