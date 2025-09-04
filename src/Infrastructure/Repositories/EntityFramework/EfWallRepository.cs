using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.EntityFramework;

public class EfWallRepository : IWallRepository
{
    private readonly ApplicationDbContext _context;

    public EfWallRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Wall?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Walls.FindAsync(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<Wall>> ListAsync(CancellationToken ct = default)
    {
        return await _context.Walls.ToListAsync(ct);
    }

    public async Task<Wall> AddAsync(Wall wall, CancellationToken ct = default)
    {
        wall.CreatedAt = DateTime.UtcNow;
        _context.Walls.Add(wall);
        await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        return wall;
    }

    public async Task UpdateAsync(Wall wall, CancellationToken ct = default)
    {
        wall.UpdatedAt = DateTime.UtcNow;
        _context.Walls.Update(wall);
        await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var wall = await _context.Walls.FindAsync(new object[] { id }, ct);
        if (wall != null)
        {
            _context.Walls.Remove(wall);
            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        }
    }
}
