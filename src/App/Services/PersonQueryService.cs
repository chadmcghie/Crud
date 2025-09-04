using Domain.Entities;
using Domain.Interfaces;
using Domain.Specifications;

namespace App.Services;

/// <summary>
/// Example service demonstrating the use of generic repository pattern with Ardalis.Specification
/// This service showcases how to use specifications for complex queries while maintaining clean architecture
/// </summary>
public interface IPersonQueryService
{
    Task<IReadOnlyList<Person>> FindPersonsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> FindPersonsByRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Person?> GetPersonWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountPersonsAsync(CancellationToken cancellationToken = default);
    Task<bool> HasPersonsWithRoleAsync(string roleName, CancellationToken cancellationToken = default);
}

public class PersonQueryService : IPersonQueryService
{
    private readonly IRepository<Person> _personRepository;

    public PersonQueryService(IRepository<Person> personRepository)
    {
        _personRepository = personRepository;
    }

    /// <summary>
    /// Find persons by partial name match (case-insensitive) with roles included
    /// Uses PersonByNameSpec to encapsulate the query logic
    /// </summary>
    public async Task<IReadOnlyList<Person>> FindPersonsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var specification = new PersonByNameSpec(name);
        return await _personRepository.ListAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Find persons by role name with roles included
    /// Uses PersonsByRoleSpec to encapsulate the complex query with joins
    /// </summary>
    public async Task<IReadOnlyList<Person>> FindPersonsByRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var specification = new PersonsByRoleSpec(roleName);
        return await _personRepository.ListAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Get a single person with roles using optimized single-result specification
    /// Uses PersonByIdWithRolesSpec with ISingleResultSpecification for better performance
    /// </summary>
    public async Task<Person?> GetPersonWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var specification = new PersonByIdWithRolesSpec(id);
        return await _personRepository.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Get total count of persons
    /// Demonstrates basic repository methods without specifications
    /// </summary>
    public async Task<int> CountPersonsAsync(CancellationToken cancellationToken = default)
    {
        return await _personRepository.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Check if any persons have a specific role
    /// Uses specification with AnyAsync for efficient existence checking
    /// </summary>
    public async Task<bool> HasPersonsWithRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var specification = new PersonsByRoleSpec(roleName);
        return await _personRepository.AnyAsync(specification, cancellationToken);
    }
}