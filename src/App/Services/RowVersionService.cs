namespace App.Services;

/// <summary>
/// Implementation of RowVersion service for SQLite-compatible optimistic concurrency control.
/// Uses GUID-based versioning to ensure uniqueness and proper concurrency validation.
/// </summary>
public class RowVersionService : IRowVersionService
{
    /// <summary>
    /// Generates an initial RowVersion for newly created entities.
    /// Uses a new GUID to ensure uniqueness.
    /// </summary>
    /// <returns>A new byte array representing the initial version</returns>
    public byte[] GenerateInitialVersion()
    {
        return Guid.NewGuid().ToByteArray();
    }
    
    /// <summary>
    /// Generates a new RowVersion for entity updates.
    /// Each call produces a unique version to detect concurrent modifications.
    /// </summary>
    /// <returns>A new byte array representing the updated version</returns>
    public byte[] GenerateNewVersion()
    {
        return Guid.NewGuid().ToByteArray();
    }
}