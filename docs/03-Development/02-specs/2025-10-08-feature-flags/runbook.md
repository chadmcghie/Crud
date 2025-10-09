# Feature Flag Runbook

> **Created**: 2025-10-08
> **Purpose**: Step-by-step guide for adding and removing feature flags

## Table of Contents

1. [Adding a New Feature Flag](#adding-a-new-feature-flag)
2. [Removing a Feature Flag](#removing-a-feature-flag)
3. [Testing a Feature Flag](#testing-a-feature-flag)
4. [Troubleshooting](#troubleshooting)

## Adding a New Feature Flag

### Step 1: Determine Toggle Type

Choose the appropriate toggle type based on your use case:

| Type | Use Case | Lifecycle |
|------|----------|-----------|
| **Ops Toggle** | Performance optimization, operational control | Long-lived (quarterly review) |
| **Release Toggle** | Gradual feature rollout, migration | Short-lived (1-2 weeks, then remove) |
| **Permission Toggle** | Environment-based access control | Long-lived (annual review) |

### Step 2: Add Constant to FeatureFlags Class

**File**: `src/Api/Constants/FeatureFlags.cs`

```csharp
public static class FeatureFlags
{
    // ... existing constants

    /// <summary>
    /// Controls [feature name and purpose]
    /// </summary>
    public const string YourFeatureName = "YourFeatureName";
}
```

### Step 3: Add Configuration to appsettings Files

Add to **all** environment-specific configuration files:

**Files**:
- `src/Api/appsettings.json` (base configuration)
- `src/Api/appsettings.Development.json`
- `src/Api/appsettings.Testing.json`
- `src/Api/appsettings.Production.json`

```json
{
  "FeatureManagement": {
    "ExistingFlag1": true,
    "ExistingFlag2": false,
    "YourFeatureName": false  // Add your flag with appropriate default
  }
}
```

**Environment-Specific Defaults**:
- **Development**: Usually `true` for testing
- **Testing**: Usually `true` for integration tests
- **Production**: Conservative defaults (often `false` initially)

### Step 4: Implement Feature Flag Check in Code

#### Option A: Service Registration (Preferred)

Wrap service registration with feature flag check in `Program.cs`:

```csharp
// Read feature flag at startup
var isYourFeatureEnabled = builder.Configuration.GetValue<bool>("FeatureManagement:YourFeatureName");

if (isYourFeatureEnabled)
{
    Log.Information("YourFeatureName feature flag is enabled - registering services");
    builder.Services.AddYourFeature();
}
else
{
    Log.Information("YourFeatureName feature flag is disabled - skipping registration");
}
```

#### Option B: Middleware Registration

Wrap middleware registration with feature flag check:

```csharp
if (isYourFeatureEnabled)
{
    app.UseYourMiddleware();
    Log.Information("YourMiddleware enabled (YourFeatureName feature flag: enabled)");
}
else
{
    Log.Information("YourMiddleware disabled (YourFeatureName feature flag: disabled)");
}
```

#### Option C: Runtime Check (Less Preferred)

Use `IFeatureManager` for runtime checks:

```csharp
public class YourService
{
    private readonly IFeatureManager _featureManager;

    public async Task DoSomething()
    {
        if (await _featureManager.IsEnabledAsync(FeatureFlags.YourFeatureName))
        {
            // Feature-enabled behavior
        }
        else
        {
            // Feature-disabled behavior
        }
    }
}
```

### Step 5: Write Tests

Create integration tests in `test/Tests.Integration.Backend/FeatureFlags/`:

**File**: `YourFeatureNameFeatureFlagTests.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Tests.Integration.Backend.Infrastructure;
using Xunit;

namespace Tests.Integration.Backend.FeatureFlags;

/// <summary>
/// Integration tests for YourFeatureName feature flag
/// Validates that [describe what the flag controls]
/// </summary>
public class YourFeatureNameFeatureFlagTests : IClassFixture<SqliteTestWebApplicationFactory>
{
    private readonly SqliteTestWebApplicationFactory _factory;

    public YourFeatureNameFeatureFlagTests(SqliteTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task YourFeatureName_FeatureFlag_CanBeChecked()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();

        // Act
        var isEnabled = await featureManager.IsEnabledAsync(Api.Constants.FeatureFlags.YourFeatureName);

        // Assert
        Assert.False(isEnabled); // Or True, depending on Testing environment default
    }

    [Theory]
    [InlineData("Development", true)]   // Adjust expected values
    [InlineData("Testing", true)]
    [InlineData("Production", false)]
    public void YourFeatureName_ShouldHaveCorrectDefaults_PerEnvironment(string environment, bool expectedState)
    {
        // Arrange
        var configuration = BuildConfigurationForEnvironment(environment);

        // Act
        var featureEnabled = configuration.GetValue<bool>("FeatureManagement:YourFeatureName");

        // Assert
        Assert.Equal(expectedState, featureEnabled);
    }

    #region Helper Methods

    private IConfiguration BuildConfigurationForEnvironment(string environment)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var repoRoot = currentDirectory;

        while (!Directory.Exists(Path.Combine(repoRoot, "src")) && Directory.GetParent(repoRoot) != null)
        {
            repoRoot = Directory.GetParent(repoRoot)!.FullName;
        }

        var apiConfigPath = Path.Combine(repoRoot, "src", "Api");

        var builder = new ConfigurationBuilder()
            .SetBasePath(apiConfigPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    #endregion
}
```

### Step 6: Run Tests

```bash
# Run feature flag tests
dotnet test test/Tests.Integration.Backend/Tests.Integration.Backend.csproj --filter "FullyQualifiedName~FeatureFlags"

# Run all integration tests
dotnet test test/Tests.Integration.Backend/Tests.Integration.Backend.csproj
```

### Step 7: Update Documentation

Update the following documentation:

1. **Feature Flag Inventory** in `feature-flag-guide.md`
2. **Task List** in `tasks.md` (if applicable)
3. **Spec Document** in `spec.md` (if applicable)

### Step 8: Set Expiration Date (Release Toggles Only)

For Release Toggles:

1. Add expiration date to code comment:
   ```csharp
   /// <summary>
   /// Controls gradual rollout of new email service
   /// Expiration: 2025-11-15 (remove after 2 weeks of stability)
   /// </summary>
   public const string EmailService = "EmailService";
   ```

2. Create calendar reminder for removal
3. Document removal plan in spec

## Removing a Feature Flag

### When to Remove

- **Release Toggles**: 1-2 weeks after feature is stable
- **Ops Toggles**: Never remove unless feature is deprecated
- **Permission Toggles**: Never remove unless security model changes

### Step 1: Verify Feature is Stable

```bash
# Check logs for errors related to feature
grep "YourFeatureName" logs/log-*.txt

# Verify no rollbacks in past week
git log --since="1 week ago" --grep="rollback"

# Check monitoring/metrics for issues
```

### Step 2: Update All Environments to Permanent State

Before removing, ensure all environments use the final state:

```json
// All appsettings files should have the feature enabled/disabled permanently
{
  "FeatureManagement": {
    "YourFeatureName": true  // Or false - the final state
  }
}
```

Deploy and verify stability.

### Step 3: Remove Feature Flag Checks from Code

```csharp
// BEFORE (with feature flag)
var isYourFeatureEnabled = builder.Configuration.GetValue<bool>("FeatureManagement:YourFeatureName");
if (isYourFeatureEnabled)
{
    builder.Services.AddYourFeature();
}

// AFTER (flag removed)
builder.Services.AddYourFeature();  // Now always registered
```

### Step 4: Remove Constant from FeatureFlags Class

Remove the constant definition from `src/Api/Constants/FeatureFlags.cs`.

### Step 5: Remove Configuration from appsettings Files

Remove from **all** appsettings files:

```json
{
  "FeatureManagement": {
    "OtherFlag": true,
    // "YourFeatureName": true,  <- Remove this line
  }
}
```

### Step 6: Remove or Update Tests

Either:
- **Remove** the feature flag test file entirely
- **Update** tests to remove feature flag checks (if testing the feature itself)

### Step 7: Update Documentation

1. Remove from **Feature Flag Inventory** in `feature-flag-guide.md`
2. Update **Task List** in `tasks.md` to mark as removed
3. Update **Spec Document** to note removal date

### Step 8: Commit and Deploy

```bash
# Format code
dotnet format solutions/Crud.sln

# Run tests
dotnet test

# Commit changes
git add .
git commit -m "feat: remove YourFeatureName feature flag

Feature has been stable for 2 weeks and is now permanently enabled.

- Removed FeatureFlags.YourFeatureName constant
- Removed configuration from all appsettings files
- Removed conditional registration in Program.cs
- Removed feature flag tests

🤖 Generated with Claude Code"

# Deploy through normal pipeline
```

## Testing a Feature Flag

### Test Both States

Always test both enabled and disabled states:

```csharp
[Theory]
[InlineData(true)]  // Feature enabled
[InlineData(false)] // Feature disabled
public async Task Feature_ShouldWork_InBothStates(bool featureEnabled)
{
    // Arrange
    var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["FeatureManagement:YourFeatureName"] = featureEnabled.ToString()
                });
            });
        });

    var client = factory.CreateClient();

    // Act & Assert
    // Test behavior in both states
}
```

### Local Testing

```bash
# Test with feature enabled
export FeatureManagement__YourFeatureName=true
dotnet run --project src/Api/Api.csproj

# Test with feature disabled
export FeatureManagement__YourFeatureName=false
dotnet run --project src/Api/Api.csproj
```

### Test in All Environments

1. **Development**: Test with default Development configuration
2. **Staging**: Test with Production-like configuration
3. **Production**: Gradual rollout with monitoring

## Troubleshooting

### Feature Flag Not Working

**Symptom**: Feature behavior doesn't change when flag is toggled

**Solutions**:
1. Verify configuration is loaded:
   ```csharp
   var value = builder.Configuration.GetValue<bool>("FeatureManagement:YourFeatureName");
   Log.Information("YourFeatureName flag value: {Value}", value);
   ```

2. Check if application was restarted after configuration change

3. Verify environment variable isn't overriding:
   ```bash
   echo $FeatureManagement__YourFeatureName
   ```

### Compilation Errors After Adding Flag

**Symptom**: Build fails after adding feature flag

**Solutions**:
1. Verify constant name matches exactly in all places
2. Check for typos in configuration keys
3. Ensure all appsettings files are valid JSON

### Tests Failing After Adding Flag

**Symptom**: Integration tests fail after adding feature flag

**Solutions**:
1. Verify Testing environment configuration is correct
2. Check if test assumes feature is always enabled/disabled
3. Update test factories to use correct configuration

## Best Practices

1. **Always Test Both States**: Every feature flag should be tested in both enabled and disabled states
2. **Log Flag State**: Always log whether a feature is enabled/disabled at startup
3. **Minimize Toggle Count**: "Feature toggles are inventory with carrying cost" - minimize active toggles
4. **Short-Lived Release Toggles**: Remove release toggles within 1-2 weeks of stability
5. **Review Long-Lived Toggles**: Quarterly review for Ops toggles, annual for Permission toggles
6. **Document Expiration**: Always set and document expiration dates for Release toggles
7. **Prefer Compile-Time**: Use feature flags for service registration (compile-time) over runtime checks when possible
8. **Avoid Deep Nesting**: Don't nest feature flags inside other feature flags
9. **Consistent Naming**: Use PascalCase for flag names, matching constant names exactly

## References

- **Feature Flag Guide**: `feature-flag-guide.md`
- **Martin Fowler - Feature Toggles**: https://martinfowler.com/articles/feature-toggles.html
- **Microsoft.FeatureManagement Docs**: https://github.com/microsoft/FeatureManagement-Dotnet
