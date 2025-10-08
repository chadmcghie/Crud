# Ardalis Specification Pattern Implementation

This document explains the implementation of the Ardalis Specification Pattern and generic repository pattern in this application.

## Overview

This implementation addresses issues #35 and #36 by adding:
- **Ardalis.Specification.EntityFrameworkCore** integration
- **Generic IRepository<T> pattern** alongside existing custom repositories
- **Specification classes** for encapsulating complex query logic

## Architecture

### Layer Structure
- **Domain Layer**: Contains `IRepository<T>` interface and specification classes
- **Infrastructure Layer**: Contains `EfRepository<T>` implementation using Ardalis.Specification.EntityFrameworkCore
- **Application Layer**: Contains services that use the generic repository with specifications
- **API Layer**: Contains controllers demonstrating specification usage

## Components

### 1. Generic Repository Interface
```csharp
// Domain/Interfaces/IRepository.cs
public interface IRepository<T> : IRepositoryBase<T> where T : class
{
    // Inherits all standard repository methods from Ardalis.Specification
    // - GetByIdAsync, ListAsync, AddAsync, UpdateAsync, DeleteAsync
    // - FirstOrDefaultAsync, SingleOrDefaultAsync, AnyAsync, CountAsync
    // - All methods support specifications for complex queries
}
```

### 2. Entity Framework Implementation
```csharp
// Infrastructure/Repositories/EntityFramework/EfRepository.cs
public class EfRepository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    public EfRepository(ApplicationDbContext dbContext) : base(dbContext) { }
}
```

### 3. Example Specifications
```csharp
// Domain/Specifications/PersonSpecifications.cs

// Find persons by partial name match with roles included
public sealed class PersonByNameSpec : Specification<Person>
{
    public PersonByNameSpec(string name)
    {
        Query.Where(p => p.FullName.ToLower().Contains(name.ToLower()))
             .Include(p => p.Roles);
    }
}

// Find persons with specific roles
public sealed class PersonsByRoleSpec : Specification<Person>
{
    public PersonsByRoleSpec(string roleName)
    {
        Query.Where(p => p.Roles.Any(r => r.Name == roleName))
             .Include(p => p.Roles);
    }
}

// Get person by ID with roles (optimized single result)
public sealed class PersonByIdWithRolesSpec : Specification<Person>, ISingleResultSpecification<Person>
{
    public PersonByIdWithRolesSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Roles);
    }
}
```

## Usage Examples

### 1. In Services
```csharp
public class PersonQueryService : IPersonQueryService
{
    private readonly IRepository<Person> _personRepository;

    public PersonQueryService(IRepository<Person> personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<IReadOnlyList<Person>> FindPersonsByNameAsync(string name)
    {
        var specification = new PersonByNameSpec(name);
        return await _personRepository.ListAsync(specification);
    }

    public async Task<Person?> GetPersonWithRolesAsync(Guid id)
    {
        var specification = new PersonByIdWithRolesSpec(id);
        return await _personRepository.FirstOrDefaultAsync(specification);
    }
}
```

### 2. In Controllers
```csharp
[HttpGet("search")]
public async Task<ActionResult<IEnumerable<PersonResponse>>> SearchByName([FromQuery] string name)
{
    var people = await _personQueryService.FindPersonsByNameAsync(name);
    return Ok(_mapper.Map<IEnumerable<PersonResponse>>(people));
}
```

## Key Benefits

### 1. Query Encapsulation
- Complex query logic is encapsulated in reusable specification classes
- No LINQ queries scattered throughout controllers or services
- Easy to unit test specifications independently

### 2. Type Safety
- Generic repository provides compile-time type safety
- Specifications are strongly typed to specific entity types
- IntelliSense support for all operations

### 3. Performance Optimization
- `ISingleResultSpecification<T>` interface for single-result queries provides better performance
- EF Core query optimization through proper Include statements
- Composable specifications can be combined for complex queries

### 4. Backward Compatibility
- Existing custom repositories (`IPersonRepository`, `IRoleRepository`, etc.) continue to work
- Gradual migration path from custom repositories to generic pattern
- Both patterns can coexist in the same application

### 5. Testing
- Easy to mock `IRepository<T>` for unit testing
- Specifications can be tested independently with in-memory databases
- Clear separation of concerns between data access and business logic

## Dependency Injection Setup

Both SQL Server and SQLite configurations register the generic repository:

```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
```

Application services are registered in App layer:
```csharp
// App/DependencyInjection.cs
services.AddScoped<IPersonQueryService, PersonQueryService>();
```

## API Endpoints (Example)

The new `PeopleQueriesController` demonstrates the specification pattern:

- `GET /api/people/queries/search?name=john` - Search by name
- `GET /api/people/queries/by-role?roleName=Admin` - Find by role  
- `GET /api/people/queries/{id}/with-roles` - Get with roles included
- `GET /api/people/queries/count` - Get total count
- `GET /api/people/queries/has-role?roleName=Manager` - Check role existence

## Testing

### Unit Tests
- `EfRepositoryTests` - Tests generic repository with in-memory database
- All specifications tested with various scenarios
- 7 unit tests covering CRUD operations and specification usage

### Integration Tests
- `GenericRepositoryIntegrationTests` - Tests with real database and DI container
- Tests interaction between generic and custom repositories
- Verifies end-to-end specification functionality

## Best Practices

1. **Use Specifications for Complex Queries**: Encapsulate filtering, sorting, and includes
2. **Single Responsibility**: Each specification should handle one specific query concern
3. **Composition**: Combine specifications for complex scenarios when needed
4. **Performance**: Use `ISingleResultSpecification<T>` for single-result queries
5. **Naming**: Use descriptive names like `PersonByNameSpec`, `ActiveUsersSpec`

## Migration Strategy

For existing code:
1. Start using generic repository for new features
2. Gradually migrate existing custom repositories to specifications
3. Keep custom repositories for complex domain-specific operations
4. Use generic repository for standard CRUD with query variations

This implementation provides a solid foundation for the specification pattern while maintaining flexibility and backward compatibility.