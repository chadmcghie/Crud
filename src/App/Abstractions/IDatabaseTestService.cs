using System.Threading.Tasks;
using App.Models;

namespace App.Abstractions;

/// <summary>
/// Interface for managing database operations during testing.
/// Provides database reset and seeding capabilities without infrastructure dependencies.
/// </summary>
public interface IDatabaseTestService
{
    /// <summary>
    /// Initializes the database service.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Resets the database to a clean state, removing all data while preserving schema.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    /// <param name="seedData">Whether to seed initial test data after reset (default: false)</param>
    Task ResetDatabaseAsync(int workerIndex, bool seedData = false);

    /// <summary>
    /// Seeds the database with initial test data.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    Task SeedDatabaseAsync(int workerIndex);

    /// <summary>
    /// Gets database statistics for debugging purposes.
    /// </summary>
    Task<DatabaseStats> GetDatabaseStatsAsync();

    /// <summary>
    /// Validates that the database is in a clean state before test execution.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    Task<DatabaseValidationResult> ValidatePreTestStateAsync(int workerIndex);

    /// <summary>
    /// Validates the database state after test execution to ensure proper cleanup.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    Task<DatabaseValidationResult> ValidatePostTestStateAsync(int workerIndex);

    /// <summary>
    /// Performs database integrity verification.
    /// </summary>
    /// <param name="workerIndex">The worker index for parallel test execution</param>
    Task<bool> VerifyDatabaseIntegrityAsync(int workerIndex);
}