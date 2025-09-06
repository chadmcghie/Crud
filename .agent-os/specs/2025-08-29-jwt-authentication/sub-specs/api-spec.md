# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-08-29-jwt-authentication/spec.md

## Endpoints

### POST /api/auth/register

**Purpose:** Register a new user account with email and password

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "StrongP@ssw0rd!",
  "confirmPassword": "StrongP@ssw0rd!"
}
```

**Validation Rules:**
- Email: Required, valid email format, max 256 characters
- Password: Required, min 8 characters, must contain uppercase, lowercase, number, and special character
- ConfirmPassword: Must match Password

**Response:** 201 Created
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "roles": ["User"],
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900
}
```

**Response Headers:**
```
Set-Cookie: refreshToken=<token>; HttpOnly; Secure; SameSite=Strict; Max-Age=604800
```

**Errors:**
- 400 Bad Request: Invalid input or password requirements not met
- 409 Conflict: Email already registered

### POST /api/auth/login

**Purpose:** Authenticate user and receive JWT tokens

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "StrongP@ssw0rd!"
}
```

**Response:** 200 OK
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "roles": ["User"],
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900
}
```

**Response Headers:**
```
Set-Cookie: refreshToken=<token>; HttpOnly; Secure; SameSite=Strict; Max-Age=604800
```

**Errors:**
- 400 Bad Request: Missing or invalid credentials
- 401 Unauthorized: Invalid email or password

### POST /api/auth/refresh

**Purpose:** Refresh access token using refresh token from cookie

**Request Headers:**
```
Cookie: refreshToken=<token>
```

**Response:** 200 OK
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900
}
```

**Response Headers:**
```
Set-Cookie: refreshToken=<new-token>; HttpOnly; Secure; SameSite=Strict; Max-Age=604800
```

**Errors:**
- 401 Unauthorized: Missing, invalid, or expired refresh token

### POST /api/auth/logout

**Purpose:** Logout user and revoke refresh token

**Request Headers:**
```
Authorization: Bearer <access-token>
Cookie: refreshToken=<token>
```

**Response:** 204 No Content

**Response Headers:**
```
Set-Cookie: refreshToken=; HttpOnly; Secure; SameSite=Strict; Max-Age=0
```

**Errors:**
- 401 Unauthorized: Missing or invalid access token

### GET /api/auth/me

**Purpose:** Get current authenticated user information

**Request Headers:**
```
Authorization: Bearer <access-token>
```

**Response:** 200 OK
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "roles": ["User"],
  "createdAt": "2025-08-29T10:00:00Z"
}
```

**Errors:**
- 401 Unauthorized: Missing or invalid access token

## Authentication Controller Implementation

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterUserCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);
        SetRefreshTokenCookie(result.RefreshToken);
        return Created($"/api/auth/me", result);
    }
    
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }
    
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(result);
    }
    
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _mediator.Send(new RevokeTokenCommand(refreshToken));
        }
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }
    
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var query = new GetUserByIdQuery(Guid.Parse(userId));
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
```

## Authorization Requirements

### Protected Endpoints
All endpoints except `/api/auth/register`, `/api/auth/login`, and `/api/auth/refresh` require valid JWT bearer token.

### Role-Based Access
```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> AdminOnlyEndpoint() { }

[Authorize(Roles = "User,Admin")]
public async Task<IActionResult> AuthenticatedUserEndpoint() { }
```

### JWT Token Claims
```json
{
  "sub": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "roles": ["User"],
  "exp": 1735470000,
  "iss": "CrudTemplate",
  "aud": "CrudTemplate"
}
```

## Error Response Format
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid or expired token",
  "traceId": "00-abc123-def456-00"
}
```