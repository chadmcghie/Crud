# Technical Specification

> Sub-Spec: Controller Authorization Protection - Technical Implementation
> Created: 2025-01-15
> Parent Spec: Controller Authorization Protection

## Technical Overview

This specification details the technical implementation of authorization protection for business API controllers. The implementation involves adding `[Authorize]` attributes and role-based policies to secure endpoints that are currently publicly accessible.

## Current State Analysis

### Authentication Infrastructure (✅ Complete)
- JWT token service implemented and working
- AuthController properly secured with `[Authorize]` attributes
- Authorization policies defined: `AdminOnly`, `UserOrAdmin`
- Middleware pipeline configured with authentication and authorization

### Unprotected Controllers (❌ Need Protection)
- **PeopleController** - No authorization attributes
- **RolesController** - No authorization attributes  
- **WallsController** - No authorization attributes
- **WindowsController** - No authorization attributes

## Technical Implementation

### 1. Controller Authorization Attributes

#### PeopleController
```csharp
[ApiController]
[Tags("People")]
[Route("api/[controller]")]
[Authorize] // Add class-level authorization
public class PeopleController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<IEnumerable<PersonResponse>>> List(CancellationToken ct)
    
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<PersonResponse>> Get(Guid id, CancellationToken ct)
    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<PersonResponse>> Create([FromBody] CreatePersonRequest request, CancellationToken ct)
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<PersonResponse>> Update(Guid id, [FromBody] UpdatePersonRequest request, CancellationToken ct)
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
}
```

#### RolesController
```csharp
[ApiController]
[Tags("Roles")]
[Route("api/[controller]")]
[Authorize] // Add class-level authorization
public class RolesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<IEnumerable<RoleResponse>>> List(CancellationToken ct)
    
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<RoleResponse>> Get(Guid id, CancellationToken ct)
    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<RoleResponse>> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
}
```

#### WallsController
```csharp
[ApiController]
[Tags("Walls")]
[Route("api/[controller]")]
[Authorize] // Add class-level authorization
public class WallsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<IEnumerable<WallResponse>>> List(CancellationToken ct)
    
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<WallResponse>> Get(Guid id, CancellationToken ct)
    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<WallResponse>> Create([FromBody] CreateWallRequest request, CancellationToken ct)
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<WallResponse>> Update(Guid id, [FromBody] UpdateWallRequest request, CancellationToken ct)
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
}
```

#### WindowsController
```csharp
[ApiController]
[Tags("Windows")]
[Route("api/[controller]")]
[Authorize] // Add class-level authorization
public class WindowsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<IEnumerable<WindowResponse>>> List(CancellationToken ct)
    
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "UserOrAdmin")] // Read access for all authenticated users
    public async Task<ActionResult<WindowResponse>> Get(Guid id, CancellationToken ct)
    
    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<WindowResponse>> Create([FromBody] CreateWindowRequest request, CancellationToken ct)
    
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<ActionResult<WindowResponse>> Update(Guid id, [FromBody] UpdateWindowRequest request, CancellationToken ct)
    
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")] // Write access for admins only
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
}
```

### 2. Authorization Policy Strategy

#### Read Operations (GET)
- **Policy**: `UserOrAdmin`
- **Access**: All authenticated users (both User and Admin roles)
- **Rationale**: Business data should be readable by all authenticated users

#### Write Operations (POST, PUT, DELETE)
- **Policy**: `AdminOnly`
- **Access**: Admin users only
- **Rationale**: Data modification should be restricted to administrators

### 3. Testing Strategy

#### Integration Test Updates
- Add authentication to existing integration tests
- Test unauthorized access scenarios (401/403 responses)
- Test authorized access for both User and Admin roles
- Verify role-based access control works correctly

#### Test Authentication Setup
```csharp
// Example test setup with authentication
public class PeopleControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PeopleControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPeople_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/people");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPeople_WithUserAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync("user@example.com", "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/people");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePerson_WithUserAuth_ReturnsForbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync("user@example.com", "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/api/people", new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePerson_WithAdminAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync("admin@example.com", "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsync("/api/people", new StringContent("{}", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Implementation Steps

1. **Add Authorization Attributes** - Add `[Authorize]` attributes to all business controllers
2. **Update Integration Tests** - Modify existing tests to include authentication
3. **Test Authorization** - Verify unauthorized access returns 401/403
4. **Test Role-Based Access** - Verify User vs Admin access works correctly
5. **Update Documentation** - Update API documentation to reflect authentication requirements

## Risk Mitigation

### Potential Issues
- **Breaking Changes**: Existing tests may fail without authentication
- **Frontend Integration**: Angular app may need updates for protected endpoints
- **API Documentation**: Swagger may need updates for authentication

### Mitigation Strategies
- **Gradual Implementation**: Add authorization one controller at a time
- **Comprehensive Testing**: Test all endpoints with different user roles
- **Documentation Updates**: Update API documentation and Swagger configuration
- **Frontend Coordination**: Ensure Angular app handles authentication properly

## Success Metrics

- [ ] All business controllers have `[Authorize]` attributes
- [ ] Unauthorized access returns 401 (Unauthorized)
- [ ] Insufficient permissions return 403 (Forbidden)
- [ ] Authorized access works for appropriate roles
- [ ] All integration tests pass
- [ ] API documentation reflects authentication requirements
- [ ] No breaking changes to existing functionality
