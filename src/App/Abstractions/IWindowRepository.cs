using Domain.Entities;

namespace App.Abstractions;

public interface IWindowRepository
{
    Task<Window?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Window>> ListAsync(CancellationToken ct = default);
    Task<Window> AddAsync(Window window, CancellationToken ct = default);
    Task UpdateAsync(Window window, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
