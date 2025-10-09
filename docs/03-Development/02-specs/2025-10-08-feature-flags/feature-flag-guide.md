# Feature Flag Configuration Guide

> **Created**: 2025-10-08
> **Spec**: Feature Flags / Feature Toggles
> **GitHub Issue**: #252

## Overview

This guide provides comprehensive information for operators and developers on configuring and managing feature flags in the CRUD application. Feature flags follow Martin Fowler's Feature Toggle architecture patterns.

## Table of Contents

1. [Feature Flag Types](#feature-flag-types)
2. [Configuration](#configuration)
3. [Feature Flag Inventory](#feature-flag-inventory)
4. [Environment-Specific Defaults](#environment-specific-defaults)
5. [Lifecycle Management](#lifecycle-management)
6. [Testing Strategy](#testing-strategy)
7. [Troubleshooting](#troubleshooting)

## Feature Flag Types

### Ops Toggles (Long-lived)
**Purpose**: Control operational characteristics and system behavior
**Lifecycle**: Long-lived, reviewed quarterly
**Examples**: Caching, Compression, Rate Limiting, OpenTelemetry

**When to Use**:
- Performance optimization features that may need to be disabled during incidents
- Observability features that have performance implications
- Security features like rate limiting and CORS

### Release Toggles (Short-lived)
**Purpose**: Gradual rollout of new features and migrations
**Lifecycle**: 1-2 weeks maximum, remove after feature is stable
**Examples**: Identity/Authentication migration, Email Service rollout

**When to Use**:
- Migrating from one implementation to another (e.g., JWT → ASP.NET Core Identity)
- Rolling out new third-party integrations (e.g., Mock → Real Email Service)
- Testing resilience policies before full deployment

**Important**: Set expiration dates and remove toggles promptly to avoid technical debt.

### Permission Toggles (Long-lived)
**Purpose**: Environment-based feature access control
**Lifecycle**: Long-lived, reviewed annually
**Examples**: Swagger UI, Database Seeding, Detailed Error Messages

**When to Use**:
- Features that should only be enabled in specific environments
- Security-sensitive features (e.g., detailed health checks, exception information)
- Development/testing tools (e.g., Swagger, database seeding)

## Configuration

### Configuration File Structure

Feature flags are configured in `appsettings.{Environment}.json` using the `FeatureManagement` section:

```json
{
  "FeatureManagement": {
    "Caching": true,
    "Compression": true,
    "RateLimiting": true,
    "OpenTelemetry": true,
    "Cors": true,
    "HealthChecksDetailed": false,
    "DynamicLogLevel": false,
    "IdentityAuthentication": false,
    "EmailService": false,
    "Resilience": true,
    "Swagger": false,
    "DatabaseSeeding": false,
    "DetailedExceptions": false,
    "ConditionalRequests": true
  }
}
```

### Environment Variable Override

Feature flags can be overridden using environment variables:

```bash
# Format: FeatureManagement__{FlagName}
export FeatureManagement__Caching=false
export FeatureManagement__Swagger=true
```

### Runtime Access

```csharp
// Inject IFeatureManager
private readonly IFeatureManager _featureManager;

// Check if feature is enabled
if (await _featureManager.IsEnabledAsync(FeatureFlags.Caching))
{
    // Feature is enabled
}
```

## Feature Flag Inventory

### Ops Toggles (7 features)

| Flag Name | Constant | Purpose | Default (Prod) | Review Frequency |
|-----------|----------|---------|----------------|------------------|
| `Caching` | `FeatureFlags.Caching` | LazyCache, Redis, Output Cache | ✅ Enabled | Quarterly |
| `Compression` | `FeatureFlags.Compression` | Brotli, Gzip response compression | ✅ Enabled | Quarterly |
| `RateLimiting` | `FeatureFlags.RateLimiting` | API rate limiting | ✅ Enabled | Quarterly |
| `OpenTelemetry` | `FeatureFlags.OpenTelemetry` | Tracing and metrics | ✅ Enabled | Quarterly |
| `Cors` | `FeatureFlags.Cors` | CORS policy | ✅ Enabled | Quarterly |
| `HealthChecksDetailed` | `FeatureFlags.HealthChecksDetailed` | /health/detailed endpoint | ❌ Disabled | Quarterly |
| `DynamicLogLevel` | `FeatureFlags.DynamicLogLevel` | Runtime log level adjustment | ❌ Disabled | Quarterly |

### Release Toggles (3 features)

| Flag Name | Constant | Purpose | Default (Prod) | Expiration Date |
|-----------|----------|---------|----------------|-----------------|
| `IdentityAuthentication` | `FeatureFlags.IdentityAuthentication` | ASP.NET Core Identity migration | ❌ Disabled (JWT) | TBD (1-2 weeks post-stable) |
| `EmailService` | `FeatureFlags.EmailService` | Real email service rollout | ❌ Disabled (Mock) | TBD (1-2 weeks post-stable) |
| `Resilience` | `FeatureFlags.Resilience` | Polly resilience policies | ✅ Enabled | TBD (1-2 weeks post-stable) |

### Permission Toggles (4 features)

| Flag Name | Constant | Purpose | Default (Prod) | Review Frequency |
|-----------|----------|---------|----------------|------------------|
| `Swagger` | `FeatureFlags.Swagger` | Swagger UI and OpenAPI docs | ❌ Disabled | Annually |
| `DatabaseSeeding` | `FeatureFlags.DatabaseSeeding` | Automatic database seeding | ❌ Disabled | Annually |
| `DetailedExceptions` | `FeatureFlags.DetailedExceptions` | Detailed exception information | ❌ Disabled | Annually |
| `ConditionalRequests` | `FeatureFlags.ConditionalRequests` | ETag, If-Modified-Since headers | ✅ Enabled | Annually |

## Environment-Specific Defaults

### Production Environment (`appsettings.Production.json`)

```json
{
  "FeatureManagement": {
    // Ops Toggles - All enabled for performance
    "Caching": true,
    "Compression": true,
    "RateLimiting": true,
    "OpenTelemetry": true,
    "Cors": true,
    "HealthChecksDetailed": false,  // Security: Hide detailed diagnostics
    "DynamicLogLevel": false,

    // Release Toggles - Conservative defaults
    "IdentityAuthentication": false,  // JWT until migration complete
    "EmailService": false,            // Mock until service validated
    "Resilience": true,               // Enabled for fault tolerance

    // Permission Toggles - Security-focused
    "Swagger": false,                 // Security: No API docs in prod
    "DatabaseSeeding": false,         // Security: No auto-seeding
    "DetailedExceptions": false,      // Security: Sanitized errors only
    "ConditionalRequests": true       // Performance: Enable caching
  }
}
```

### Development Environment (`appsettings.Development.json`)

```json
{
  "FeatureManagement": {
    // Ops Toggles - All enabled for testing
    "Caching": true,
    "Compression": true,
    "RateLimiting": true,
    "OpenTelemetry": true,
    "Cors": true,
    "HealthChecksDetailed": true,   // Dev: Full diagnostics
    "DynamicLogLevel": false,

    // Release Toggles - Testing configurations
    "IdentityAuthentication": false,
    "EmailService": false,
    "Resilience": true,

    // Permission Toggles - Developer-friendly
    "Swagger": true,                // Dev: API documentation
    "DatabaseSeeding": true,        // Dev: Auto-populate test data
    "DetailedExceptions": true,     // Dev: Full error details
    "ConditionalRequests": true
  }
}
```

### Testing Environment (`appsettings.Testing.json`)

```json
{
  "FeatureManagement": {
    // Ops Toggles - Test-optimized
    "Caching": true,
    "Compression": true,
    "RateLimiting": true,           // Relaxed limits for tests
    "OpenTelemetry": true,
    "Cors": true,
    "HealthChecksDetailed": true,
    "DynamicLogLevel": false,

    // Release Toggles - Test configurations
    "IdentityAuthentication": false,
    "EmailService": false,          // Mock for predictable tests
    "Resilience": true,

    // Permission Toggles - Testing-friendly
    "Swagger": true,
    "DatabaseSeeding": true,        // Test: Seed test data
    "DetailedExceptions": true,     // Test: Full error details for debugging
    "ConditionalRequests": true
  }
}
```

## Lifecycle Management

### Release Toggle Lifecycle

Release toggles have a strict lifecycle to prevent technical debt:

1. **Creation** (Day 0)
   - Add toggle to configuration
   - Set expiration date (1-2 weeks from stability)
   - Document removal plan

2. **Testing** (Days 1-7)
   - Test both enabled/disabled states
   - Validate migration path
   - Monitor metrics

3. **Gradual Rollout** (Days 7-14)
   - Enable in development → staging → production
   - Monitor for issues
   - Rollback plan ready

4. **Stabilization** (Days 14-28)
   - Feature proven stable
   - No rollbacks in 1 week
   - Prepare for toggle removal

5. **Removal** (Day 28+)
   - Remove toggle code
   - Update configuration files
   - Update documentation
   - Deploy cleanup

### Ops/Permission Toggle Review

Long-lived toggles require periodic review:

- **Quarterly Review** (Ops Toggles)
  - Validate toggle still needed
  - Review default states
  - Check for performance impact
  - Update documentation

- **Annual Review** (Permission Toggles)
  - Validate security requirements
  - Review environment-specific needs
  - Update compliance documentation

## Testing Strategy

### Unit Tests

Test feature flag service independently:

```csharp
[Fact]
public async Task FeatureFlag_ShouldBeDisabled_WhenConfiguredFalse()
{
    // Arrange
    var config = new Dictionary<string, string>
    {
        ["FeatureManagement:Caching"] = "false"
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(config)
        .Build();

    var featureManager = // ... create feature manager

    // Act
    var isEnabled = await featureManager.IsEnabledAsync("Caching");

    // Assert
    Assert.False(isEnabled);
}
```

### Integration Tests

Test feature-flagged services through WebApplicationFactory:

```csharp
[Fact]
public async Task Caching_ShouldBeDisabled_WhenFeatureFlagOff()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["FeatureManagement:Caching"] = "false"
                });
            });
        });

    // Act & Assert - Test behavior without caching
}
```

### E2E Tests

Validate production-like configurations:

```bash
# Test with production configuration
ASPNETCORE_ENVIRONMENT=Production npm run test:smoke
```

## Troubleshooting

### Feature Not Behaving as Expected

1. **Check Configuration**
   ```bash
   # View effective configuration
   GET /api/diagnostics/feature-flags
   ```

2. **Verify Environment Variables**
   ```bash
   echo $FeatureManagement__Caching
   ```

3. **Check Application Logs**
   ```bash
   grep "feature flag" logs/log-*.txt
   ```

### Feature Flag Not Loading

1. **Validate JSON Syntax**
   - Check for trailing commas
   - Verify nested structure
   - Use JSON validator

2. **Check Configuration Priority**
   - appsettings.json (lowest)
   - appsettings.{Environment}.json
   - Environment variables (highest)

3. **Restart Application**
   - Feature flags load at startup
   - Configuration changes require restart

### Performance Issues

1. **Feature Flag Overhead**
   - `IFeatureManager` checks are cached
   - <1ms overhead per check
   - Use `IFeatureManagerSnapshot` for request-scoped caching

2. **Avoid Toggle Checks in Hot Paths**
   - Check at service registration time
   - Don't check on every request

## References

- **Martin Fowler - Feature Toggles**: https://martinfowler.com/articles/feature-toggles.html
- **Microsoft.FeatureManagement**: https://github.com/microsoft/FeatureManagement-Dotnet
- **Spec Document**: @docs/03-Development/02-specs/2025-10-08-feature-flags/spec.md
- **API Specification**: @docs/03-Development/02-specs/2025-10-08-feature-flags/sub-specs/api-spec.md
- **Technical Specification**: @docs/03-Development/02-specs/2025-10-08-feature-flags/sub-specs/technical-spec.md
