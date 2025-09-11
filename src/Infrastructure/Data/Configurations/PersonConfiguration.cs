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

        // Configure RowVersion as optional property without concurrency token
        // Can be enabled for concurrency control in the future if needed
        builder.Property(p => p.RowVersion)
            .HasColumnType("BLOB")
            .IsRequired(false);
        // .IsConcurrencyToken(); // Disabled to allow updates without strict concurrency control

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
