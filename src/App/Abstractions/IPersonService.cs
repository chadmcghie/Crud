using Domain.Entities;

namespace App.Abstractions;

public interface IPersonService
{
    Task<Person?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default);
    Task<Person> CreateAsync(string fullName, string? phone, IEnumerable<Guid>? roleIds = null, CancellationToken ct = default);
    Task UpdateAsync(Guid id, string fullName, string? phone, IEnumerable<Guid>? roleIds = null, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
