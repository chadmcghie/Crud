using Ardalis.Specification;
using Domain.Entities;

namespace Domain.Specifications;

/// <summary>
/// Specification for finding persons by their full name (case-insensitive)
/// </summary>
public sealed class PersonByNameSpec : Specification<Person>
{
    public PersonByNameSpec(string name)
    {
        Query.Where(p => p.FullName.ToLower().Contains(name.ToLower()))
             .Include(p => p.Roles);
    }
}

/// <summary>
/// Specification for finding persons with specific roles
/// </summary>
public sealed class PersonsByRoleSpec : Specification<Person>
{
    public PersonsByRoleSpec(string roleName)
    {
        Query.Where(p => p.Roles.Any(r => r.Name == roleName))
             .Include(p => p.Roles);
    }
}

/// <summary>
/// Specification for getting person by ID with roles included
/// </summary>
public sealed class PersonByIdWithRolesSpec : Specification<Person>, ISingleResultSpecification<Person>
{
    public PersonByIdWithRolesSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Roles);
    }
}