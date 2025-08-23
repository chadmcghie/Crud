using App.Abstractions;
using Domain.Entities;

namespace Infrastructure.Repositories.InMemory;

public class InMemoryPersonRepository : IPersonRepository
{
    private readonly Dictionary<Guid, Person> _store = new();

    public Task<Person?> GetAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var p) ? p : null);

    public Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Person>)_store.Values.ToList());

    public Task<Person> AddAsync(Person person, CancellationToken ct = default)
    {
        _store[person.Id] = person;
        return Task.FromResult(person);
    }

    public Task UpdateAsync(Person person, CancellationToken ct = default)
    {
        _store[person.Id] = person;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }
}
