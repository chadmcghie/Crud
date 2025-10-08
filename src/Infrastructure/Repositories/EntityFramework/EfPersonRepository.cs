using App.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories.EntityFramework;

public class EfPersonRepository : IPersonRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EfPersonRepository> _logger;

    public EfPersonRepository(ApplicationDbContext context, ILogger<EfPersonRepository> logger)
    {
        _context = context;
        _logger = logger;
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
            // SIMPLER APPROACH: Load the tracked entity and update it directly
            // This ensures EF Core properly tracks all changes including many-to-many
            var trackedPerson = await _context.People
                .Include(p => p.Roles)
                .FirstOrDefaultAsync(p => p.Id == person.Id, ct);

            if (trackedPerson == null)
            {
                throw new InvalidOperationException($"Person with ID {person.Id} not found in database.");
            }

            // Update scalar properties using domain methods
            trackedPerson.UpdateFullName(person.FullName);
            trackedPerson.UpdatePhone(person.Phone);

            // Get the new roles from DB (ensure they're tracked)
            var newRoleIds = person.Roles.Select(r => r.Id).ToHashSet();
            var newRoles = await _context.Roles
                .Where(r => newRoleIds.Contains(r.Id))
                .ToListAsync(ct);

            // Update roles using domain method on the TRACKED entity
            trackedPerson.UpdateRoles(newRoles);

            await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update person {PersonId}", person.Id);
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
