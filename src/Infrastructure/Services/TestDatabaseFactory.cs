using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Factory for creating isolated test databases per worker to ensure true test isolation
/// </summary>
public class TestDatabaseFactory
{
    private static readonly object _lockObject = new object();
    private static readonly Dictionary<int, string> _workerDatabases = new();
    private readonly ILogger<TestDatabaseFactory> _logger;

    public TestDatabaseFactory(ILogger<TestDatabaseFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates or gets a worker-specific database file path
    /// </summary>
    public string GetWorkerDatabasePath(int workerIndex)
    {
        lock (_lockObject)
        {
            if (_workerDatabases.TryGetValue(workerIndex, out var existingPath))
            {
                return existingPath;
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var processId = Environment.ProcessId;
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"CrudTest_Worker{workerIndex}_{processId}_{timestamp}.db"
            );

            _workerDatabases[workerIndex] = databasePath;
            
            _logger.LogInformation("Created database path for worker {WorkerIndex}: {DatabasePath}", 
                workerIndex, databasePath);
            
            return databasePath;
        }
    }

    /// <summary>
    /// Creates and initializes a database for the specified worker
    /// </summary>
    public async Task<string> CreateWorkerDatabaseAsync(int workerIndex, bool forceRecreate = false)
    {
        var databasePath = GetWorkerDatabasePath(workerIndex);
        
        // Apply file locking to prevent race conditions
        var lockFilePath = databasePath + ".lock";
        var maxRetries = 5;
        var retryDelay = TimeSpan.FromMilliseconds(100);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Use a lock file to prevent concurrent access
                using var lockFile = File.Open(lockFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                
                if (forceRecreate && File.Exists(databasePath))
                {
                    await RetryDeleteFileAsync(databasePath, maxRetries: 3);
                }

                if (!File.Exists(databasePath))
                {
                    await CreateDatabaseFileAsync(databasePath, workerIndex);
                }

                return databasePath;
            }
            catch (IOException ex) when (attempt < maxRetries - 1)
            {
                _logger.LogWarning("Database creation attempt {Attempt} failed for worker {WorkerIndex}: {Error}. Retrying...", 
                    attempt + 1, workerIndex, ex.Message);
                await Task.Delay(retryDelay);
            }
            finally
            {
                // Clean up lock file
                try
                {
                    if (File.Exists(lockFilePath))
                    {
                        File.Delete(lockFilePath);
                    }
                }
                catch
                {
                    // Ignore lock file cleanup errors
                }
            }
        }

        throw new InvalidOperationException($"Failed to create database for worker {workerIndex} after {maxRetries} attempts");
    }

    /// <summary>
    /// Creates the database file and initializes the schema
    /// </summary>
    private async Task CreateDatabaseFileAsync(string databasePath, int workerIndex)
    {
        var connectionString = $"Data Source={databasePath}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        _logger.LogInformation("Database schema created for worker {WorkerIndex} at {DatabasePath}", 
            workerIndex, databasePath);
    }

    /// <summary>
    /// Cleans up databases for all workers
    /// </summary>
    public async Task CleanupWorkerDatabasesAsync()
    {
        Dictionary<int, string> databasesToCleanup;
        
        lock (_lockObject)
        {
            databasesToCleanup = new Dictionary<int, string>(_workerDatabases);
            _workerDatabases.Clear();
        }
        
        foreach (var kvp in databasesToCleanup)
        {
            var workerIndex = kvp.Key;
            var databasePath = kvp.Value;
            
            try
            {
                if (File.Exists(databasePath))
                {
                    await RetryDeleteFileAsync(databasePath);
                    _logger.LogInformation("Cleaned up database for worker {WorkerIndex}: {DatabasePath}", 
                        workerIndex, databasePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to cleanup database for worker {WorkerIndex}: {Error}", 
                    workerIndex, ex.Message);
            }
        }
    }

    /// <summary>
    /// Cleans up database for a specific worker
    /// </summary>
    public async Task CleanupWorkerDatabaseAsync(int workerIndex)
    {
        string? databasePath = null;
        
        lock (_lockObject)
        {
            if (_workerDatabases.TryGetValue(workerIndex, out databasePath))
            {
                _workerDatabases.Remove(workerIndex);
            }
        }
        
        if (databasePath != null)
        {
            try
            {
                if (File.Exists(databasePath))
                {
                    await RetryDeleteFileAsync(databasePath);
                    _logger.LogInformation("Cleaned up database for worker {WorkerIndex}: {DatabasePath}", 
                        workerIndex, databasePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to cleanup database for worker {WorkerIndex}: {Error}", 
                    workerIndex, ex.Message);
            }
        }
    }

    /// <summary>
    /// Retry logic for file deletion to handle file locking issues
    /// </summary>
    private async Task RetryDeleteFileAsync(string filePath, int maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return;
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)));
            }
        }
    }
}