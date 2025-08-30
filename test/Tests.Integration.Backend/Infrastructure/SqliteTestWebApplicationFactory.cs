using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Infrastructure.Data;
using Infrastructure.Services;
using System.IO;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// SQLite-based implementation of test web application factory
/// Uses file-based SQLite databases for fast, isolated testing
/// </summary>
public class SqliteTestWebApplicationFactory : WebApplicationFactory<Api.Program>, ITestWebApplicationFactory
{
    private static readonly object _lockObject = new object();
    private static bool _databaseInitialized = false;
    private readonly int _workerIndex;
    private readonly TestDatabaseFactory _databaseFactory;
    private string? _databasePath;
    private string? _connectionString;

    public SqliteTestWebApplicationFactory()
    {
        // Get worker index from environment or generate a unique one per factory instance
        _workerIndex = Environment.GetEnvironmentVariable("WORKER_INDEX") != null 
            ? int.Parse(Environment.GetEnvironmentVariable("WORKER_INDEX")!) 
            : GenerateUniqueWorkerIndex(); // Generate unique index per factory instance
            
        _databaseFactory = new TestDatabaseFactory(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<TestDatabaseFactory>());
    }

    private static int GenerateUniqueWorkerIndex()
    {
        // Combine process ID with a random number and current ticks for uniqueness
        var processId = Environment.ProcessId;
        var random = Random.Shared.Next(1000, 9999);
        var ticks = DateTime.UtcNow.Ticks % 10000;
        return Math.Abs((processId + random + (int)ticks).GetHashCode()) % 100000;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set content root to the Api project output directory where appsettings files are located
        var contentRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../src/Api/bin/Release/net8.0"));
        if (!Directory.Exists(contentRoot))
        {
            // Fallback to default if the Release path doesn't exist
            contentRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../../src/Api"));
        }
        
        builder.UseContentRoot(contentRoot);
        builder.ConfigureServices(services =>
        {
            // Initialize worker-specific database if not already done
            if (string.IsNullOrEmpty(_connectionString))
            {
                _databasePath = _databaseFactory.CreateWorkerDatabaseAsync(_workerIndex).Result;
                _connectionString = $"Data Source={_databasePath}";
            }

            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add SQLite with our worker-specific database connection string
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connectionString);
                options.EnableSensitiveDataLogging();
            });

            // Register the TestDatabaseFactory as a service
            services.AddSingleton(_databaseFactory);
            
            // Register DatabaseTestService for improved database cleanup
            services.AddScoped<DatabaseTestService>();

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
                    
                    // Ensure the database schema is created (should already be done by TestDatabaseFactory)
                    context.Database.EnsureCreated();
                    
                    _databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to initialize SQLite database for worker {_workerIndex}. " +
                        $"Error: {ex.Message}", ex);
                }
            }
        }
    }

    public async Task ClearDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseTestService>();
        
        // Use the improved database reset logic with proper transaction handling
        await databaseService.ResetDatabaseAsync(_workerIndex);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Clean up the worker-specific database
                _databaseFactory.CleanupWorkerDatabaseAsync(_workerIndex).Wait();
            }
            catch (Exception)
            {
                // Ignore cleanup errors during disposal
            }
        }
        base.Dispose(disposing);
    }
}
