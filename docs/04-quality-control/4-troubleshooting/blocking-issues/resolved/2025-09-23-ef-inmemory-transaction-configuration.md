---
id: BI-2025-09-23-005
status: resolved
category: test
severity: low
created: 2025-09-23 08:45
resolved: 2025-09-23 15:15
spec: troubleshoot/integration-test-blockers
task: ef-inmemory-transaction-handling
---

# Entity Framework InMemory Transaction Configuration Issue

## Problem Statement
Entity Framework InMemory provider transaction warning is not properly suppressed, causing a test that expects a warning to instead receive an exception. This indicates DbContext configuration for test scenarios is not handling InMemory provider limitations correctly.

## Symptoms
- TransactionHandlingDifferenceTests.InMemory_Should_Have_Limited_Transaction_Support test failing
- Error: "Transactions are not supported by the in-memory store" exception instead of expected warning
- Test expects transaction operations to generate warnings but configuration throws exceptions
- InMemory provider transaction handling not configured for test scenarios

## Impact
- **Affected Tests**: 1 test failure (transaction handling validation)
- **Business Impact**: Minimal - does not affect production functionality
- **Development Impact**: Test configuration inconsistency for InMemory provider
- **Testing Risk**: DbContext configuration may not properly handle provider-specific limitations
- **Components Affected**: DbContext configuration, InMemory provider setup, transaction handling tests

## Root Cause Analysis (Five Whys)
1. **Why** is the InMemory transaction handling test failing?
   - Test expects warning but receives exception when using transactions with InMemory provider
2. **Why** does the test receive an exception instead of a warning?
   - DbContext configuration for InMemory provider not set up to suppress transaction exceptions
3. **Why** is DbContext configuration not suppressing transaction exceptions for InMemory provider?
   - InMemory provider configuration doesn't include proper warning handling setup
4. **Why** doesn't InMemory provider configuration include proper warning handling?
   - Test setup assumes InMemory provider will handle transactions gracefully with warnings
5. **Why** does test setup make this assumption? (ROOT CAUSE)
   - Entity Framework InMemory provider behavior expectations not aligned with actual implementation

## Attempted Solutions

### Attempt 1: [2025-09-23 08:25]
**Approach**: Analysis of failing test expectations and InMemory provider behavior
**Result**: Identified mismatch between expected warning behavior and actual exception throwing
**Files Modified**:
- No files modified (investigation only)
**Key Learning**: InMemory provider transaction handling needs specific configuration to match test expectations

### Attempt 2: [2025-09-23 15:10]
**Approach**: Configure InMemory provider to suppress TransactionIgnoredWarning using ConfigureWarnings
**Result**: SUCCESS - Test now passes without throwing exception
**Files Modified**:
- test/Tests.Integration.Backend/Infrastructure/InMemoryTestWebApplicationFactory.cs (lines 63-64): Added ConfigureWarnings to suppress InMemoryEventId.TransactionIgnoredWarning
- test/Tests.Unit.Backend/Infrastructure/EfRepositoryTests.cs (lines 29-30): Added ConfigureWarnings to suppress InMemoryEventId.TransactionIgnoredWarning
**Key Learning**: Entity Framework InMemory provider throws exception instead of warning by default when transactions are used - must explicitly configure warnings suppression using Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning

## Permanent Solution

**Resolved**: 2025-09-23 15:15
**Solution Summary**: Configured Entity Framework InMemory provider to suppress TransactionIgnoredWarning using ConfigureWarnings method

**Implementation**:
```csharp
// In InMemoryTestWebApplicationFactory.cs
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase(_databaseName);
    options.EnableSensitiveDataLogging();
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
});

// In EfRepositoryTests.cs
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
    .Options;
```

**Why This Works**:
Entity Framework InMemory provider doesn't support real transactions but allows transaction syntax. By default, it throws an exception when transactions are attempted. The ConfigureWarnings method with Ignore allows the test to proceed by suppressing the TransactionIgnoredWarning, enabling the test to verify that InMemory provider accepts transaction calls but doesn't actually perform rollbacks.

**Changes Made**:
- File: test/Tests.Integration.Backend/Infrastructure/InMemoryTestWebApplicationFactory.cs - Change: Added ConfigureWarnings to suppress transaction warning
- File: test/Tests.Unit.Backend/Infrastructure/EfRepositoryTests.cs - Change: Added ConfigureWarnings to suppress transaction warning

**Lessons Learned**:
- Entity Framework InMemory provider behavior differs between versions and configurations
- Transaction support testing requires specific warning suppression configuration
- ConfigureWarnings is the standard Microsoft-recommended approach for suppressing EF Core warnings
- Always use Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId namespace for warning IDs

**Prevention**:
When setting up InMemory provider for testing, always include ConfigureWarnings for transaction warnings if transaction-related functionality will be tested

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [✓] File: test/Tests.Integration.Backend/Infrastructure/InMemoryTestWebApplicationFactory.cs - Lines: 63-64 - Change: Added ConfigureWarnings for InMemory transaction warning suppression - Reason: Required for InMemory provider transaction handling tests

## Related Issues
- Link to integration test troubleshooting branch: troubleshoot/integration-test-blockers
- Link to spec task: EF InMemory transaction handling
- Resolution validates that InMemory provider configuration is now consistent across test infrastructure