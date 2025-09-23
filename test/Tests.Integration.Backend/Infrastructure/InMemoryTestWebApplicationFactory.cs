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
/// In-Memory database implementation of test web application factory
/// Uses Entity Framework's InMemory provider for ultra-fast testing without persistence
/// </summary>
public class InMemoryTestWebApplicationFactory : WebApplicationFactory<Api.Program>, IMultiProviderTestWebApplicationFactory
{
    private readonly int _workerIndex;
    private readonly string _databaseName;
    private TestLogCapture? _logCapture;

    public DatabaseProvider Provider => DatabaseProvider.InMemory;
    public string ProviderName => "InMemory";
    public bool SupportsTransactions => false; // InMemory provider doesn't support real transactions
    public bool SupportsForeignKeys => false; // InMemory provider doesn't enforce FK constraints
    public bool SupportsPersistence => false; // InMemory data is lost when context is disposed

    public InMemoryTestWebApplicationFactory()
    {
        // Generate unique database name per factory instance to ensure isolation
        _workerIndex = Environment.GetEnvironmentVariable("WORKER_INDEX") != null
            ? int.Parse(Environment.GetEnvironmentVariable("WORKER_INDEX")!)
            : GenerateUniqueWorkerIndex();

        _databaseName = $"TestDb_InMemory_{_workerIndex}_{Guid.NewGuid():N}";
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

            // Add InMemory database with unique name for isolation
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Register cache services for integration tests (same as SQLite factory)
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Caching:UseRedis"] = "false",
                ["Caching:UseLazyCache"] = "false",
                ["Caching:UseComposite"] = "false",
                ["Caching:DefaultExpirationMinutes"] = "5",
                ["OutputCaching:Disabled"] = "false"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            services.AddCachingServices(configuration);

            // Configure logging for test debugging
            _logCapture = new TestLogCapture("InMemoryIntegrationTest");
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
    }

    public TestLogCapture? LogCapture => _logCapture;

    public void EnsureDatabaseCreated()
    {
        // InMemory database is automatically created, but ensure context is available
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // EnsureCreated is a no-op for InMemory, but this verifies the context works
        context.Database.EnsureCreated();
    }

    public async Task ClearDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // For InMemory provider, manually clear all entities
        // This is faster than recreating the context and maintains the same database instance
        await ClearAllEntitiesAsync(context);
    }

    private static async Task ClearAllEntitiesAsync(ApplicationDbContext context)
    {
        // Clear all entities in dependency order (children first, then parents)
        // This avoids FK constraint issues even though InMemory doesn't enforce them

        // Clear User entities (they don't have dependencies)
        context.Users.RemoveRange(context.Users);

        // Clear Window entities (depend on Wall)
        context.Windows.RemoveRange(context.Windows);

        // Clear Wall entities (depend on Person)
        context.Walls.RemoveRange(context.Walls);

        // Clear Person entities and their roles (root entities)
        context.People.RemoveRange(context.People);
        context.Roles.RemoveRange(context.Roles);

        await context.SaveChangesAsync();
    }

    public async Task SetUserRoleAsync(string email, string role)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user != null)
        {
            // Add Admin role if requested (User role is already added by default)
            if (role == "Admin")
            {
                user.AddRole("Admin");
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
