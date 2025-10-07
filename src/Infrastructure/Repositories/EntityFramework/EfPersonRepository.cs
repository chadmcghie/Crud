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
            _logger.LogWarning("🔍 REPO UPDATE: Starting UpdateAsync for person {PersonId}", person.Id);
            _logger.LogWarning("🔍 REPO UPDATE: Person has {RoleCount} roles: {Roles}",
                person.Roles.Count, string.Join(", ", person.Roles.Select(r => $"{r.Name}({r.Id})")));

            // Ensure entity is properly tracked with its navigation properties
            var entry = _context.Entry(person);
            _logger.LogWarning("🔍 REPO UPDATE: Person entity state BEFORE attach: {State}", entry.State);

            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                _logger.LogWarning("🔍 REPO UPDATE: Person was DETACHED, attaching now");
                // If detached, attach and load existing roles to properly track many-to-many changes
                _context.People.Attach(person);
                await entry.Collection(p => p.Roles).LoadAsync(ct);
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _logger.LogWarning("🔍 REPO UPDATE: After LoadAsync, person has {RoleCount} roles", person.Roles.Count);
            }

            _logger.LogWarning("🔍 REPO UPDATE: Person entity state AFTER attach: {State}", entry.State);

            // Ensure all role entities in the person's collection are tracked by this context
            foreach (var role in person.Roles)
            {
                var roleEntry = _context.Entry(role);
                _logger.LogWarning("🔍 REPO UPDATE: Role {RoleName}({RoleId}) state: {State}",
                    role.Name, role.Id, roleEntry.State);

                if (roleEntry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    _logger.LogWarning("🔍 REPO UPDATE: Role {RoleName} was DETACHED, attaching", role.Name);
                    _context.Attach(role);
                }
            }

            // CRITICAL FIX: Force EF Core to detect changes in the many-to-many collection
            _logger.LogWarning("🔍 REPO UPDATE: Calling DetectChanges()");
            _context.ChangeTracker.DetectChanges();

            // Log what changes EF Core detected
            var changes = _context.ChangeTracker.Entries()
                .Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                .Select(e => $"{e.Entity.GetType().Name} ({e.State})")
                .ToList();
            _logger.LogWarning("🔍 REPO UPDATE: ChangeTracker detected {ChangeCount} changes: {Changes}",
                changes.Count, string.Join(", ", changes));

            _logger.LogWarning("🔍 REPO UPDATE: Calling SaveChangesAsync");
            var saved = await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
            _logger.LogWarning("🔍 REPO UPDATE: SaveChanges returned {SavedCount} rows affected", saved);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "🔍 REPO UPDATE: DbUpdateException occurred!");
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
