using App.Abstractions;
using Domain.Entities;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryPersonRepository : IPersonRepository
{
    private readonly Dictionary<Guid, Person> _store = new();
    private readonly object _lock = new object();

    public Task<Person?> GetAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.TryGetValue(id, out var p) ? p : null);
        }
    }

    public Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult((IReadOnlyList<Person>)_store.Values.ToList());
        }
    }

    public Task<Person> AddAsync(Person person, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _store[person.Id] = person;
            return Task.FromResult(person);
        }
    }

    public Task UpdateAsync(Person person, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _store[person.Id] = person;
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
