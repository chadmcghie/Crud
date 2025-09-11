using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    private TestLogCapture? _logCapture;

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

            // Register cache services for integration tests
            // Create a minimal configuration for caching (in-memory only for tests)
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Caching:UseRedis"] = "false",
                ["Caching:UseLazyCache"] = "false",
                ["Caching:UseComposite"] = "false",
                ["Caching:DefaultExpirationMinutes"] = "5",
                ["OutputCaching:Disabled"] = "true"  // Disable output caching for conditional request tests
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Use the same caching configuration as production but with in-memory only
            services.AddCachingServices(configuration);

            // Configure logging for better error debugging in tests
            _logCapture = new TestLogCapture("IntegrationTest");
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.AddDebug();
                builder.AddProvider(new TestLogCaptureProvider(_logCapture));
                // Enable detailed logging for errors and critical issues
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                builder.AddFilter("System", LogLevel.Warning);
                // Enable error-level logging for application components
                builder.AddFilter("Api", LogLevel.Error);
                builder.AddFilter("App", LogLevel.Error);
                builder.AddFilter("Infrastructure", LogLevel.Error);
                // Always show exceptions and global exception handling
                builder.AddFilter("Api.Middleware.GlobalExceptionHandlingMiddleware", LogLevel.Debug);
            });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Gets the test log capture instance for debugging server-side errors
    /// </summary>
    public TestLogCapture? LogCapture => _logCapture;

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

    public async Task SetUserRoleAsync(string email, string role)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // The Roles field stores comma-separated values
        // For testing, we'll just set it to "User,Admin" if Admin is requested
        var rolesValue = role == "Admin" ? "User,Admin" : "User";

        // Use raw SQL to avoid EF issues with owned entities
        var sql = @"
            UPDATE Users 
            SET Roles = @p0, UpdatedAt = @p1
            WHERE Email = @p2";

        await dbContext.Database.ExecuteSqlRawAsync(sql, rolesValue, DateTime.UtcNow, email);
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
