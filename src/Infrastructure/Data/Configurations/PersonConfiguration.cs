using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Phone)
            .HasMaxLength(20);

        // Configure RowVersion as BLOB but disable concurrency token for many-to-many compatibility
        builder.Property(p => p.RowVersion)
            .HasColumnType("BLOB")
            .IsRequired(false);
            // .IsConcurrencyToken() - Disabled due to EF Core conflicts with many-to-many updates

        // Configure many-to-many relationship with Role
        builder.HasMany(p => p.Roles)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PersonRole",
                j => j.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                j => j.HasOne<Person>().WithMany().HasForeignKey("PersonId"),
                j =>
                {
                    j.HasKey("PersonId", "RoleId");
                    j.ToTable("PersonRoles");
                });

        builder.ToTable("People");
    }
}
