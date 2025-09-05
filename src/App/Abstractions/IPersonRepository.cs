using Domain.Entities;

namespace App.Abstractions;

public interface IPersonRepository
{
    Task<Person?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default);
    Task<Person> AddAsync(Person person, CancellationToken ct = default);
    Task UpdateAsync(Person person, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
