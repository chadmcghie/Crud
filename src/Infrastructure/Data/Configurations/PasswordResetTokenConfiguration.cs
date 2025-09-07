using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(prt => prt.Id);

        builder.Property(prt => prt.Id)
            .ValueGeneratedOnAdd();

        builder.Property(prt => prt.Token)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(prt => prt.UserId)
            .IsRequired();

        builder.Property(prt => prt.CreatedAt)
            .IsRequired();

        builder.Property(prt => prt.ExpiresAt)
            .IsRequired();

        builder.Property(prt => prt.IsUsed)
            .IsRequired();

        builder.Property(prt => prt.UsedAt);

        // Concurrency control
        builder.Property(prt => prt.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Indexes for performance
        builder.HasIndex(prt => prt.Token)
            .IsUnique();

        builder.HasIndex(prt => prt.UserId);

        builder.HasIndex(prt => prt.ExpiresAt);

        builder.HasIndex(prt => new { prt.UserId, prt.IsUsed, prt.ExpiresAt });

        // Relationships
        builder.HasOne(prt => prt.User)
            .WithMany()
            .HasForeignKey(prt => prt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(prt => prt.IsExpired);
        builder.Ignore(prt => prt.IsValid);
    }
}
