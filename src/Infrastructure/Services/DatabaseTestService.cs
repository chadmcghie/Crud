using System.Data.Common;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respawn;
using Respawn.Graph;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing database operations during testing.
/// Provides database reset and seeding capabilities using EF Core for SQLite.
/// </summary>
public class DatabaseTestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseTestService> _logger;
    private readonly object _respawnerLock = new();
    private static readonly SemaphoreSlim _databaseMutex = new SemaphoreSlim(1, 1);

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
    /// In CI/Docker environments, uses file deletion for better performance.
    /// Uses Respawn when possible, falls back to EF Core for SQLite compatibility.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    /// <param name="seedData">Whether to seed initial test data after reset (default: false)</param>
    public async Task ResetDatabaseAsync(int workerIndex, bool seedData = false)
    {
        _logger.LogInformation("Resetting database for worker {WorkerIndex}", workerIndex);

        try
        {
            var connectionString = _context.Database.GetConnectionString();

            // In CI/Docker environments, use file deletion for much better performance
            if (Environment.GetEnvironmentVariable("CI") == "true" &&
                !string.IsNullOrEmpty(connectionString))
            {
                await ResetByFileDeletionAsync(connectionString, workerIndex, seedData);
                return;
            }

            // Try to use Respawn for more reliable database cleanup
            if (!string.IsNullOrEmpty(connectionString) &&
                await TryResetWithRespawnAsync(connectionString, workerIndex, seedData))
            {
                return;
            }

            // Fallback to EF Core cleanup with improved transaction handling
            await ResetWithEfCoreAsync(workerIndex, seedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset database for worker {WorkerIndex}", workerIndex);
            throw;
        }
    }

    /// <summary>
    /// Resets database by deleting the file and recreating it (fastest for CI/Docker)
    /// </summary>
    private async Task ResetByFileDeletionAsync(string connectionString, int workerIndex, bool seedData)
    {
        _logger.LogInformation("Resetting database via file deletion for worker {WorkerIndex} in CI environment", workerIndex);
        _logger.LogInformation("Connection string: {ConnectionString}", connectionString?.Replace("Password=", "Password=***"));
        var startTime = DateTime.UtcNow;

        // Use mutex to ensure only one reset operation at a time
        _logger.LogInformation("Acquiring database mutex for worker {WorkerIndex}...", workerIndex);
        await _databaseMutex.WaitAsync();

        try
        {
            _logger.LogInformation("Database mutex acquired for worker {WorkerIndex}", workerIndex);
            // Log current database state
            _logger.LogInformation("Current database state - CanConnect: {CanConnect}", await _context.Database.CanConnectAsync());

            // Try to close the connection first
            _logger.LogInformation("[Phase 0] Closing database connection for worker {WorkerIndex}...", workerIndex);
            var closeStart = DateTime.UtcNow;
            await _context.Database.CloseConnectionAsync();
            var closeTime = (DateTime.UtcNow - closeStart).TotalMilliseconds;
            _logger.LogInformation("[Phase 0] Connection closed in {Ms}ms", closeTime);

            // Use EF Core's built-in methods which handle all the complexity
            // This properly closes connections, deletes files, and handles locks
            _logger.LogInformation("[Phase 1] Starting EnsureDeletedAsync for worker {WorkerIndex}...", workerIndex);
            var deleteStart = DateTime.UtcNow;
            await _context.Database.EnsureDeletedAsync();
            var deleteTime = (DateTime.UtcNow - deleteStart).TotalMilliseconds;
            _logger.LogInformation("[Phase 1] EnsureDeletedAsync completed in {Ms}ms", deleteTime);

            // Small delay to ensure file system has released the file
            await Task.Delay(100);

            // Recreate the database with schema
            _logger.LogInformation("[Phase 2] Starting EnsureCreatedAsync for worker {WorkerIndex}...", workerIndex);
            var createStart = DateTime.UtcNow;
            await _context.Database.EnsureCreatedAsync();
            var createTime = (DateTime.UtcNow - createStart).TotalMilliseconds;
            _logger.LogInformation("[Phase 2] EnsureCreatedAsync completed in {Ms}ms", createTime);

            // Optionally seed the database with initial data
            double seedTime = 0;
            if (seedData)
            {
                _logger.LogInformation("[Phase 3] Starting database seeding for worker {WorkerIndex}...", workerIndex);
                var seedStart = DateTime.UtcNow;
                await SeedRolesAsync();
                await SeedPeopleAsync();
                await _context.SaveChangesWithRetryAsync();
                seedTime = (DateTime.UtcNow - seedStart).TotalMilliseconds;
                _logger.LogInformation("[Phase 3] Database seeding completed in {Ms}ms", seedTime);
            }
            else
            {
                _logger.LogInformation("[Phase 3] Skipping database seeding as requested for worker {WorkerIndex}", workerIndex);
            }

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            if (seedData)
            {
                _logger.LogInformation("Database reset via file deletion completed for worker {WorkerIndex} in {Ms}ms (Close: {CloseMs}ms, Delete: {DeleteMs}ms, Create: {CreateMs}ms, Seed: {SeedMs}ms)",
                    workerIndex, totalTime, closeTime, deleteTime, createTime, seedTime);
            }
            else
            {
                _logger.LogInformation("Database reset via file deletion completed for worker {WorkerIndex} in {Ms}ms (Close: {CloseMs}ms, Delete: {DeleteMs}ms, Create: {CreateMs}ms)",
                    workerIndex, totalTime, closeTime, deleteTime, createTime);
            }
        }
        catch (Exception ex)
        {
            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "File deletion reset failed for worker {WorkerIndex} after {Ms}ms",
                workerIndex, totalTime);
            throw;
        }
        finally
        {
            _logger.LogInformation("Releasing database mutex for worker {WorkerIndex}", workerIndex);
            _databaseMutex.Release();
        }
    }

    /// <summary>
    /// Attempts to reset database using Respawn (more reliable but limited SQLite support)
    /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
    private Task<bool> TryResetWithRespawnAsync(string connectionString, int workerIndex, bool seedData)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        try
        {
            // For SQLite, Respawn has limited support and can cause issues
            // Skip Respawn for SQLite and use EF Core cleanup instead
            _logger.LogDebug("Skipping Respawn for SQLite database, using EF Core cleanup for worker {WorkerIndex}", workerIndex);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Respawn reset failed for worker {WorkerIndex}, falling back to EF Core: {Error}",
                workerIndex, ex.Message);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Resets database using EF Core with optimized bulk operations
    /// </summary>
    private async Task ResetWithEfCoreAsync(int workerIndex, bool seedData)
    {
        _logger.LogDebug("Resetting database for worker {WorkerIndex} using EF Core", workerIndex);
        var startTime = DateTime.UtcNow;

        try
        {
            // For SQLite, we can use more efficient bulk operations without explicit transactions
            // since SQLite handles this automatically and transactions can cause locking issues

            // Use ExecuteDeleteAsync for better performance (EF Core 7+)
            // This generates efficient DELETE statements instead of loading entities
            // Note: Removed AnyAsync() checks as they were causing performance issues in test scenarios

            _logger.LogDebug("Deleting People for worker {WorkerIndex}...", workerIndex);
            var peopleStart = DateTime.UtcNow;
            await _context.People.ExecuteDeleteAsync();
            _logger.LogDebug("Deleted People in {Ms}ms", (DateTime.UtcNow - peopleStart).TotalMilliseconds);

            _logger.LogDebug("Deleting Roles for worker {WorkerIndex}...", workerIndex);
            var rolesStart = DateTime.UtcNow;
            await _context.Roles.ExecuteDeleteAsync();
            _logger.LogDebug("Deleted Roles in {Ms}ms", (DateTime.UtcNow - rolesStart).TotalMilliseconds);

            _logger.LogDebug("Deleting Windows for worker {WorkerIndex}...", workerIndex);
            var windowsStart = DateTime.UtcNow;
            await _context.Windows.ExecuteDeleteAsync();
            _logger.LogDebug("Deleted Windows in {Ms}ms", (DateTime.UtcNow - windowsStart).TotalMilliseconds);

            _logger.LogDebug("Deleting Walls for worker {WorkerIndex}...", workerIndex);
            var wallsStart = DateTime.UtcNow;
            await _context.Walls.ExecuteDeleteAsync();
            _logger.LogDebug("Deleted Walls in {Ms}ms", (DateTime.UtcNow - wallsStart).TotalMilliseconds);

            // Optionally seed data after cleanup
            if (seedData)
            {
                _logger.LogDebug("Seeding database for worker {WorkerIndex}...", workerIndex);
                await SeedRolesAsync();
                await SeedPeopleAsync();
                await _context.SaveChangesWithRetryAsync();
            }

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Database reset completed using EF Core for worker {WorkerIndex} in {Ms}ms (seedData: {SeedData})", workerIndex, totalTime, seedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EF Core database reset failed for worker {WorkerIndex} after {Ms}ms",
                workerIndex, (DateTime.UtcNow - startTime).TotalMilliseconds);
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

        await _context.SaveChangesWithRetryAsync();

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

    /// <summary>
    /// Validates that the database is in a clean state before test execution
    /// </summary>
    public async Task<DatabaseValidationResult> ValidatePreTestStateAsync(int workerIndex)
    {
        _logger.LogDebug("Validating pre-test database state for worker {WorkerIndex}", workerIndex);

        var stats = await GetDatabaseStatsAsync();
        var issues = new List<string>();

        // Check if database is empty
        if (stats.PeopleCount > 0)
            issues.Add($"People table contains {stats.PeopleCount} records");
        if (stats.RolesCount > 0)
            issues.Add($"Roles table contains {stats.RolesCount} records");
        if (stats.WallsCount > 0)
            issues.Add($"Walls table contains {stats.WallsCount} records");
        if (stats.WindowsCount > 0)
            issues.Add($"Windows table contains {stats.WindowsCount} records");

        // Check database connectivity
        if (!stats.CanConnect)
            issues.Add("Cannot connect to database");

        var result = new DatabaseValidationResult
        {
            WorkerIndex = workerIndex,
            IsValid = issues.Count == 0,
            Issues = issues,
            Stats = stats,
            ValidationType = "PreTest"
        };

        if (!result.IsValid)
        {
            _logger.LogWarning("Pre-test validation failed for worker {WorkerIndex}: {Issues}",
                workerIndex, string.Join(", ", issues));
        }
        else
        {
            _logger.LogDebug("Pre-test validation passed for worker {WorkerIndex}", workerIndex);
        }

        return result;
    }

    /// <summary>
    /// Validates the database state after test execution to ensure proper cleanup
    /// </summary>
    public async Task<DatabaseValidationResult> ValidatePostTestStateAsync(int workerIndex)
    {
        _logger.LogDebug("Validating post-test database state for worker {WorkerIndex}", workerIndex);

        var stats = await GetDatabaseStatsAsync();
        var issues = new List<string>();

        // Check if database was properly cleaned up
        if (stats.PeopleCount > 0)
            issues.Add($"People table not cleaned up: {stats.PeopleCount} records remain");
        if (stats.RolesCount > 0)
            issues.Add($"Roles table not cleaned up: {stats.RolesCount} records remain");
        if (stats.WallsCount > 0)
            issues.Add($"Walls table not cleaned up: {stats.WallsCount} records remain");
        if (stats.WindowsCount > 0)
            issues.Add($"Windows table not cleaned up: {stats.WindowsCount} records remain");

        // Check database connectivity
        if (!stats.CanConnect)
            issues.Add("Database connection lost during test");

        var result = new DatabaseValidationResult
        {
            WorkerIndex = workerIndex,
            IsValid = issues.Count == 0,
            Issues = issues,
            Stats = stats,
            ValidationType = "PostTest"
        };

        if (!result.IsValid)
        {
            _logger.LogError("Post-test validation failed for worker {WorkerIndex}: {Issues}",
                workerIndex, string.Join(", ", issues));
        }
        else
        {
            _logger.LogDebug("Post-test validation passed for worker {WorkerIndex}", workerIndex);
        }

        return result;
    }

    /// <summary>
    /// Performs database integrity verification
    /// </summary>
    public async Task<bool> VerifyDatabaseIntegrityAsync(int workerIndex)
    {
        try
        {
            _logger.LogDebug("Verifying database integrity for worker {WorkerIndex}", workerIndex);

            // Test basic database operations
            await _context.Database.CanConnectAsync();
            await _context.People.CountAsync();
            await _context.Roles.CountAsync();
            await _context.Walls.CountAsync();
            await _context.Windows.CountAsync();

            _logger.LogDebug("Database integrity verification passed for worker {WorkerIndex}", workerIndex);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database integrity verification failed for worker {WorkerIndex}", workerIndex);
            return false;
        }
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

/// <summary>
/// Result of database validation operations
/// </summary>
public class DatabaseValidationResult
{
    public int WorkerIndex { get; set; }
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new List<string>();
    public DatabaseStats Stats { get; set; } = null!;
    public string ValidationType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
