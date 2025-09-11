using Domain.Entities;
using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Person> People { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Wall> Walls { get; set; } = null!;
    public DbSet<Window> Windows { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Person person)
            {
                person.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Role role)
            {
                role.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Wall wall)
            {
                wall.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Window window)
            {
                window.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
