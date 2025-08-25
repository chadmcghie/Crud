using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.EntityFramework;

public class EfRoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public EfRoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Roles.FindAsync(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default)
    {
        return await _context.Roles.ToListAsync(ct);
    }

    public async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(ct);
        return role;
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _context.Roles.FindAsync(new object[] { id }, ct);
        if (role != null)
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync(ct);
        }
    }
}