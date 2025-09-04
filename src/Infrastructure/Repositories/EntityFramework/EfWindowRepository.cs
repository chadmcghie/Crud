using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.EntityFramework;

public class EfWindowRepository : IWindowRepository
{
    private readonly ApplicationDbContext _context;

    public EfWindowRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Window?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Windows.FindAsync(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<Window>> ListAsync(CancellationToken ct = default)
    {
        return await _context.Windows.ToListAsync(ct);
    }

    public async Task<Window> AddAsync(Window window, CancellationToken ct = default)
    {
        window.CreatedAt = DateTime.UtcNow;
        _context.Windows.Add(window);
        await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        return window;
    }

    public async Task UpdateAsync(Window window, CancellationToken ct = default)
    {
        window.UpdatedAt = DateTime.UtcNow;
        _context.Windows.Update(window);
        await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var window = await _context.Windows.FindAsync(new object[] { id }, ct);
        if (window != null)
        {
            _context.Windows.Remove(window);
            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        }
    }
}
