---
id: BI-2025-09-09-001
status: resolved
category: test
severity: medium
created: 2025-09-09 00:25
resolved: 2025-09-09 00:35
spec: redis-caching-layer
task: integration-test-fixes
---

# Password Reset Tests Database Isolation Failure

## Problem Statement
Password reset integration tests are failing due to database isolation issues. Tests are encountering duplicate email constraint violations when trying to seed test users, indicating that the database cleanup between tests is not functioning properly.

## Symptoms
- Error: `SQLite Error 19: 'UNIQUE constraint failed: Users.Email'`
- Occurs when running multiple password reset tests together
- Tests pass when run individually but fail when run as a suite
- Environment: Integration test environment with SQLite

## Impact
- 4 password reset tests consistently failing
- Integration test suite reliability compromised
- CI/CD pipeline may fail intermittently
- Affects password reset feature testing coverage

## Root Cause Analysis (Five Whys)
1. Why did password reset tests fail? 
   Answer: Duplicate email constraint violation when seeding test user
2. Why is there a duplicate email? 
   Answer: Previous test data not cleared from database
3. Why isn't the database cleared?
   Answer: ClearDatabaseAsync method may not be properly cleaning Users table
4. Why isn't the Users table being cleaned?
   Answer: Possible transaction isolation or connection pooling issue
5. Why is there a connection pooling issue?
   Answer: Multiple test instances may be sharing database connections (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-09-09 00:20]
**Approach**: Run tests individually to confirm isolation issue
**Result**: Tests pass when run individually
**Files Modified**: 
- None (diagnostic only)
**Key Learning**: Issue is definitely related to test isolation, not test logic

### Attempt 2: [2025-09-09 00:22]
**Approach**: Check RunWithCleanDatabaseAsync implementation
**Result**: Method appears correct but ClearDatabaseAsync may not be effective
**Files Modified**:
- None (investigation only)
**Key Learning**: Database cleanup logic exists but may not be executing properly

### Attempt 3: [2025-09-09 00:24]
**Approach**: Analyze test execution logs for patterns
**Result**: Confirmed duplicate key violations on Users.Email
**Files Modified**:
- None (analysis only)
**Key Learning**: Problem consistently occurs with test@example.com email

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [x] File: src/Infrastructure/Services/DatabaseTestService.cs - Lines: 182-192 - Change: Added Users and PasswordResetTokens cleanup - Reason: Ensure proper test isolation for authentication tests
- [x] File: src/Infrastructure/Services/DatabaseTestService.cs - Lines: 261-262 - Change: Added Users and PasswordResetTokens to stats - Reason: Monitor cleanup effectiveness
- [x] File: src/Infrastructure/Services/DatabaseTestService.cs - Lines: 287-290 - Change: Added validation for Users and PasswordResetTokens - Reason: Detect incomplete cleanup

## Current Workaround
~~Run password reset tests individually using test filters~~
No longer needed - issue resolved.

## Next Steps
- [x] Investigate ClearDatabaseAsync implementation in SqliteTestWebApplicationFactory
- [x] Check if database connections are properly disposed between tests
- [x] Fix ResetWithEfCoreAsync to clean Users and PasswordResetTokens tables
- [x] Add Users and PasswordResetTokens to database stats and validation
- [x] Verify all integration tests pass

## Resolution Summary
The root cause was that the `ResetWithEfCoreAsync` method in `DatabaseTestService` was not cleaning up the Users and PasswordResetTokens tables between test runs. This caused duplicate email constraint violations when password reset tests tried to seed test users.

The fix involved:
1. Adding cleanup for Users and PasswordResetTokens tables in the correct order (tokens first due to FK)
2. Updating database stats and validation methods to monitor these tables
3. Ensuring proper cleanup order to respect foreign key constraints

All 110 integration tests now pass successfully.

## Related Issues
- Link to related blocking issue: None
- Link to GitHub issue/PR: https://github.com/chadmcghie/Crud/pull/170
- Link to spec task: .agent-os/specs/redis-caching-layer/tasks.md