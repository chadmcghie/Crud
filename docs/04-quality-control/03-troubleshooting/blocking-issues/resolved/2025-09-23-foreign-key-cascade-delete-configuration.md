# BI-2025-09-23-012: Foreign Key Cascade Delete Configuration Prevents Constraint Validation

**Created**: 2025-09-23 22:23
**Status**: OPEN
**Priority**: HIGH
**Category**: Database Design, Integration Testing, Data Integrity
**Affects**: Foreign Key Constraint Tests, Person-Role Relationships

## Problem Statement

Integration tests that validate foreign key constraint enforcement are failing because EF Core is configured with CASCADE delete behavior instead of RESTRICT behavior. When a Role is deleted that has associated People, the system automatically deletes the PersonRole relationships and allows the Role deletion to succeed, instead of preventing the deletion and returning an appropriate error.

## Symptoms

### Failing Tests
1. `DELETE_Role_With_People_Should_Handle_Constraints_ForConstraintProviders(provider: SQLite)` - Expected 409 Conflict, got 204 NoContent
2. `DELETE_Role_With_People_Should_Handle_Constraints_ForConstraintProviders(provider: SqlServer)` - Expected 409 Conflict, got 204 NoContent

### Test Expectations vs Reality
- **Expected**: HTTP 409 Conflict (via `InvalidOperationException` → `GlobalExceptionHandlingMiddleware`)
- **Expected**: Error message "Cannot delete role because it is assigned to one or more people"
- **Actual**: HTTP 204 NoContent (successful deletion)
- **Actual Behavior**: Role deletion succeeds, PersonRole entries are automatically cascade deleted

## Technical Analysis

### Root Cause
The EF Core configuration in `PersonConfiguration.cs` sets up a many-to-many relationship between Person and Role using a join table `PersonRoles`:

```csharp
builder.HasMany(p => p.Roles)
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "PersonRole",
        j => j.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
        j => j.HasOne<Person>().WithMany().HasForeignKey("PersonId"),
        j =>
        {
            j.HasKey("PersonId", "RoleId");
            j.ToTable("PersonRoles");
        });
```

**The Issue**: By default, EF Core uses `CASCADE` delete behavior for required foreign keys. This means:
1. When a Role is deleted, all PersonRole entries with that RoleId are automatically deleted
2. This allows the Role deletion to complete successfully
3. The foreign key constraint exception is never thrown
4. The repository's catch block for foreign key violations never executes

### Expected vs Actual Flow

**Expected Flow (RESTRICT behavior)**:
1. DELETE /api/roles/{id} called
2. `DeleteRoleCommandHandler` calls `roleRepository.DeleteAsync()`
3. EF Core attempts to delete Role
4. Database foreign key constraint prevents deletion due to existing PersonRole references
5. EF Core throws `DbUpdateException` with foreign key constraint error
6. Repository catches exception and throws `InvalidOperationException`
7. `GlobalExceptionHandlingMiddleware` catches `InvalidOperationException`
8. Returns HTTP 409 Conflict with error message

**Actual Flow (CASCADE behavior)**:
1. DELETE /api/roles/{id} called
2. `DeleteRoleCommandHandler` calls `roleRepository.DeleteAsync()`
3. EF Core automatically deletes all PersonRole entries referencing the Role
4. EF Core successfully deletes the Role
5. Repository completes without exception
6. Controller returns HTTP 204 NoContent

### Evidence from Code Investigation

1. **Repository Exception Handling is Correct**: `EfRoleRepository.DeleteAsync()` has proper exception handling:
   ```csharp
   catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed") == true)
   {
       throw new InvalidOperationException("Cannot delete role because it is assigned to one or more people.", ex);
   }
   ```

2. **Global Exception Handling is Correct**: `GlobalExceptionHandlingMiddleware` properly converts `InvalidOperationException` to HTTP 409 Conflict

3. **Foreign Key Configuration Uses Default Behavior**: No explicit `DeleteBehavior` specified, defaults to CASCADE

## Impact Assessment

### Business Logic Impact
- **MEDIUM Risk**: The current behavior allows deletion of Roles that are still assigned to People
- **Data Integrity**: PersonRole relationships are silently removed without business validation
- **User Experience**: No warning or error when deleting Roles that are still in use
- **Audit Trail**: Role deletions succeed when they should be prevented

### Integration Test Impact
- **Test Coverage**: Foreign key constraint validation tests are ineffective
- **False Confidence**: Tests pass due to incorrect behavior rather than proper implementation
- **Regression Risk**: Changes to delete behavior might not be caught by tests

## Solution Approach

### Option 1: Configure RESTRICT Delete Behavior (Recommended)
Modify the EF Core configuration to explicitly set `DeleteBehavior.Restrict`:

```csharp
builder.HasMany(p => p.Roles)
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "PersonRole",
        j => j.HasOne<Role>().WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Restrict),
        j => j.HasOne<Person>().WithMany().HasForeignKey("PersonId").OnDelete(DeleteBehavior.Restrict),
        j =>
        {
            j.HasKey("PersonId", "RoleId");
            j.ToTable("PersonRoles");
        });
```

### Option 2: Business Logic Validation
Add explicit validation in the repository or command handler to check for existing relationships before deletion.

### Option 3: Combine Both Approaches
Use both database-level constraints (RESTRICT) and application-level validation for comprehensive protection.

## Implementation Plan

### Phase 1: Fix EF Core Configuration
1. Update `PersonConfiguration.cs` to use `DeleteBehavior.Restrict`
2. Create and run EF Core migration to update database schema
3. Verify foreign key constraints are properly enforced in database

### Phase 2: Validate Fix
1. Run foreign key constraint tests to verify they now fail as expected
2. Test manual deletion scenarios to ensure proper error handling
3. Verify no regression in other CRUD operations

### Phase 3: Consider Business Logic Enhancement
1. Evaluate if additional business logic validation is needed
2. Consider implementing soft delete or role reassignment workflows
3. Update user interface to handle constraint violations gracefully

## Testing Strategy

### Validation Tests
1. **Constraint Enforcement**: Verify Role deletion fails when PersonRole relationships exist
2. **Error Handling**: Confirm proper HTTP 409 Conflict responses with meaningful error messages
3. **Successful Deletion**: Ensure Role deletion still works when no relationships exist
4. **Cross-Provider**: Validate behavior across SQLite, InMemory, and SqlServer providers

### Expected Results After Fix
- `DELETE_Role_With_People_Should_Handle_Constraints_ForConstraintProviders(SQLite)` → PASS (HTTP 409 Conflict)
- `DELETE_Role_With_People_Should_Handle_Constraints_ForConstraintProviders(SqlServer)` → PASS (HTTP 409 Conflict)

## Related Components

- `PersonConfiguration.cs:25-36` - EF Core relationship configuration
- `EfRoleRepository.cs:76-78` - Foreign key exception handling
- `GlobalExceptionHandlingMiddleware.cs:77-82` - Exception to HTTP status mapping
- `PeopleControllerMultiProviderTests.cs:111-143` - Foreign key constraint tests

## Notes

This issue reveals that the foreign key constraint validation logic was implemented correctly at the repository and middleware layers, but the database configuration was preventing the constraints from being enforced. The fix should be straightforward but requires a database migration.

The current CASCADE behavior might have been acceptable for some use cases, but it violates the principle of explicit business rule enforcement that the integration tests were designed to validate.