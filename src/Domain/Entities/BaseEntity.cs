namespace Domain.Entities;

/// <summary>
/// Base entity with audit properties and proper encapsulation.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// Nullable for SQLite compatibility.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Updates the UpdatedAt timestamp. Should be called by domain methods when entity state changes.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}