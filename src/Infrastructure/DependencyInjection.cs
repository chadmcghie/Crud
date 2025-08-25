using App.Abstractions;
using Infrastructure.Data;
using Infrastructure.Repositories.EntityFramework;
using Infrastructure.Repositories.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services using in-memory repositories (no database)
    /// </summary>
    public static IServiceCollection AddInfrastructureInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IPersonRepository, InMemoryPersonRepository>();
        services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
        services.AddSingleton<IWallRepository, InMemoryWallRepository>();
        services.AddSingleton<IWindowRepository, InMemoryWindowRepository>();
        return services;
    }

    /// <summary>
    /// Adds infrastructure services using Entity Framework with in-memory database
    /// Suitable for testing scenarios where you want EF behavior but no persistent storage
    /// </summary>
    public static IServiceCollection AddInfrastructureEntityFrameworkInMemory(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("CrudAppInMemory"));

        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IWallRepository, EfWallRepository>();
        services.AddScoped<IWindowRepository, EfWindowRepository>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services using Entity Framework with SQL Server
    /// Suitable for production and end-to-end testing scenarios
    /// </summary>
    public static IServiceCollection AddInfrastructureEntityFrameworkSqlServer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IWallRepository, EfWallRepository>();
        services.AddScoped<IWindowRepository, EfWindowRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations
    /// Call this during application startup for SQL Server configurations
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
