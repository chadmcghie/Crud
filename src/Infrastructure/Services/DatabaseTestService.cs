using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing database operations during testing.
/// Provides database reset and seeding capabilities using EF Core for SQLite.
/// </summary>
public class DatabaseTestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseTestService> _logger;

    public DatabaseTestService(ApplicationDbContext context, ILogger<DatabaseTestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database service. For SQLite, no special initialization needed.
    /// </summary>
    public async Task InitializeAsync()
    {
        var connectionString = _context.Database.GetConnectionString();
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not available");
        }

        _logger.LogInformation("Initializing database service for SQLite: {ConnectionString}", 
            connectionString.Replace("Password=", "Password=***"));

        // Ensure database exists
        await _context.Database.EnsureCreatedAsync();

        _logger.LogInformation("Database service initialized successfully");
    }

    /// <summary>
    /// Resets the database to a clean state, removing all data while preserving schema.
    /// Uses EF Core since Respawn doesn't fully support SQLite.
    /// </summary>
    public async Task ResetDatabaseAsync(int workerIndex)
    {
        _logger.LogInformation("Resetting database for worker {WorkerIndex} using EF Core", workerIndex);

        try
        {
            // Delete all data from tables in correct order (respecting foreign keys)
            
            // First, delete dependent entities
            _context.People.RemoveRange(_context.People);
            
            // Then delete referenced entities
            _context.Roles.RemoveRange(_context.Roles);
            _context.Windows.RemoveRange(_context.Windows);
            _context.Walls.RemoveRange(_context.Walls);
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Database reset completed for worker {WorkerIndex}", workerIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database for worker {WorkerIndex}", workerIndex);
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with initial test data.
    /// </summary>
    public async Task SeedDatabaseAsync(int workerIndex)
    {
        _logger.LogInformation("Seeding database for worker {WorkerIndex}", workerIndex);

        // Add seed data if tables are empty
        await SeedRolesAsync();
        await SeedPeopleAsync();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeding completed for worker {WorkerIndex}", workerIndex);
    }

    /// <summary>
    /// Gets database statistics for debugging purposes.
    /// </summary>
    public async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        return new DatabaseStats
        {
            PeopleCount = await _context.People.CountAsync(),
            RolesCount = await _context.Roles.CountAsync(),
            WallsCount = await _context.Walls.CountAsync(),
            WindowsCount = await _context.Windows.CountAsync(),
            ConnectionString = _context.Database.GetConnectionString()?.Replace("Password=", "Password=***"),
            CanConnect = await _context.Database.CanConnectAsync()
        };
    }

    private async Task SeedRolesAsync()
    {
        if (!await _context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new Domain.Entities.Role { Name = "Administrator", Description = "System administrator with full access" },
                new Domain.Entities.Role { Name = "User", Description = "Standard user with limited access" },
                new Domain.Entities.Role { Name = "Guest", Description = "Guest user with read-only access" }
            };

            _context.Roles.AddRange(roles);
            _logger.LogDebug("Added {Count} seed roles", roles.Length);
        }
    }

    private async Task SeedPeopleAsync()
    {
        if (!await _context.People.AnyAsync())
        {
            var people = new[]
            {
                new Domain.Entities.Person { FullName = "John Doe" },
                new Domain.Entities.Person { FullName = "Jane Smith" }
            };

            _context.People.AddRange(people);
            _logger.LogDebug("Added {Count} seed people", people.Length);
        }
    }
}

/// <summary>
/// Database statistics for debugging and monitoring.
/// </summary>
public class DatabaseStats
{
    public int PeopleCount { get; set; }
    public int RolesCount { get; set; }
    public int WallsCount { get; set; }
    public int WindowsCount { get; set; }
    public string? ConnectionString { get; set; }
    public bool CanConnect { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
