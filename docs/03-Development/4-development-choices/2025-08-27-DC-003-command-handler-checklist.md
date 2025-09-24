# Command Handler Development Checklist

This checklist ensures consistency and prevents common issues in command handler implementations.

## Update Command Handler Requirements

When implementing an `Update*CommandHandler`, ensure you follow these requirements:

### ✅ **Critical: UpdatedAt Assignment**

**ALWAYS** set `UpdatedAt = DateTime.UtcNow` in update command handlers:

```csharp
public class UpdateEntityCommandHandler : IRequestHandler<UpdateEntityCommand>
{
    public async Task Handle(UpdateEntityCommand request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetAsync(request.Id, cancellationToken);
        
        // ... update entity properties ...
        
        entity.UpdatedAt = DateTime.UtcNow;  // ← REQUIRED!
        await repository.UpdateAsync(entity, cancellationToken);
    }
}
```

### Why This Matters

- **Conditional Requests**: HTTP Last-Modified headers depend on UpdatedAt
- **Caching**: Output caching uses UpdatedAt for cache invalidation  
- **API Consistency**: All entities must have consistent timestamp behavior
- **Client Synchronization**: Clients rely on UpdatedAt for optimistic concurrency

### Current Implementation Status

All entities currently implement this correctly:

✅ **Windows**: `window.UpdatedAt = DateTime.UtcNow`  
✅ **Walls**: `wall.UpdatedAt = DateTime.UtcNow`  
✅ **Roles**: `role.UpdatedAt = DateTime.UtcNow`  
✅ **People**: `person.UpdatedAt = DateTime.UtcNow`  

## Other Command Handler Best Practices

### Error Handling
- Throw `KeyNotFoundException` when entity not found
- Use appropriate domain exceptions for business rule violations

### Repository Pattern
- Always use the injected repository interface
- Follow async/await patterns consistently

### Transaction Scope
- Update operations should be atomic
- Consider cascade updates for related entities

## Code Review Checklist

When reviewing command handler changes, verify:

- [ ] **UpdatedAt = DateTime.UtcNow** is present in update handlers
- [ ] Error handling follows established patterns
- [ ] Async patterns are used correctly
- [ ] Repository interfaces are used (not concrete implementations)
- [ ] Related entities are updated if needed

## Historical Issues

This checklist prevents regressions of these resolved issues:

- **BI-2025-09-20-001**: Missing UpdatedAt in role updates caused conditional request test failures
- **BI-2025-09-20-002**: Inconsistent UpdatedAt handling across entities

## Testing

After implementing a command handler:

1. Run relevant integration tests
2. Verify conditional request behavior if entity has API endpoints
3. Test Last-Modified headers are updated correctly
4. Ensure optimistic concurrency works if implemented

---

*Last Updated: 2025-09-20*  
*Related: [Conditional Request Tests](../specs/2025-09-10-api-response-caching/)*