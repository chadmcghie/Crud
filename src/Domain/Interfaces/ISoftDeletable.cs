namespace Domain.Interfaces;

/// <summary>
/// Interface for entities that support soft delete functionality.
/// Soft deletes provide a safety mechanism by marking records as deleted instead of removing them.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft deleted
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// The timestamp when the entity was soft deleted (null if not deleted)
    /// </summary>
    DateTime? DeletedAt { get; }

    /// <summary>
    /// The user or system that performed the soft delete
    /// </summary>
    string? DeletedBy { get; }

    /// <summary>
    /// Marks the entity as soft deleted
    /// </summary>
    /// <param name="deletedBy">The user or system performing the delete</param>
    void SoftDelete(string? deletedBy = null);

    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    void Restore();
}
