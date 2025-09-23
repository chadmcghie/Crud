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
/// SQL Server LocalDB implementation of test web application factory
/// Uses SQL Server LocalDB for testing provider-specific behaviors closest to production
/// </summary>
public class SqlServerTestWebApplicationFactory : WebApplicationFactory<Api.Program>, IMultiProviderTestWebApplicationFactory
{
    private readonly int _workerIndex;
    private readonly string _databaseName;
    private readonly string _connectionString;
    private TestLogCapture? _logCapture;

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;
    public string ProviderName => "SqlServer";
    public bool SupportsTransactions => true; // SQL Server supports full ACID transactions
    public bool SupportsForeignKeys => true; // SQL Server enforces FK constraints
    public bool SupportsPersistence => true; // SQL Server persists data to disk

    public SqlServerTestWebApplicationFactory()
    {
        _workerIndex = Environment.GetEnvironmentVariable("WORKER_INDEX") != null
            ? int.Parse(Environment.GetEnvironmentVariable("WORKER_INDEX")!)
            : GenerateUniqueWorkerIndex();

        _databaseName = $"CrudTestDb_SqlServer_{_workerIndex}_{Guid.NewGuid():N}";

        // Use LocalDB for testing - it's lightweight and doesn't require SQL Server installation
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
    }

    private static int GenerateUniqueWorkerIndex()
    {
        var processId = Environment.ProcessId;
        var random = Random.Shared.Next(1000, 9999);
        var ticks = DateTime.UtcNow.Ticks % 10000;
        return Math.Abs((processId + random + (int)ticks).GetHashCode()) % 100000;
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

            // Add SQL Server with unique database name
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_connectionString);
                options.EnableSensitiveDataLogging();
            });

            // Register cache services for integration tests
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Caching:UseRedis"] = "false",
                ["Caching:UseLazyCache"] = "false",
                ["Caching:UseComposite"] = "false",
                ["Caching:DefaultExpirationMinutes"] = "5",
                ["OutputCaching:Disabled"] = "false",
                // Add JWT configuration for authentication tests
                ["Jwt:Secret"] = "TestSecretKey123456789TestSecretKey123456789", // Minimum 32 chars
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            services.AddCachingServices(configuration);

            // Configure logging for test debugging
            _logCapture = new TestLogCapture("SqlServerIntegrationTest");
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.AddDebug();
                builder.AddProvider(new TestLogCaptureProvider(_logCapture));
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                builder.AddFilter("System", LogLevel.Warning);
                builder.AddFilter("Api", LogLevel.Error);
                builder.AddFilter("App", LogLevel.Error);
                builder.AddFilter("Infrastructure", LogLevel.Error);
                builder.AddFilter("Api.Middleware.GlobalExceptionHandlingMiddleware", LogLevel.Debug);
            });
        });

        builder.UseEnvironment("Testing");

        // Set authorization bypass for integration tests
        Environment.SetEnvironmentVariable("BYPASS_AUTHORIZATION_FOR_E2E", "true");
    }

    public TestLogCapture? LogCapture => _logCapture;

    public void EnsureDatabaseCreated()
    {
        try
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Create database and apply migrations
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to initialize SQL Server database '{_databaseName}'. " +
                $"Ensure LocalDB is installed and running. Error: {ex.Message}", ex);
        }
    }

    public async Task ClearDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // For SQL Server, use truncate operations which respect FK constraints
        // Execute in proper order to avoid constraint violations

        try
        {
            // Disable FK constraints temporarily for faster cleanup
            await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            // Clear all data
            await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'DELETE FROM ?'");

            // Re-enable FK constraints
            await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");

            // Reset identity seeds if any
            await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'DBCC CHECKIDENT(''?'', RESEED, 0)'");
        }
        catch
        {
            // Fallback: manually clear entities in dependency order if the above fails
            await ClearAllEntitiesManuallyAsync(context);
        }
    }

    private static async Task ClearAllEntitiesManuallyAsync(ApplicationDbContext context)
    {
        // Clear entities in dependency order to respect FK constraints
        context.Users.RemoveRange(context.Users);
        context.Windows.RemoveRange(context.Windows);
        context.Walls.RemoveRange(context.Walls);
        context.People.RemoveRange(context.People);
        context.Roles.RemoveRange(context.Roles);

        await context.SaveChangesAsync();
    }

    public async Task SetUserRoleAsync(string email, string role)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Use EF Core entity approach instead of raw SQL to leverage the value comparer fix
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        if (user != null)
        {
            // Add the requested role using the domain method
            if (role == "Admin")
            {
                user.AddRole("Admin");
            }

            await dbContext.SaveChangesAsync();
            
            // Clear change tracker to ensure fresh data on next load
            dbContext.ChangeTracker.Clear();
        }
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
            }
            catch (Exception)
            {
                // Ignore cleanup errors during disposal
            }
        }
        base.Dispose(disposing);
    }
}
