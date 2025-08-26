using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Concurrency token for optimistic concurrency control
    // Nullable for SQLite compatibility
    public byte[]? RowVersion { get; set; }
}
