using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Infrastructure.Data;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// SQLite-based implementation of test web application factory
/// Uses file-based SQLite databases for fast, isolated testing
/// </summary>
public class SqliteTestWebApplicationFactory : WebApplicationFactory<Api.Program>, ITestWebApplicationFactory
{
    private static readonly object _lockObject = new object();
    private static bool _databaseInitialized = false;
    private readonly string _databaseName;
    private readonly string _connectionString;

    public SqliteTestWebApplicationFactory()
    {
        // Each test class gets its own unique SQLite database file
        _databaseName = $"IntegrationTests_{GetType().Name}_{Guid.NewGuid():N}.db";
        
        // Use file-based SQLite database for reliable testing
        _connectionString = $"Data Source={_databaseName}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add SQLite with our unique database connection string
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connectionString);
                options.EnableSensitiveDataLogging();
            });

            // Reduce logging noise in tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }

    public void EnsureDatabaseCreated()
    {
        lock (_lockObject)
        {
            if (!_databaseInitialized)
            {
                try
                {
                    using var scope = Services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    // Ensure the database schema is created
                    context.Database.EnsureCreated();
                    
                    _databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to initialize SQLite database '{_databaseName}'. " +
                        $"Error: {ex.Message}", ex);
                }
            }
        }
    }

    public async Task ClearDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Clear in order to respect foreign key constraints
        context.People.RemoveRange(context.People);
        context.Roles.RemoveRange(context.Roles);
        context.Walls.RemoveRange(context.Walls);
        context.Windows.RemoveRange(context.Windows);
        
        await context.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Clean up the test database
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureDeleted();
                
                // Also delete the SQLite file if it exists
                if (File.Exists(_databaseName))
                {
                    File.Delete(_databaseName);
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
        base.Dispose(disposing);
    }
}
