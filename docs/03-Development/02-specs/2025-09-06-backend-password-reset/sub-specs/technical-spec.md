# Technical Specification

This is the technical specification for the spec detailed in [spec.md](../spec.md)

## Technical Requirements

### Domain Layer Changes

- **PasswordResetToken Entity**: New entity to store reset tokens with properties:
  - Id (Guid)
  - UserId (Guid) - Foreign key to User
  - Token (string) - Cryptographically secure token
  - ExpiresAt (DateTime)
  - IsUsed (bool)
  - CreatedAt (DateTime)
  - UsedAt (DateTime?)

- **Token Generation Logic**: Domain service for secure token generation using cryptographic random number generator

### Application Layer Changes

- **ForgotPasswordCommand**: CQRS command to initiate password reset
  - Input: Email address
  - Process: Generate token, store in database, trigger email
  - Output: Success/failure result

- **ResetPasswordCommand**: CQRS command to complete password reset
  - Input: Token, new password
  - Process: Validate token, update password, mark token as used
  - Output: Success/failure result

- **ValidateResetTokenQuery**: CQRS query to check token validity
  - Input: Token
  - Process: Check token exists, not expired, not used
  - Output: Valid/invalid status

- **IEmailService Interface**: Abstraction for email sending functionality

### Infrastructure Layer Changes

- **PasswordResetTokenRepository**: Repository implementation for token persistence
- **EmailService Implementation**: Concrete email service using SMTP or SendGrid
- **Database Migrations**: Add PasswordResetTokens table
- **Rate Limiting Service**: Track and limit password reset requests per email

### Presentation Layer Changes

- **AuthController Extensions**: Add three new endpoints
  - POST /api/auth/forgot-password
  - POST /api/auth/reset-password
  - POST /api/auth/validate-reset-token

### Integration Requirements

- **Existing User Entity**: Integrate with current User domain entity
- **PasswordHash Value Object**: Reuse existing password hashing logic
- **JWT Authentication**: Work alongside existing JWT token system
- **Angular Frontend**: Match expected API contracts from frontend implementation

### Performance Criteria

- Token generation < 100ms
- Email sending asynchronous (don't block API response)
- Database queries use appropriate indexes
- Rate limiting check < 50ms

## External Dependencies

- **MailKit** (v4.3.0) - Modern SMTP client library for .NET
  - **Justification:** Industry standard for email in .NET, supports modern protocols and security
  
- **SendGrid** (v9.28.1) - Optional cloud email service SDK
  - **Justification:** Reliable email delivery service with good .NET support, optional alternative to SMTP
  
- **Microsoft.Extensions.Caching.Memory** - Rate limiting storage
  - **Justification:** Already included in ASP.NET Core, efficient for rate limiting tracking