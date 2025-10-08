using System.Data.Common;
using App.Abstractions;
using App.Interfaces;
using App.Models;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Services;

/// <summary>
/// Service for managing database operations during testing.
/// Provides database reset and seeding capabilities using EF Core for SQLite.
/// </summary>
public class DatabaseTestService : IDatabaseTestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseTestService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IOutputCacheStore? _outputCacheStore;
    private readonly object _respawnerLock = new();
    private static readonly SemaphoreSlim _databaseMutex = new SemaphoreSlim(1, 1);

    public DatabaseTestService(
        ApplicationDbContext context,
        ILogger<DatabaseTestService> logger,
        ICacheService cacheService,
        IOutputCacheStore? outputCacheStore = null)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
        _outputCacheStore = outputCacheStore;
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

        // Logging: Only log provider and database initialization; do not log connection string
        _logger.LogInformation("Initializing database service for provider {Provider}.",
            _context.Database.ProviderName);

        // Ensure database exists
        await _context.Database.EnsureCreatedAsync();

        _logger.LogInformation("Database service initialized successfully");
    }

    /// <summary>
    /// Resets the database to a clean state, removing all data while preserving schema.
    /// In CI/Docker environments, uses file deletion for better performance.
    /// Uses EF Core cleanup for SQLite compatibility.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    /// <param name="seedData">Whether to seed initial test data after reset (default: false)</param>
    public async Task ResetDatabaseAsync(int workerIndex, bool seedData = false)
    {
        _logger.LogInformation("Resetting database for worker {WorkerIndex}", workerIndex);

        try
        {
            var connectionString = _context.Database.GetConnectionString();

            // TEMPORARY FIX: Always use EF Core cleanup for better reliability in CI
            // The file deletion method has SQLite locking issues in GitHub Actions
            // TODO: Investigate and fix file deletion method for better performance
            _logger.LogInformation("Using EF Core cleanup method for reliable database reset in CI");
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
    private async Task ResetByFileDeletionAsync(int workerIndex, bool seedData)
    {
        _logger.LogInformation("Resetting database via file deletion for worker {WorkerIndex} in CI environment", workerIndex);
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

            // CRITICAL: Clear ALL cache layers after database reset to prevent stale data
            _logger.LogInformation("[Phase 4] Clearing all caches for worker {WorkerIndex}...", workerIndex);
            var cacheStart = DateTime.UtcNow;
            double cacheTime = 0;
            try
            {
                // Clear application-level cache (LazyCache/Redis)
                await _cacheService.RemoveByPatternAsync("*");
                _logger.LogInformation("[Phase 4] Cleared application cache");

                // Clear ASP.NET Core Output Cache for all entity types
                if (_outputCacheStore != null)
                {
                    var entityTypes = new[] { "people", "roles", "walls", "windows", "users" };
                    foreach (var entityType in entityTypes)
                    {
                        await _outputCacheStore.EvictByTagAsync(entityType, default);
                    }
                    _logger.LogInformation("[Phase 4] Cleared output cache for all entity types");
                }

                cacheTime = (DateTime.UtcNow - cacheStart).TotalMilliseconds;
                _logger.LogInformation("[Phase 4] Cleared all caches in {Ms}ms", cacheTime);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "[Phase 4] Failed to clear caches, but continuing anyway");
            }

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            if (seedData)
            {
                _logger.LogInformation("Database reset via file deletion completed for worker {WorkerIndex} in {Ms}ms (Close: {CloseMs}ms, Delete: {DeleteMs}ms, Create: {CreateMs}ms, Seed: {SeedMs}ms, Cache: {CacheMs}ms)",
                    workerIndex, totalTime, closeTime, deleteTime, createTime, seedTime, cacheTime);
            }
            else
            {
                _logger.LogInformation("Database reset via file deletion completed for worker {WorkerIndex} in {Ms}ms (Close: {CloseMs}ms, Delete: {DeleteMs}ms, Create: {CreateMs}ms, Cache: {CacheMs}ms)",
                    workerIndex, totalTime, closeTime, deleteTime, createTime, cacheTime);
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
    /// Resets database using EF Core with optimized bulk operations.
    /// CRITICAL: Uses IgnoreQueryFilters() to ensure ALL entities are deleted, including soft-deleted ones.
    /// </summary>
    private async Task ResetWithEfCoreAsync(int workerIndex, bool seedData)
    {
        _logger.LogInformation("Resetting database for worker {WorkerIndex} using EF Core with hard delete", workerIndex);
        var startTime = DateTime.UtcNow;

        try
        {
            // Use ExecuteDeleteAsync for better performance (EF Core 7+)
            // This generates efficient DELETE statements instead of loading entities
            // IMPORTANT: IgnoreQueryFilters() is CRITICAL to bypass soft delete filters and truly delete everything

            // CRITICAL: Delete join tables FIRST to avoid FK constraint violations
            // PersonRoles is a many-to-many join table that must be deleted before People and Roles
            _logger.LogDebug("Deleting PersonRoles join table for worker {WorkerIndex}...", workerIndex);
            var personRolesStart = DateTime.UtcNow;
            var personRolesDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM PersonRoles");
            _logger.LogDebug("Deleted {Count} PersonRoles records in {Ms}ms",
                personRolesDeleted, (DateTime.UtcNow - personRolesStart).TotalMilliseconds);

            // Delete PasswordResetTokens first (has FK to Users)
            _logger.LogDebug("Deleting PasswordResetTokens for worker {WorkerIndex}...", workerIndex);
            var tokensStart = DateTime.UtcNow;
            await _context.PasswordResetTokens.IgnoreQueryFilters().ExecuteDeleteAsync();
            var tokensCount = await _context.PasswordResetTokens.IgnoreQueryFilters().CountAsync();
            _logger.LogDebug("Deleted PasswordResetTokens in {Ms}ms, remaining count: {Count}",
                (DateTime.UtcNow - tokensStart).TotalMilliseconds, tokensCount);

            // Delete Users (authentication data)
            // CRITICAL: Use raw SQL for Users to bypass soft-delete and unique constraints
            // ExecuteDeleteAsync may not work properly with soft-deleted records and unique constraints
            _logger.LogDebug("Deleting Users for worker {WorkerIndex}...", workerIndex);
            var usersStart = DateTime.UtcNow;
            var usersDeleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM Users");
            var usersCount = await _context.Users.IgnoreQueryFilters().CountAsync();
            _logger.LogDebug("Deleted {Count} Users in {Ms}ms, remaining count: {RemainingCount}",
                usersDeleted, (DateTime.UtcNow - usersStart).TotalMilliseconds, usersCount);

            // Delete People (now that PersonRoles join table is empty)
            _logger.LogDebug("Deleting People for worker {WorkerIndex}...", workerIndex);
            var peopleStart = DateTime.UtcNow;
            var peopleCountBefore = await _context.People.IgnoreQueryFilters().CountAsync();
            _logger.LogDebug("People count before delete: {Count}", peopleCountBefore);
            await _context.People.IgnoreQueryFilters().ExecuteDeleteAsync();
            var peopleCountAfter = await _context.People.IgnoreQueryFilters().CountAsync();
            _logger.LogDebug("Deleted People in {Ms}ms, before: {Before}, after: {After}",
                (DateTime.UtcNow - peopleStart).TotalMilliseconds, peopleCountBefore, peopleCountAfter);

            // Delete Roles (now that PersonRoles join table is empty)
            _logger.LogDebug("Deleting Roles for worker {WorkerIndex}...", workerIndex);
            var rolesStart = DateTime.UtcNow;
            var rolesCountBefore = await _context.Roles.IgnoreQueryFilters().CountAsync();
            _logger.LogDebug("Roles count before delete: {Count}", rolesCountBefore);
            await _context.Roles.IgnoreQueryFilters().ExecuteDeleteAsync();
            var rolesCountAfter = await _context.Roles.IgnoreQueryFilters().CountAsync();
            _logger.LogDebug("Deleted Roles in {Ms}ms, before: {Before}, after: {After}",
                (DateTime.UtcNow - rolesStart).TotalMilliseconds, rolesCountBefore, rolesCountAfter);

            _logger.LogDebug("Deleting Windows for worker {WorkerIndex}...", workerIndex);
            var windowsStart = DateTime.UtcNow;
            await _context.Windows.IgnoreQueryFilters().ExecuteDeleteAsync();
            _logger.LogDebug("Deleted Windows in {Ms}ms", (DateTime.UtcNow - windowsStart).TotalMilliseconds);

            _logger.LogDebug("Deleting Walls for worker {WorkerIndex}...", workerIndex);
            var wallsStart = DateTime.UtcNow;
            await _context.Walls.IgnoreQueryFilters().ExecuteDeleteAsync();
            _logger.LogDebug("Deleted Walls in {Ms}ms", (DateTime.UtcNow - wallsStart).TotalMilliseconds);

            // Optionally seed data after cleanup
            if (seedData)
            {
                _logger.LogDebug("Seeding database for worker {WorkerIndex}...", workerIndex);
                await SeedRolesAsync();
                await SeedPeopleAsync();
                await _context.SaveChangesWithRetryAsync();
            }

            // CRITICAL: Clear ALL cache layers after database reset to prevent stale data
            _logger.LogDebug("Clearing all caches for worker {WorkerIndex}...", workerIndex);
            var cacheStart = DateTime.UtcNow;
            try
            {
                // Clear application-level cache (LazyCache/Redis)
                await _cacheService.RemoveByPatternAsync("*");
                _logger.LogDebug("Cleared application cache");

                // Clear ASP.NET Core Output Cache for all entity types
                if (_outputCacheStore != null)
                {
                    var entityTypes = new[] { "people", "roles", "walls", "windows", "users" };
                    foreach (var entityType in entityTypes)
                    {
                        await _outputCacheStore.EvictByTagAsync(entityType, default);
                    }
                    _logger.LogDebug("Cleared output cache for all entity types");
                }

                _logger.LogDebug("Cleared all caches in {Ms}ms",
                    (DateTime.UtcNow - cacheStart).TotalMilliseconds);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Failed to clear caches, but continuing anyway");
            }

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Database reset completed using EF Core for worker {WorkerIndex} in {Ms}ms (seedData: {SeedData})", workerIndex, totalTime, seedData);
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
    /// IMPORTANT: Uses IgnoreQueryFilters() to count ALL entities including soft-deleted ones.
    /// </summary>
    public async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        return new DatabaseStats
        {
            PeopleCount = await _context.People.IgnoreQueryFilters().CountAsync(),
            RolesCount = await _context.Roles.IgnoreQueryFilters().CountAsync(),
            WallsCount = await _context.Walls.IgnoreQueryFilters().CountAsync(),
            WindowsCount = await _context.Windows.IgnoreQueryFilters().CountAsync(),
            UsersCount = await _context.Users.IgnoreQueryFilters().CountAsync(),
            PasswordResetTokensCount = await _context.PasswordResetTokens.IgnoreQueryFilters().CountAsync(),
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
        if (stats.UsersCount > 0)
            issues.Add($"Users table contains {stats.UsersCount} records");
        if (stats.PasswordResetTokensCount > 0)
            issues.Add($"PasswordResetTokens table contains {stats.PasswordResetTokensCount} records");

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
        if (stats.UsersCount > 0)
            issues.Add($"Users table not cleaned up: {stats.UsersCount} records remain");
        if (stats.PasswordResetTokensCount > 0)
            issues.Add($"PasswordResetTokens table not cleaned up: {stats.PasswordResetTokensCount} records remain");

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
    /// IMPORTANT: Uses IgnoreQueryFilters() to verify ALL entities including soft-deleted ones.
    /// </summary>
    public async Task<bool> VerifyDatabaseIntegrityAsync(int workerIndex)
    {
        try
        {
            _logger.LogDebug("Verifying database integrity for worker {WorkerIndex}", workerIndex);

            // Test basic database operations - check ALL entities including soft-deleted
            await _context.Database.CanConnectAsync();
            await _context.People.IgnoreQueryFilters().CountAsync();
            await _context.Roles.IgnoreQueryFilters().CountAsync();
            await _context.Walls.IgnoreQueryFilters().CountAsync();
            await _context.Windows.IgnoreQueryFilters().CountAsync();
            await _context.Users.IgnoreQueryFilters().CountAsync();
            await _context.PasswordResetTokens.IgnoreQueryFilters().CountAsync();

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
            // Generate unique role names to avoid conflicts in parallel test execution
            var uniqueSuffix = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}";
            var roles = new[]
            {
                Domain.Entities.Role.Create($"Administrator_{uniqueSuffix}", "System administrator with full access"),
                Domain.Entities.Role.Create($"User_{uniqueSuffix}", "Standard user with limited access"),
                Domain.Entities.Role.Create($"Guest_{uniqueSuffix}", "Guest user with read-only access")
            };

            _context.Roles.AddRange(roles);
            _logger.LogDebug("Added {Count} seed roles with unique names", roles.Length);
        }
    }

    private async Task SeedPeopleAsync()
    {
        if (!await _context.People.AnyAsync())
        {
            var people = new[]
            {
                Domain.Entities.Person.Create("John Doe"),
                Domain.Entities.Person.Create("Jane Smith")
            };

            _context.People.AddRange(people);
            _logger.LogDebug("Added {Count} seed people", people.Length);
        }
    }

    // The MaskConnectionString method has been removed as we now avoid logging connection strings entirely.
}
