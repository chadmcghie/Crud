# Technical Specification

This is the technical specification for the spec detailed in @.agent-os/specs/2025-08-29-jwt-authentication/spec.md

## Technical Requirements

### Domain Layer Changes
- **User Entity**: Create User aggregate root with properties: Id (Guid), Email (value object), PasswordHash (value object), Roles (collection), CreatedAt, UpdatedAt, RefreshTokens (collection)
- **RefreshToken Entity**: Track refresh tokens with properties: Token, UserId, ExpiresAt, CreatedAt, RevokedAt
- **Role Value Object**: Enumeration for Admin and User roles
- **Domain Events**: UserRegisteredEvent, UserLoggedInEvent, TokenRefreshedEvent
- **Domain Services**: IPasswordHasher interface for password hashing abstraction

### Application Layer Changes
- **Commands**: RegisterUserCommand, LoginCommand, RefreshTokenCommand, RevokeTokenCommand
- **Queries**: GetUserByEmailQuery, ValidateTokenQuery
- **DTOs**: UserDto, LoginRequestDto, LoginResponseDto, RegisterRequestDto, TokenResponseDto
- **Interfaces**: IJwtTokenService, ICurrentUserService, IAuthenticationService
- **Validators**: RegisterUserCommandValidator, LoginCommandValidator with FluentValidation rules

### Infrastructure Layer Changes
- **JWT Service Implementation**: JwtTokenService using System.IdentityModel.Tokens.Jwt for token generation and validation
- **Password Hasher**: BCryptPasswordHasher using BCrypt.Net-Next library
- **Authentication Middleware**: Custom JWT authentication middleware for token validation
- **Authorization Policies**: Configure role-based policies for Admin and User roles
- **Cookie Configuration**: HTTP-only cookie settings for refresh token storage
- **User Repository**: Implementation of IUserRepository with EF Core

### Presentation Layer Changes
- **Authentication Controller**: Endpoints for Register, Login, RefreshToken, Logout
- **Authorization Attributes**: [Authorize] and [Authorize(Roles = "Admin")] for endpoint protection
- **Current User Middleware**: Middleware to extract and set current user context from JWT
- **Error Handling**: Standardized authentication/authorization error responses
- **Swagger Configuration**: JWT bearer token configuration for API documentation

### Integration Requirements
- Configure JWT bearer authentication in Program.cs
- Add authentication and authorization middleware to pipeline
- Configure CORS to handle authentication headers
- Set up dependency injection for authentication services
- Configure token validation parameters (issuer, audience, signing key)

### Performance Criteria
- Token generation: < 100ms
- Token validation: < 50ms per request
- Password hashing: < 500ms (BCrypt with appropriate work factor)
- Database queries for user lookup: < 100ms with proper indexing

## External Dependencies

- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication middleware for ASP.NET Core
- **Justification:** Official Microsoft package for JWT authentication in ASP.NET Core applications

- **System.IdentityModel.Tokens.Jwt** - JWT token generation and validation
- **Justification:** Industry standard library for working with JWT tokens in .NET

- **BCrypt.Net-Next** (v4.0.3) - Password hashing using BCrypt algorithm
- **Justification:** Secure password hashing with configurable work factor, actively maintained fork of BCrypt.Net