# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-09-06-backend-password-reset/spec.md

## Endpoints

### POST /api/auth/forgot-password

**Purpose:** Initiate password reset process by generating token and sending email

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "If the email exists, a password reset link has been sent."
}
```

**Response (429 Too Many Requests):**
```json
{
  "error": "Too many requests. Please try again later."
}
```

**Headers:**
- `Retry-After`: Number of seconds before next request allowed (on 429 response)

**Business Logic:**
- Always return 200 OK to prevent email enumeration
- Generate cryptographically secure token
- Store token with 1-hour expiration
- Send email asynchronously
- Implement rate limiting (3 requests per hour per email)

### POST /api/auth/reset-password

**Purpose:** Complete password reset using valid token

**Request Body:**
```json
{
  "token": "cryptographically-secure-token-string",
  "newPassword": "NewSecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Password has been reset successfully."
}
```

**Response (400 Bad Request):**
```json
{
  "error": "Invalid or expired token."
}
```

**Response (400 Bad Request - Validation):**
```json
{
  "error": "Password does not meet complexity requirements."
}
```

**Business Logic:**
- Validate token exists and not expired
- Verify token hasn't been used
- Update user password using existing PasswordHash logic
- Mark token as used
- Invalidate all user refresh tokens for security

### POST /api/auth/validate-reset-token

**Purpose:** Check if reset token is valid without consuming it

**Request Body:**
```json
{
  "token": "cryptographically-secure-token-string"
}
```

**Response (200 OK):**
```json
{
  "valid": true
}
```

**Response (200 OK - Invalid):**
```json
{
  "valid": false,
  "reason": "expired" // or "invalid" or "used"
}
```

**Business Logic:**
- Check token exists in database
- Verify not expired
- Verify not already used
- Don't consume the token
- Used by frontend to show appropriate UI

## Controller Implementation

### AuthController Extensions

```csharp
[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordCommand command,
    CancellationToken cancellationToken)
{
    // Rate limiting check
    if (!await _rateLimiter.AllowRequest(command.Email))
    {
        Response.Headers.Add("Retry-After", "3600");
        return StatusCode(429, new { error = "Too many requests. Please try again later." });
    }
    
    var result = await _mediator.Send(command, cancellationToken);
    
    // Always return success to prevent email enumeration
    return Ok(new { 
        success = true, 
        message = "If the email exists, a password reset link has been sent." 
    });
}

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword(
    [FromBody] ResetPasswordCommand command,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(command, cancellationToken);
    
    if (result.Success)
    {
        return Ok(new { 
            success = true, 
            message = "Password has been reset successfully." 
        });
    }
    
    return BadRequest(new { error = result.Error });
}

[HttpPost("validate-reset-token")]
[AllowAnonymous]
public async Task<IActionResult> ValidateResetToken(
    [FromBody] ValidateResetTokenQuery query,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(query, cancellationToken);
    
    return Ok(new { 
        valid = result.IsValid,
        reason = result.InvalidReason 
    });
}
```

## DTOs

### ForgotPasswordCommand
```csharp
public class ForgotPasswordCommand : IRequest<Result>
{
    public string Email { get; set; }
}
```

### ResetPasswordCommand
```csharp
public class ResetPasswordCommand : IRequest<Result>
{
    public string Token { get; set; }
    public string NewPassword { get; set; }
}
```

### ValidateResetTokenQuery
```csharp
public class ValidateResetTokenQuery : IRequest<TokenValidationResult>
{
    public string Token { get; set; }
}
```

## Security Considerations

1. **Prevent Email Enumeration**: Always return success for forgot-password
2. **Rate Limiting**: Maximum 3 requests per hour per email
3. **Token Security**: Use cryptographically secure random generation
4. **Constant Time Comparison**: Prevent timing attacks on token validation
5. **HTTPS Only**: All endpoints require HTTPS in production
6. **Token Expiration**: Strict 1-hour expiration, no extensions
7. **Single Use**: Tokens can only be used once
8. **Audit Logging**: Log all password reset attempts for security monitoring