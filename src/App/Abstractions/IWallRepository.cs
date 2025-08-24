using Domain.Entities;

namespace App.Abstractions;

public interface IWallRepository
{
    Task<Wall?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Wall>> ListAsync(CancellationToken ct = default);
    Task<Wall> AddAsync(Wall wall, CancellationToken ct = default);
    Task UpdateAsync(Wall wall, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
