# Technical Specification

> Parent Spec: Health Check Consolidation
> Created: 2025-09-29

## Current Architecture Analysis

### Existing Health Check Systems

1. **HealthController** (`src/Api/Controllers/HealthController.cs`)
   - Route: `/health`
   - Methods: GET `/health`, GET `/health/quick`
   - Logic: Custom `CanConnectAsync()` database test
   - Dependencies: Injects `HealthCheckService` but doesn't use it
   - Used by: Integration tests

2. **ApiHealthController** (`src/Api/Controllers/ApiHealthController.cs`)
   - Route: `/api/health`
   - Methods: GET `/api/health`
   - Logic: Calls `HealthCheckService.CheckHealthAsync()` + custom DB test
   - Returns: Detailed JSON with environment, DB provider, app version
   - Used by: Manual testing, monitoring dashboards

3. **ASP.NET Core Health Checks** (Middleware)
   - Configuration: `builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>`
   - Endpoints: `/health/system` (liveness), `/health/ready` (readiness)
   - Logic: Uses `DatabaseHealthCheck` class with `CanConnectAsync()` + `CountAsync()`
   - Used by: Playwright (readiness), Kubernetes probes (intended)

### Problems with Current Implementation

- **Code Duplication**: Three separate implementations of essentially the same functionality
- **Inconsistent Behavior**: Each system tests database connectivity differently
- **Unused Dependencies**: HealthController injects but doesn't use HealthCheckService
- **Confusing Endpoint Structure**: 5 different endpoints with unclear purposes
- **Maintenance Burden**: Changes require updating multiple locations

## Proposed Architecture

### Target State: Single Health Check System

Use only ASP.NET Core Health Checks middleware with tag-based filtering for different probe types.

#### Components to Remove
- `src/Api/Controllers/HealthController.cs` - Delete entire file
- `src/Api/Controllers/ApiHealthController.cs` - Delete entire file

#### Components to Keep/Modify
- `src/Api/HealthChecks/DatabaseHealthCheck.cs` - Keep (already has warm-up logic)
- `src/Api/Program.cs` - Modify health check registration and endpoint mapping

### Health Check Registration

```csharp
// Program.cs - Service Registration
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "live" });
```

**Tags Explained**:
- `"live"` - Liveness check (is app alive? should it be restarted?)
- `"ready"` - Readiness check (is app ready for traffic? should it receive requests?)

### Endpoint Mapping

```csharp
// Program.cs - Endpoint Mapping

// Liveness probe - checks if app is alive (all checks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Readiness probe - checks if app is ready for traffic (warm-up complete)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Detailed diagnostics - verbose information for troubleshooting
app.MapHealthChecks("/health/detailed", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteDetailedHealthResponse
});
```

### Custom Response Writer for Detailed Endpoint

Create a helper method to match the detailed response format previously provided by ApiHealthController.

## Migration Path

### Integration Test Updates

Current tests use `/health` (HealthController). After migration, update to use `/health` (middleware).

**Files to Update**:
- `test/Tests.Integration.Backend/SmokeTests/HealthEndpointSmokeTests.cs`
- `test/Tests.Integration.Backend/Configuration/MultiEnvironmentSmokeTests.cs`
- `test/Tests.Integration.Backend/SmokeTests/SmokeTestPerformanceValidation.cs`

**Test Expectations**:
- Response format will change from `{"status": "Healthy"}` to ASP.NET Core health check JSON format
- HTTP status codes remain the same: 200 (Healthy), 503 (Unhealthy)

### Playwright Configuration

No changes needed - already using `/health/ready` which will remain.

## Implementation Order

1. Add custom response writer for detailed endpoint
2. Update health check registration with proper tags
3. Remap endpoints to new structure
4. Update integration tests to expect new response format
5. Delete HealthController.cs
6. Delete ApiHealthController.cs
7. Run full test suite to verify no regressions
8. Update documentation

## Rollback Plan

If issues arise during deployment:
1. Revert Program.cs endpoint mapping changes
2. Restore deleted controller files from git history
3. Revert integration test changes
4. Deploy previous version
