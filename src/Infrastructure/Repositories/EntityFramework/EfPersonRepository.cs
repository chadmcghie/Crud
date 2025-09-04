using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
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
            await _context.SaveChangesAsync(ct);
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
            _context.People.Update(person);
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The person was modified by another user. Please refresh and try again.", ex);
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
                _context.People.Remove(person);
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The person was modified by another user. Please refresh and try again.", ex);
        }
    }
}
