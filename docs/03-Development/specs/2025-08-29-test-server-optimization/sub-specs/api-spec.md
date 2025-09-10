# API Specification

This is the API specification for the spec detailed in [spec.md](../spec.md)

## Endpoints

### GET /health

**Purpose:** Health check endpoint for server availability detection
**Parameters:** None
**Response:** 200 OK with "Healthy" body
**Errors:** None expected

### POST /api/database/reset (Conditional - Testing Environment Only)

**Purpose:** Reset test database to clean state
**Parameters:** 
- Body: DatabaseResetRequest
  - workerIndex: number (optional, default 0)
  - preserveSchema: boolean (optional, default true)
  - resetToken: string (required security token)

**Response:** 
- 200 OK: { message: "Database reset successfully", timestamp: "ISO-8601" }
- 404 Not Found: Endpoint hidden in non-testing environments

**Errors:**
- 404: Environment is not Testing
- 403: Invalid or missing reset token
- 403: Request not from localhost
- 500: Database reset failed

## Security Requirements

### Environment Restrictions
- Endpoint only exists when ASPNETCORE_ENVIRONMENT = "Testing"
- Returns 404 in Development or Production environments

### Access Controls
- Localhost-only access (check RemoteIpAddress = IPAddress.Loopback)
- Required security header: X-Test-Reset-Token
- Token value configured in appsettings.Testing.json

### Implementation Safety
```csharp
[HttpPost("reset")]
public async Task<IActionResult> ResetDatabase([FromBody] DatabaseResetRequest request)
{
    // Multiple security gates
    if (_environment.EnvironmentName != "Testing") 
        return NotFound();
    
    if (!HttpContext.Connection.RemoteIpAddress.Equals(IPAddress.Loopback))
        return NotFound();
    
    if (Request.Headers["X-Test-Reset-Token"] != _config["TestResetToken"])
        return NotFound();
    
    // Safe to proceed with reset
}
```