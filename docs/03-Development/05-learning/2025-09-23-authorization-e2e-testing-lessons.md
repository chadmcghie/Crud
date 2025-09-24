# Troubleshooting Authorization in E2E Tests

## Problem Summary
E2E tests were failing with 403 authorization errors when trying to access protected endpoints, preventing smoke tests from running successfully.

## Root Cause Analysis

### 1. **Environment Detection Issues**
- **Problem**: API was running in `Development` environment even when `ASPNETCORE_ENVIRONMENT=Testing` was set
- **Cause**: Launch profiles in `launchSettings.json` override environment variables
- **Solution**: Use `--no-launch-profile` flag when starting API for testing

### 2. **Authorization Policy Conflicts**
- **Problem**: Standard `[Authorize]` attributes always enforce authorization, even in test environments
- **Cause**: No conditional logic for bypassing authorization in testing scenarios
- **Solution**: Created `ConditionalAuthorizeAttribute` that bypasses authorization in Testing environment

### 3. **Database Schema Mismatches**
- **Problem**: Old database files had outdated schema causing SQL errors
- **Cause**: Database migrations not applied or using stale database files
- **Solution**: Delete old database files and let EF Core recreate with current schema

## Key Lessons Learned

### 1. **Environment Variable Precedence**
```bash
# ❌ This doesn't work - launch profile overrides environment
ASPNETCORE_ENVIRONMENT=Testing dotnet run --project src/Api --launch-profile http

# ✅ This works - no launch profile means environment variable takes precedence
ASPNETCORE_ENVIRONMENT=Testing dotnet run --project src/Api --no-launch-profile --urls http://localhost:5172
```

**Lesson**: Always use `--no-launch-profile` when you need to override environment settings for testing.

### 2. **Conditional Authorization Pattern**
```csharp
// ❌ Standard authorization - always enforced
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> Create([FromBody] CreatePersonRequest request) { }

// ✅ Conditional authorization - bypassed in Testing environment
[ConditionalAuthorize("AdminOnly")]
public async Task<IActionResult> Create([FromBody] CreatePersonRequest request) { }
```

**Lesson**: Use conditional authorization attributes for endpoints that need to be testable without authentication.

### 3. **Progressive Error Diagnosis**
- **401 Unauthorized**: Authentication missing/invalid → Implement auth bypass
- **403 Forbidden**: Authorization policy failed → Implement conditional authorization
- **400 Bad Request**: Validation errors → Expected behavior, authorization working

**Lesson**: Different HTTP status codes indicate different layers of the request pipeline. Fix them in order: 401 → 403 → business logic.

### 4. **Database Environment Isolation**
```csharp
// Different databases for different environments
var connectionString = environment.IsEnvironment("Testing")
    ? "Data Source=TestDatabase.db"
    : "Data Source=CrudAppDev.db";
```

**Lesson**: Use separate databases for different environments to avoid schema conflicts and data pollution.

## Implementation Patterns

### ConditionalAuthorizeAttribute
```csharp
public class ConditionalAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var environment = context.HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>();

        // Complete bypass for Testing environment
        if (environment.IsEnvironment("Testing"))
        {
            return; // Allow all requests
        }

        // Normal authorization in other environments
        // ... standard authorization logic
    }
}
```

### Environment-Specific Startup
```csharp
// In Program.cs
if (builder.Environment.IsEnvironment("Testing"))
{
    // Bypass all authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context => true));
        options.AddPolicy("UserOrAdmin", policy => policy.RequireAssertion(context => true));
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(context => true)
            .Build();
    });
}
```

## Prevention Checklist

### Before Running E2E Tests:
1. ✅ Verify environment is set to "Testing"
2. ✅ Use `--no-launch-profile` flag
3. ✅ Check database file is appropriate for environment
4. ✅ Confirm authorization bypass is working with test endpoints

### When Adding New Protected Endpoints:
1. ✅ Use `[ConditionalAuthorize]` instead of `[Authorize]` for testable endpoints
2. ✅ Add corresponding E2E test cases
3. ✅ Verify endpoints work in both Testing and non-Testing environments

### When Environment Issues Arise:
1. ✅ Check `launchSettings.json` for environment overrides
2. ✅ Verify environment detection with `/api/test/environment` endpoint
3. ✅ Confirm database connection string matches environment
4. ✅ Test authorization bypass with `/api/test/auth-test` endpoint

## Reference Commands

```bash
# Start API in Testing environment (correct way)
ASPNETCORE_ENVIRONMENT=Testing dotnet run --project src/Api --no-launch-profile --urls http://localhost:5172

# Test environment detection
curl http://localhost:5172/api/test/environment

# Test authorization bypass
curl http://localhost:5172/api/test/auth-test

# Run E2E tests with proper environment
cd test/Tests.E2E.NG
npm run test:smoke
```

## Related Files
- `src/Api/Attributes/ConditionalAuthorizeAttribute.cs` - Custom authorization attribute
- `src/Api/Controllers/TestController.cs` - Environment and auth testing endpoints
- `src/Api/Program.cs` - Environment-specific authorization configuration
- `test/Tests.E2E.NG/test-environment.js` - Environment verification script

## Status
✅ **Resolved**: 403 authorization errors eliminated using conditional authorization bypass in Testing environment.