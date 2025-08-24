using App.Abstractions;
using Domain.Entities;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryWallRepository : IWallRepository
{
    private readonly Dictionary<Guid, Wall> _store = new();

    public Task<Wall?> GetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var w) ? w : null);

    public Task<IReadOnlyList<Wall>> ListAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Wall>)_store.Values.ToList());

    public Task<Wall> AddAsync(Wall wall, CancellationToken ct = default)
    {
        _store[wall.Id] = wall;
        return Task.FromResult(wall);
    }

    public Task UpdateAsync(Wall wall, CancellationToken ct = default)
    {
        _store[wall.Id] = wall;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }
}
