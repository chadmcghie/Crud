# Conflict Resolution Guide for PR #71

## Summary
Most conflicts are minor (whitespace/formatting) except for DatabaseTestService.cs where we need to remove the Respawn code.

## Conflict Resolution Strategy

### 1. DatabaseTestService.cs
**Resolution**: Accept `review/deprecated` version (removes Respawn code)
- Lines 72-83: Keep only line 82-83 (Use EF Core cleanup comment + method call)
- Lines 180-203: Remove entire TryResetWithRespawnAsync method (lines 180-201)
- Lines 215-225: Keep simplified comment from review/deprecated (lines 223-224)

### 2. PeopleQueriesController.cs
**Resolution**: Accept `dev` version (no functional changes, just whitespace)

### 3. DependencyInjection.cs
**Resolution**: Accept `dev` version (no functional changes, just whitespace)

### 4. PersonQueryService.cs
**Resolution**: Accept `dev` version (keep new query service functionality)

### 5. IRepository.cs
**Resolution**: Accept `dev` version (keep new repository interface methods)

### 6. PersonSpecifications.cs
**Resolution**: Accept `dev` version (keep new specifications)

### 7. EfRepository.cs
**Resolution**: Accept `dev` version (keep new EF implementation)

### 8. PollyPolicies.cs
**Resolution**: Accept `dev` version (keep resilience policies)

### 9. ResilientDbContext.cs
**Resolution**: Accept `dev` version (keep resilient context)

### 10-12. Test Files
**Resolution**: Accept `dev` version for all test files (keep new tests)

## Commands to Execute

```bash
# Start fresh
git checkout review/deprecated
git pull origin dev

# Resolve conflicts
# For DatabaseTestService.cs - manually edit to remove Respawn
# For all others - accept dev version

git add .
git commit -m "Merge dev into review/deprecated - removed deprecated Respawn code"
git push origin review/deprecated
```

## What Gets Removed
- Respawn integration code (never worked with SQLite)
- Obsolete comments about removed AnyAsync() checks
- TryResetWithRespawnAsync method entirely

## What Gets Kept
- All new features from dev branch
- New query services and specifications
- Resilience policies
- New tests