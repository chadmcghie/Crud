using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.EntityFramework;

public class EfPersonRepository : IPersonRepository
{
    private readonly ApplicationDbContext _context;

    public EfPersonRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Person?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.People
            .Include(p => p.Roles)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default)
    {
        return await _context.People
            .Include(p => p.Roles)
            .ToListAsync(ct);
    }

    public async Task<Person> AddAsync(Person person, CancellationToken ct = default)
    {
        try
        {
            _context.People.Add(person);
            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
            return person;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to create person. Please check that all referenced roles exist.", ex);
        }
    }

    public async Task UpdateAsync(Person person, CancellationToken ct = default)
    {
        try
        {
            // Ensure entity is properly tracked with its navigation properties
            var entry = _context.Entry(person);
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                // If detached, attach and load existing roles to properly track many-to-many changes
                _context.People.Attach(person);
                await entry.Collection(p => p.Roles).LoadAsync(ct);
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                // For tracked entities, ensure roles collection is loaded
                if (!entry.Collection(p => p.Roles).IsLoaded)
                {
                    await entry.Collection(p => p.Roles).LoadAsync(ct);
                }
            }

            // Ensure all role entities in the person's collection are tracked by this context
            foreach (var role in person.Roles)
            {
                var roleEntry = _context.Entry(role);
                if (roleEntry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    _context.Attach(role);
                }
            }

            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to update person. Please check that all referenced roles exist.", ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var person = await _context.People.FindAsync(new object[] { id }, ct);
            if (person != null)
            {
                // Use soft delete instead of hard delete for data safety
                person.SoftDelete("system"); // TODO: Get current user context for audit trail
                await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The person was modified by another user. Please refresh and try again.", ex);
        }
    }
}
