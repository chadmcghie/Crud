using Domain.Interfaces;

namespace Domain.Entities;

/// <summary>
/// Base entity with audit properties, proper encapsulation, and soft delete support.
/// Soft deletes provide a safety mechanism by marking records as deleted instead of removing them.
/// </summary>
public abstract class BaseEntity : ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// Nullable for SQLite compatibility.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    // Soft delete properties
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Updates the UpdatedAt timestamp. Should be called by domain methods when entity state changes.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Allows setting Id for testing purposes. Should only be used in test scenarios.
    /// </summary>
    protected void SetId(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Marks the entity as soft deleted
    /// </summary>
    /// <param name="deletedBy">The user or system performing the delete</param>
    public virtual void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        MarkAsUpdated();
    }

    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        MarkAsUpdated();
    }
}
