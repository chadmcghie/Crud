using Domain.Entities;

namespace App.Abstractions;

public interface IRoleService
{
    Task<Role?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default);
    Task<Role> CreateAsync(string name, string? description = null, CancellationToken ct = default);
    Task UpdateAsync(Guid id, string name, string? description = null, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
