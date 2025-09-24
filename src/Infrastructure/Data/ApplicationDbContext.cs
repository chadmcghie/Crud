using Domain.Entities;
using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

        // Apply global query filters for soft delete
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to all entities that inherit from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var propertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));
                var isDeletedProperty = Expression.Call(propertyMethod, parameter, Expression.Constant("IsDeleted"));
                var compareExpression = Expression.MakeBinary(ExpressionType.Equal, isDeletedProperty, Expression.Constant(false));
                var lambda = Expression.Lambda(compareExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }

            // Also apply to User entity which doesn't inherit from BaseEntity but implements ISoftDeletable
            if (entityType.ClrType == typeof(User))
            {
                var parameter = Expression.Parameter(typeof(User), "u");
                var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
                var compareExpression = Expression.MakeBinary(ExpressionType.Equal, isDeletedProperty, Expression.Constant(false));
                var lambda = Expression.Lambda<Func<User, bool>>(compareExpression, parameter);

                modelBuilder.Entity<User>().HasQueryFilter(lambda);
            }
        }
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
            if (entry.Entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
