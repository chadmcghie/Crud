using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Resilience;
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

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name, ct);
    }

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default)
    {
        return await _context.Roles.ToListAsync(ct);
    }

    public async Task<Role> AddAsync(Role role, CancellationToken ct = default)
    {
        try
        {
            _context.Roles.Add(role);
            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
            return role;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
        {
            throw new InvalidOperationException($"A role with the name '{role.Name}' already exists.", ex);
        }
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        try
        {
            _context.Roles.Update(role);
            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The role was modified by another user. Please refresh and try again.", ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var role = await _context.Roles.FindAsync(new object[] { id }, ct);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The role was modified by another user. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed") == true)
        {
            throw new InvalidOperationException("Cannot delete role because it is assigned to one or more people.", ex);
        }
    }
}
