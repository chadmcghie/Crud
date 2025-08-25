using App.Abstractions;
using Domain.Entities;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryWindowRepository : IWindowRepository
{
    private readonly Dictionary<Guid, Window> _store = new();

    public Task<Window?> GetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var w) ? w : null);

    public Task<IReadOnlyList<Window>> ListAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Window>)_store.Values.ToList());

    public Task<Window> AddAsync(Window window, CancellationToken ct = default)
    {
        _store[window.Id] = window;
        return Task.FromResult(window);
    }

    public Task UpdateAsync(Window window, CancellationToken ct = default)
    {
        _store[window.Id] = window;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }
}