using App.Abstractions;
using Domain.Entities;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryRoleRepository : IRoleRepository
{
    private readonly Dictionary<Guid, Role> _store = new();
    private readonly object _lock = new object();

    public Task<Role?> GetAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.TryGetValue(id, out var r) ? r : null);
        }
    }

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult((IReadOnlyList<Role>)_store.Values.ToList());
        }
    }

    public Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _store[role.Id] = role;
            return Task.FromResult(role);
        }
    }

    public Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _store[role.Id] = role;
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _store.Remove(id);
            return Task.CompletedTask;
        }
    }
}
