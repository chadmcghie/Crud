using Domain.Entities.Authentication;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        // Configure Email value object
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();
            
            email.HasIndex(e => e.Value)
                .IsUnique();
        });

        // Configure PasswordHash value object
        builder.OwnsOne(u => u.PasswordHash, password =>
        {
            password.Property(p => p.Value)
                .HasColumnName("PasswordHash")
                .HasMaxLength(256)
                .IsRequired();
        });

        // Configure Roles collection as JSON
        builder.Property(u => u.Roles)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet())
            .HasMaxLength(500);

        // Configure timestamps
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();
        
        // Configure RowVersion for concurrency
        builder.Property(u => u.RowVersion)
            .IsConcurrencyToken();

        // Configure relationship with RefreshTokens
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes
        builder.HasIndex(u => u.CreatedAt);
    }
}