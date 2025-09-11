namespace App.Services;

/// <summary>
/// Service for managing RowVersion values for optimistic concurrency control in SQLite environments.
/// SQLite doesn't support automatic RowVersion generation, so we manage it at the application level.
/// </summary>
public interface IRowVersionService
{
    /// <summary>
    /// Generates an initial RowVersion for newly created entities.
    /// </summary>
    /// <returns>A new byte array representing the initial version</returns>
    byte[] GenerateInitialVersion();
    
    /// <summary>
    /// Generates a new RowVersion for entity updates.
    /// This should be called whenever an entity is modified to ensure concurrency control.
    /// </summary>
    /// <returns>A new byte array representing the updated version</returns>
    byte[] GenerateNewVersion();
}