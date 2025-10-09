# API Specification

This is the API specification for the spec detailed in @docs/03-Development/02-specs/2025-10-08-feature-flags/spec.md

## Overview

Feature flags primarily affect **backend behavior and service registration**, not API endpoints. However, a **diagnostic endpoint** is useful for operations teams to verify feature flag states in different environments.

## Endpoints

### GET /api/diagnostics/feature-flags

**Purpose**: Retrieve current feature flag states for operational visibility and troubleshooting

**Authentication**: Required (Admin role only)

**Authorization**: Requires `Admin` or `Operations` role

**Parameters**: None

**Response Format**:
```json
{
  "environment": "Development",
  "flags": {
    "ops": {
      "caching": true,
      "compression": true,
      "rateLimiting": true,
      "openTelemetry": true,
      "cors": true,
      "detailedHealthChecks": false,
      "dynamicLogLevel": true
    },
    "release": {
      "aspNetCoreIdentity": false,
      "realEmailService": false,
      "resilience": true
    },
    "permission": {
      "swagger": true,
      "databaseSeeding": true,
      "detailedExceptions": true,
      "httpConditionalRequests": true
    }
  },
  "timestamp": "2025-10-08T14:30:00Z"
}
```

**Status Codes**:
- `200 OK` - Feature flags retrieved successfully
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions (not Admin/Operations role)

**Errors**:
```json
{
  "error": "Unauthorized",
  "message": "Admin or Operations role required to view feature flags"
}
```

**Security Considerations**:
- **Production**: This endpoint should be restricted to Admin/Operations roles only
- **Alternative**: Consider disabling this endpoint in production via feature flag itself
- **Rate Limiting**: Apply rate limiting to prevent abuse
- **Audit Logging**: Log all access attempts to this endpoint

### Controller Implementation

**Controller**: `DiagnosticsController` (new)

**MediatR Handler**: `GetFeatureFlagsQuery` and `GetFeatureFlagsQueryHandler`

**Dependencies**:
- `IFeatureManager` - Microsoft.FeatureManagement service
- `IFeatureFlagService` - Custom service wrapping IFeatureManager

**Example Implementation**:
```csharp
[ApiController]
[Route("api/diagnostics")]
[Authorize(Roles = "Admin,Operations")]
public class DiagnosticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiagnosticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("feature-flags")]
    public async Task<ActionResult<FeatureFlagsDto>> GetFeatureFlags()
    {
        var query = new GetFeatureFlagsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

**MediatR Query**:
```csharp
public record GetFeatureFlagsQuery : IRequest<FeatureFlagsDto>;

public class GetFeatureFlagsQueryHandler : IRequestHandler<GetFeatureFlagsQuery, FeatureFlagsDto>
{
    private readonly IFeatureManager _featureManager;
    private readonly IConfiguration _configuration;

    public GetFeatureFlagsQueryHandler(IFeatureManager featureManager, IConfiguration configuration)
    {
        _featureManager = featureManager;
        _configuration = configuration;
    }

    public async Task<FeatureFlagsDto> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var flags = new FeatureFlagsDto
        {
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
            Flags = new FeatureFlagCollectionDto
            {
                Ops = new OpsFlagsDto
                {
                    Caching = await _featureManager.IsEnabledAsync("Caching"),
                    Compression = await _featureManager.IsEnabledAsync("Compression"),
                    RateLimiting = await _featureManager.IsEnabledAsync("RateLimiting"),
                    OpenTelemetry = await _featureManager.IsEnabledAsync("OpenTelemetry"),
                    Cors = await _featureManager.IsEnabledAsync("CORS"),
                    DetailedHealthChecks = await _featureManager.IsEnabledAsync("DetailedHealthChecks"),
                    DynamicLogLevel = await _featureManager.IsEnabledAsync("DynamicLogLevel")
                },
                Release = new ReleaseFlagsDto
                {
                    AspNetCoreIdentity = await _featureManager.IsEnabledAsync("AspNetCoreIdentity"),
                    RealEmailService = await _featureManager.IsEnabledAsync("RealEmailService"),
                    Resilience = await _featureManager.IsEnabledAsync("Resilience")
                },
                Permission = new PermissionFlagsDto
                {
                    Swagger = await _featureManager.IsEnabledAsync("Swagger"),
                    DatabaseSeeding = await _featureManager.IsEnabledAsync("DatabaseSeeding"),
                    DetailedExceptions = await _featureManager.IsEnabledAsync("DetailedExceptions"),
                    HttpConditionalRequests = await _featureManager.IsEnabledAsync("HttpConditionalRequests")
                }
            },
            Timestamp = DateTime.UtcNow
        };

        return flags;
    }
}
```

## DTOs

### FeatureFlagsDto
```csharp
public class FeatureFlagsDto
{
    public string Environment { get; set; } = string.Empty;
    public FeatureFlagCollectionDto Flags { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
```

### FeatureFlagCollectionDto
```csharp
public class FeatureFlagCollectionDto
{
    public OpsFlagsDto Ops { get; set; } = new();
    public ReleaseFlagsDto Release { get; set; } = new();
    public PermissionFlagsDto Permission { get; set; } = new();
}
```

### OpsFlagsDto
```csharp
public class OpsFlagsDto
{
    public bool Caching { get; set; }
    public bool Compression { get; set; }
    public bool RateLimiting { get; set; }
    public bool OpenTelemetry { get; set; }
    public bool Cors { get; set; }
    public bool DetailedHealthChecks { get; set; }
    public bool DynamicLogLevel { get; set; }
}
```

### ReleaseFlagsDto
```csharp
public class ReleaseFlagsDto
{
    public bool AspNetCoreIdentity { get; set; }
    public bool RealEmailService { get; set; }
    public bool Resilience { get; set; }
}
```

### PermissionFlagsDto
```csharp
public class PermissionFlagsDto
{
    public bool Swagger { get; set; }
    public bool DatabaseSeeding { get; set; }
    public bool DetailedExceptions { get; set; }
    public bool HttpConditionalRequests { get; set; }
}
```

## Integration with Existing Features

Feature flags will **modify the behavior** of existing endpoints, not create new ones (except the diagnostic endpoint above):

### Health Checks
- **Endpoint**: `GET /health/detailed` (existing)
- **Feature Flag**: `DetailedHealthChecks`
- **Behavior**: Endpoint is conditionally registered in `Program.cs` based on flag state
- **Implementation**: Use `IFeatureManager` in `Program.cs` to conditionally map endpoint

### Swagger UI
- **Endpoint**: `GET /swagger` (existing)
- **Feature Flag**: `Swagger`
- **Behavior**: Swagger UI is conditionally enabled in `Program.cs` based on flag state
- **Implementation**: Use `IFeatureManager` in `Program.cs` to conditionally register Swagger services

### Authentication (Future)
- **Endpoints**: `POST /api/auth/login`, `POST /api/auth/register` (existing)
- **Feature Flag**: `AspNetCoreIdentity`
- **Behavior**: Switch between JWT-only and ASP.NET Core Identity based on flag state
- **Implementation**: Conditional service registration and middleware configuration

## Testing

### Integration Tests

Test the diagnostic endpoint:
```csharp
[Fact]
public async Task GetFeatureFlags_AsAdmin_ReturnsFlags()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

    // Act
    var response = await client.GetAsync("/api/diagnostics/feature-flags");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<FeatureFlagsDto>();
    result.Should().NotBeNull();
    result!.Environment.Should().Be("Testing");
    result.Flags.Ops.Caching.Should().BeTrue();
}

[Fact]
public async Task GetFeatureFlags_AsRegularUser_ReturnsForbidden()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userToken);

    // Act
    var response = await client.GetAsync("/api/diagnostics/feature-flags");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Feature Flag Testing

Test feature-flagged behavior:
```csharp
[Theory]
[InlineData(true)] // Caching enabled
[InlineData(false)] // Caching disabled
public async Task CachingBehavior_RespectsFeatureFlag(bool cachingEnabled)
{
    // Arrange
    var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["FeatureManagement:Caching"] = cachingEnabled.ToString()
                });
            });
        });
    var client = factory.CreateClient();

    // Act & Assert
    // Test caching behavior based on flag state
}
```

## Future Enhancements (Out of Scope for Phase 1)

- **POST /api/diagnostics/feature-flags** - Update feature flags at runtime (requires dynamic configuration provider)
- **GET /api/diagnostics/feature-flags/{flagName}** - Get individual flag state
- **User-level feature targeting** - Enable features for specific users or roles
- **A/B testing endpoints** - Gradual rollout with percentage-based targeting
- **Feature flag analytics** - Track usage and adoption of feature flags
