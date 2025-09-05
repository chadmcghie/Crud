using App.Abstractions;
using Domain.Entities;

namespace App.Services;

public class RoleService(IRoleRepository roles) : IRoleService
{
    public async Task<Role?> GetAsync(Guid id, CancellationToken ct = default)
        => await roles.GetAsync(id, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default)
        => await roles.ListAsync(ct);

    public async Task<Role> CreateAsync(string name, string? description = null, CancellationToken ct = default)
    {
        // ensure unique by name
        var existing = await roles.GetByNameAsync(name, ct);
        if (existing is not null)
            return existing;

        return await roles.AddAsync(new Role { Name = name, Description = description }, ct);
    }

    public async Task UpdateAsync(Guid id, string name, string? description = null, CancellationToken ct = default)
    {
        var role = await roles.GetAsync(id, ct) ?? throw new KeyNotFoundException($"Role {id} not found");
        role.Name = name;
        role.Description = description;
        await roles.UpdateAsync(role, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await roles.DeleteAsync(id, ct);
}
