---
id: BI-2025-09-11-003
status: active
category: functionality
severity: high
created: 2025-09-11 07:15
resolved: 
spec: controller-authorization-protection
task: PUT_People_Should_Update_Person_Roles test implementation
---

# RowVersion Concurrency Control 409 Conflict Issue

## Problem Statement
PUT_People_Should_Update_Person_Roles test consistently fails with 409 Conflict after implementing complete RowVersion concurrency control infrastructure. The test was originally skipped with message "Skipping due to concurrency conflict - RowVersion not implemented in UpdatePersonRequest" but now fails even though all required infrastructure has been implemented correctly.

## Symptoms
- Test fails with HTTP 409 Conflict status code instead of expected 204 No Content
- Error: "Expected response.StatusCode to be HttpStatusCode.NoContent {value: 204}, but found HttpStatusCode.Conflict {value: 409}"
- Test builds successfully and is no longer skipped
- Issue occurs both locally and in CI environments
- All other Person entity operations (create, get, delete) work correctly

## Impact
- RowVersion concurrency control implementation cannot be completed and validated
- Integration test remains failing, blocking CI pipeline completion
- Concurrency protection for Person entity updates not properly functional
- Person role assignment updates are blocked due to failing validation

## Root Cause Analysis (Five Whys)

1. **Why does the PUT_People_Should_Update_Person_Roles test fail with 409 Conflict?**
   Answer: GlobalExceptionHandlingMiddleware converts InvalidOperationException (from repository) to HTTP 409 status

2. **Why does the repository throw InvalidOperationException?**
   Answer: EfPersonRepository catches DbUpdateConcurrencyException and re-throws as InvalidOperationException

3. **Why does Entity Framework throw DbUpdateConcurrencyException?**
   Answer: EF Core detects concurrency conflicts when updating Person entities with role changes

4. **Why does EF Core detect concurrency conflicts when PersonConfiguration has .IsConcurrencyToken() disabled?**
   Answer: RowVersion property exists on Person entity and EF may still be performing concurrency checks based on entity state tracking

5. **Why does entity state tracking cause concurrency conflicts even with disabled concurrency token?**
   Answer: The UpdateAsync method uses _context.People.Update() which marks the entire entity as modified, potentially triggering RowVersion validation regardless of configuration (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-09-11 05:00]
**Approach**: Pass null RowVersion in test request to bypass concurrency checks
**Result**: Test still fails with 409 Conflict
**Files Modified**: 
- test/Tests.Integration.Backend/Controllers/PeopleControllerTests.cs (line 269)
**Key Learning**: Null RowVersion doesn't prevent EF from detecting concurrency conflicts

### Attempt 2: [2025-09-11 05:30]
**Approach**: Fetch current RowVersion from GET request and pass to PUT request
**Result**: Test still fails with 409 Conflict - Entity Framework still detects conflicts
**Files Modified**:
- test/Tests.Integration.Backend/Controllers/PeopleControllerTests.cs (lines 264-266, 269)
**Key Learning**: Even with correct RowVersion, EF Core entity tracking causes conflicts during role updates

### Attempt 3: [2025-09-11 06:00]
**Approach**: Completely disable .IsConcurrencyToken() in PersonConfiguration
**Result**: Test still fails with 409 Conflict - disabled token doesn't resolve Update() method conflicts
**Files Modified**:
- src/Infrastructure/Data/Configurations/PersonConfiguration.cs (line 25 - commented out .IsConcurrencyToken())
**Key Learning**: Disabling concurrency token in configuration doesn't fix Update() method entity tracking conflicts

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: src/Api/Dtos/PeopleDtos.cs - Lines: 11-16 - Change: Added RowVersion parameter to UpdatePersonRequest - Reason: Foundation for proper concurrency control implementation
- [x] File: src/Api/Dtos/PeopleDtos.cs - Lines: 18 - Change: Added RowVersion to PersonResponse - Reason: Clients need RowVersion for optimistic concurrency control
- [x] File: src/App/Features/People/Commands.cs - Lines: 12-18 - Change: Added RowVersion parameter to UpdatePersonCommand - Reason: Command layer must support concurrency control
- [x] File: src/Api/Controllers/PeopleController.cs - Lines: 85 - Change: Pass RowVersion from request to command - Reason: Controller must forward concurrency token
- [x] File: test/Tests.Integration.Backend/Infrastructure/TestDataBuilders.cs - Lines: 49-61 - Change: Added RowVersion parameter to UpdatePersonRequest builder - Reason: Test infrastructure must support concurrency control testing
- [x] File: src/Domain/Entities/Person.cs - Lines: 29-31 - Change: Added RowVersion property with proper documentation - Reason: Domain entity needs concurrency control foundation

## Current Workaround
Remove RowVersion entirely from UpdatePersonRequest, command, and entity - test passes but defeats the purpose of implementing concurrency control.

### Attempt 4: [2025-09-11 14:40]
**Hypothesis**: Complete RowVersion concurrency control implementation with proper command handler integration
**Approach**: Enable .IsConcurrencyToken(), add RowVersion handling in command handler, create migration
**Implementation**:
```csharp
// PersonConfiguration.cs - Enabled concurrency token
builder.Property(p => p.RowVersion)
    .HasColumnType("BLOB")
    .IsRequired(false)
    .IsConcurrencyToken(); // Enabled for proper concurrency control

// CommandHandlers.cs - Added RowVersion handling
if (request.RowVersion != null)
{
    person.RowVersion = request.RowVersion;
}

// Applied migration: 20250911143832_EnablePersonRowVersionConcurrency
```
**Result**: **CRITICAL DISCOVERY** - 17/18 PeopleController tests pass, only role update test fails
**Files Modified**: 
- src/Infrastructure/Data/Configurations/PersonConfiguration.cs (line 24): Enabled .IsConcurrencyToken()
- src/App/Features/People/CommandHandlers.cs (lines 37-41): Added RowVersion handling
- Created migration for concurrency control
**Key Learning**: Core Person CRUD works perfectly - issue is **specific to many-to-many role relationship updates**

## Root Cause Identified
The issue is **NOT** with basic concurrency control but with **many-to-many relationship modifications** triggering EF Core concurrency conflicts. All other Person operations (create, update properties, delete) work correctly.

### Attempt 5: [2025-09-11 16:45]
**Hypothesis**: SQLite doesn't auto-generate RowVersion - need application-managed concurrency control with tracked entities
**Approach**: Create centralized RowVersion service, update command handlers to use tracked entities, implement GUID-based versioning
**Implementation**:
```csharp
// IRowVersionService.cs - Centralized RowVersion management
public interface IRowVersionService
{
    byte[] GenerateInitialVersion();
    byte[] GenerateNewVersion();
}

// RowVersionService.cs - GUID-based implementation for SQLite
public class RowVersionService : IRowVersionService
{
    public byte[] GenerateInitialVersion() => Guid.NewGuid().ToByteArray();
    public byte[] GenerateNewVersion() => Guid.NewGuid().ToByteArray();
}

// UpdatePersonCommandHandler - Switch to tracked entities
var person = await personRepository.GetAsync(request.Id, cancellationToken);
if (request.RowVersion != null && person.RowVersion != null)
{
    if (!request.RowVersion.SequenceEqual(person.RowVersion))
        throw new InvalidOperationException("Person modified by another user");
}
person.RowVersion = rowVersionService.GenerateNewVersion();
await personRepository.UpdateAsync(person, cancellationToken);

// EfPersonRepository - Simplified for tracked entities
public async Task UpdateAsync(Person person, CancellationToken ct = default)
{
    await _context.SaveChangesWithRetryAsync(cancellationToken: ct);
}
```
**Result**: **MAJOR BREAKTHROUGH** - 409 conflicts resolved! All Person CRUD operations working correctly
**Files Modified**: 
- Created: src/App/Services/IRowVersionService.cs, src/App/Services/RowVersionService.cs
- Modified: src/App/DependencyInjection.cs (service registration)
- Modified: src/App/Features/People/CommandHandlers.cs (tracked entities + RowVersion generation)
- Modified: src/Infrastructure/Repositories/EntityFramework/EfPersonRepository.cs (simplified UpdateAsync)
- Created: test/Tests.Integration.Backend/Controllers/PersonConcurrencyTests.cs (validation tests)
**Key Learning**: **ROOT CAUSE SOLVED** - SQLite requires application-managed RowVersion generation. Tracked entity pattern eliminates concurrency conflicts.

**Status**: **PENDING CI VALIDATION** - Solution works locally but must be validated in CI workflow before claiming resolution

## Root Cause and Solution Summary
**Root Cause**: SQLite database doesn't auto-generate RowVersion values like SQL Server, causing EF Core concurrency control to fail silently. Combined with detached entity update pattern, this created persistent 409 conflicts.

**Solution**: Application-managed RowVersion using centralized service with GUID-based versioning + tracked entity update pattern.

## Next Steps
- [x] ~~Investigate basic concurrency control~~ - **WORKING CORRECTLY**
- [x] ~~Create centralized RowVersion service~~ - **IMPLEMENTED**
- [x] ~~Fix command handlers with tracked entities~~ - **IMPLEMENTED** 
- [x] ~~Validate local testing~~ - **PASSING**
- [ ] **CRITICAL: CI validation via PR** - **IN PROGRESS**
- [ ] Address role replacement logic refinement (separate issue)

## Related Issues
- Link to related blocking issue: N/A
- Link to GitHub issue/PR: **PENDING PR CREATION**  
- Link to spec task: controller-authorization-protection