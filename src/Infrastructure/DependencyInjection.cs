using App.Abstractions;
using Infrastructure.Data;
using Infrastructure.Repositories.EntityFramework;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{



    /// <summary>
    /// Adds infrastructure services using Entity Framework with SQL Server
    /// Suitable for production scenarios
    /// </summary>
    public static IServiceCollection AddInfrastructureEntityFrameworkSqlServer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IWallRepository, EfWallRepository>();
        services.AddScoped<IWindowRepository, EfWindowRepository>();

        // Add database test service for testing scenarios
        services.AddScoped<DatabaseTestService>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services using Entity Framework with SQLite
    /// Suitable for development, testing, and lightweight production scenarios
    /// </summary>
    public static IServiceCollection AddInfrastructureEntityFrameworkSqlite(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IWallRepository, EfWallRepository>();
        services.AddScoped<IWindowRepository, EfWindowRepository>();

        // Add database test service for testing scenarios
        services.AddScoped<DatabaseTestService>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations
    /// Call this during application startup for Entity Framework configurations
    /// </summary>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            await context.Database.EnsureCreatedAsync();
        }
    }
}
