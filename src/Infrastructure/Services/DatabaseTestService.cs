using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respawn;
using Respawn.Graph;
using System.Data.Common;

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
    public async Task ResetDatabaseAsync(int workerIndex)
    {
        _logger.LogInformation("Resetting database for worker {WorkerIndex}", workerIndex);

        try
        {
            var connectionString = _context.Database.GetConnectionString();
            
            // In CI/Docker environments, use file deletion for much better performance
            if (Environment.GetEnvironmentVariable("CI") == "true" && 
                !string.IsNullOrEmpty(connectionString))
            {
                await ResetByFileDeletionAsync(connectionString, workerIndex);
                return;
            }
            
            // Try to use Respawn for more reliable database cleanup
            if (!string.IsNullOrEmpty(connectionString) && 
                await TryResetWithRespawnAsync(connectionString, workerIndex))
            {
                return;
            }
            
            // Fallback to EF Core cleanup with improved transaction handling
            await ResetWithEfCoreAsync(workerIndex);
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
    private async Task ResetByFileDeletionAsync(string connectionString, int workerIndex)
    {
        _logger.LogInformation("Resetting database via file deletion for worker {WorkerIndex} in CI environment", workerIndex);
        var startTime = DateTime.UtcNow;

        try
        {
            // Extract database file path from connection string
            var dbPath = ExtractDatabasePath(connectionString);
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new InvalidOperationException($"Could not extract database path from connection string: {connectionString}");
            }

            _logger.LogDebug("Database path: {Path}", dbPath);

            // Close all connections to the database
            await _context.Database.CloseConnectionAsync();
            _context.Dispose();
            
            // Small delay to ensure connections are fully closed
            await Task.Delay(100);

            // Delete all SQLite files (main db, WAL, and shared memory)
            var filesToDelete = new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" };
            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    _logger.LogDebug("Deleting file: {File}", file);
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning("Failed to delete {File}: {Error}. Retrying...", file, ex.Message);
                        await Task.Delay(500);
                        File.Delete(file);
                    }
                }
            }

            // Recreate the database with schema
            _logger.LogDebug("Recreating database for worker {WorkerIndex}...", workerIndex);
            await _context.Database.EnsureCreatedAsync();
            
            // Re-seed the database with initial data
            await SeedRolesAsync();
            await SeedPeopleAsync();
            await _context.SaveChangesAsync();

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Database reset via file deletion completed for worker {WorkerIndex} in {Ms}ms", 
                workerIndex, totalTime);
        }
        catch (Exception ex)
        {
            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "File deletion reset failed for worker {WorkerIndex} after {Ms}ms", 
                workerIndex, totalTime);
            throw;
        }
    }

    /// <summary>
    /// Extracts the database file path from a SQLite connection string
    /// </summary>
    private string? ExtractDatabasePath(string connectionString)
    {
        // Parse "Data Source=path" from connection string
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("Data Source=".Length).Trim();
            }
        }
        return null;
    }

    /// <summary>
    /// Attempts to reset database using Respawn (more reliable but limited SQLite support)
    /// </summary>
    private Task<bool> TryResetWithRespawnAsync(string connectionString, int workerIndex)
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
    private async Task ResetWithEfCoreAsync(int workerIndex)
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
            
            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Database reset completed using EF Core for worker {WorkerIndex} in {Ms}ms", workerIndex, totalTime);
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

    /// <summary>
    /// Validates that the database is in a clean state before test execution
    /// </summary>
    public async Task<DatabaseValidationResult> ValidatePreTestStateAsync(int workerIndex)
    {
        _logger.LogDebug("Validating pre-test database state for worker {WorkerIndex}", workerIndex);
        
        var stats = await GetDatabaseStatsAsync();
        var issues = new List<string>();
        
        // Check if database is empty
        if (stats.PeopleCount > 0) issues.Add($"People table contains {stats.PeopleCount} records");
        if (stats.RolesCount > 0) issues.Add($"Roles table contains {stats.RolesCount} records");
        if (stats.WallsCount > 0) issues.Add($"Walls table contains {stats.WallsCount} records");
        if (stats.WindowsCount > 0) issues.Add($"Windows table contains {stats.WindowsCount} records");
        
        // Check database connectivity
        if (!stats.CanConnect) issues.Add("Cannot connect to database");
        
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
        if (stats.PeopleCount > 0) issues.Add($"People table not cleaned up: {stats.PeopleCount} records remain");
        if (stats.RolesCount > 0) issues.Add($"Roles table not cleaned up: {stats.RolesCount} records remain");
        if (stats.WallsCount > 0) issues.Add($"Walls table not cleaned up: {stats.WallsCount} records remain");
        if (stats.WindowsCount > 0) issues.Add($"Windows table not cleaned up: {stats.WindowsCount} records remain");
        
        // Check database connectivity
        if (!stats.CanConnect) issues.Add("Database connection lost during test");
        
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
