# Spec Tasks

These are the tasks to be completed for the spec detailed in @.agent-os/specs/2025-09-06-backend-password-reset/spec.md

> Created: 2025-09-07
> Status: Ready for Implementation

## Tasks

### 1. Password Reset Token Entity and Repository (Domain Layer)
**Issue:** #127
**Estimated:** 4 hours
**Priority:** High

- [x] 1.1 Write unit tests for PasswordResetToken entity
  - [x] Test token generation with proper randomness
  - [x] Test expiration logic (default 24 hours)
  - [x] Test IsExpired property functionality
  - [x] Test token validation rules
- [x] 1.2 Implement PasswordResetToken domain entity
  - [x] Create entity with UserId, Token, ExpiresAt, CreatedAt properties
  - [x] Implement token generation using cryptographically secure random
  - [x] Add expiration validation logic
  - [x] Ensure entity follows domain layer patterns
- [x] 1.3 Create IPasswordResetTokenRepository interface in App layer
  - [x] Define CreateAsync method
  - [x] Define GetByTokenAsync method
  - [x] Define DeleteAsync method
  - [x] Define DeleteExpiredTokensAsync method
- [x] 1.4 Implement PasswordResetTokenRepository in Infrastructure layer
  - [x] Create EF Core entity configuration
  - [x] Implement repository methods with proper error handling
  - [x] Add database indexes for performance
  - [x] Configure entity in DbContext
- [x] 1.5 Create and run database migration
  - [x] Generate EF Core migration for PasswordResetToken table
  - [x] Review migration script for correctness
  - [x] Update database schema
- [x] 1.6 Verify all tests pass and code quality
  - [x] Run unit tests and ensure 100% pass rate
  - [x] Run dotnet format for code formatting
  - [x] Verify Clean Architecture boundaries are maintained

### 2. Email Service Infrastructure
**Issue:** #128
**Estimated:** 3 hours
**Priority:** High

- [x] 2.1 Write unit tests for email service interface
  - [x] Test SendPasswordResetEmailAsync method signature
  - [x] Test email content generation
  - [x] Test error handling scenarios
  - [x] Mock email service behavior for testing
- [x] 2.2 Create IEmailService interface in App layer
  - [x] Define SendPasswordResetEmailAsync method
  - [x] Include proper method signatures with email, token parameters
  - [x] Add cancellation token support
  - [x] Define email template structure
- [x] 2.3 Implement MockEmailService for development
  - [x] Create implementation that logs email details
  - [x] Store sent emails in memory for testing verification
  - [x] Implement realistic delays to simulate email sending
  - [x] Add configuration for mock behavior
- [x] 2.4 Configure service registration in dependency injection
  - [x] Register IEmailService in Program.cs
  - [x] Configure different implementations for different environments
  - [x] Set up service lifetime appropriately
  - [x] Add configuration section for email settings
- [x] 2.5 Verify all tests pass and integration works
  - [x] Run unit tests for email service
  - [x] Test service resolution through DI container
  - [x] Verify mock service functionality
  - [x] Run dotnet format for code formatting

### 3. Forgot Password Endpoint and Business Logic
**Issue:** #129
**Estimated:** 5 hours
**Priority:** High

- [x] 3.1 Write unit tests for forgot password handler
  - [x] Test valid email scenarios
  - [x] Test non-existent user scenarios
  - [x] Test rate limiting behavior
  - [x] Test email service integration
  - [x] Test token generation and storage
- [x] 3.2 Implement ForgotPasswordCommand and Handler using MediatR
  - [x] Create command with email validation
  - [x] Implement handler with business logic
  - [x] Add user lookup functionality
  - [x] Integrate password reset token creation
  - [x] Add proper error handling and logging
- [x] 3.3 Create API endpoint with rate limiting
  - [x] Create ForgotPasswordController endpoint
  - [x] Implement rate limiting (max 3 requests per 15 minutes per IP)
  - [x] Add request validation and model binding
  - [x] Configure proper HTTP status codes
  - [x] Add API documentation attributes
- [x] 3.4 Integrate with email service
  - [x] Connect handler to IEmailService
  - [x] Generate password reset URLs with tokens
  - [x] Handle email sending failures gracefully
  - [x] Add retry logic for transient failures
- [x] 3.5 Add comprehensive logging and monitoring
  - [x] Log forgot password attempts
  - [x] Log email sending success/failures
  - [x] Add metrics for monitoring
  - [x] Implement security event logging
- [x] 3.6 Verify all tests pass and endpoint functionality
  - [x] Run unit tests with 100% coverage
  - [x] Test API endpoint manually
  - [x] Verify rate limiting works correctly
  - [x] Run integration tests

### 4. Reset Password and Token Validation Endpoints
**Issue:** #130
**Estimated:** 6 hours
**Priority:** High

- [x] 4.1 Write unit tests for reset password handler
  - [x] Test valid token scenarios
  - [x] Test expired token scenarios
  - [x] Test invalid token scenarios
  - [x] Test password validation rules
  - [x] Test successful password update
- [x] 4.2 Write unit tests for token validation handler
  - [x] Test valid token validation
  - [x] Test expired token validation
  - [x] Test non-existent token validation
  - [x] Test malformed token scenarios
- [x] 4.3 Implement ResetPasswordCommand and Handler
  - [x] Create command with token and new password
  - [x] Add password strength validation
  - [x] Implement token verification logic
  - [x] Add password hashing using existing UserService
  - [x] Clean up used tokens after successful reset
- [x] 4.4 Implement ValidateResetTokenQuery and Handler
  - [x] Create query to check token validity
  - [x] Return token status and expiration info
  - [x] Handle all edge cases gracefully
  - [x] Add proper error responses
- [x] 4.5 Create API endpoints with security measures
  - [x] Create ResetPasswordController endpoints
  - [x] Implement HTTPS-only requirements
  - [x] Add CSRF protection measures
  - [x] Configure proper CORS settings
  - [x] Add rate limiting (max 5 attempts per hour per IP)
- [x] 4.6 Add security hardening
  - [x] Implement constant-time token comparison
  - [x] Add protection against timing attacks
  - [x] Log all reset attempts for security monitoring
  - [x] Implement account lockout on multiple failures
- [x] 4.7 Verify all tests pass and security measures work
  - [x] Run comprehensive unit test suite
  - [x] Test all security scenarios
  - [x] Verify rate limiting effectiveness
  - [x] Run integration tests

### 5. Integration and E2E Testing
**Issue:** #131
**Estimated:** 4 hours
**Priority:** Medium

- [x] 5.1 Write integration tests for all endpoints
  - [x] Test forgot password endpoint with real database
  - [x] Test reset password endpoint integration
  - [x] Test token validation endpoint
  - [x] Test database cleanup of expired tokens
  - [x] Test email service integration
- [x] 5.2 Create E2E test scenarios using Playwright
  - [x] Test complete password reset workflow
  - [x] Test expired token handling
  - [x] Test invalid token scenarios
  - [x] Test rate limiting behavior
  - [x] Test error message display
- [x] 5.3 Test email workflow end-to-end
  - [x] Verify email content generation
  - [x] Test reset URL construction
  - [x] Validate email template rendering
  - [x] Test mock email service in development
- [x] 5.4 Performance and security testing
  - [x] Load test password reset endpoints
  - [x] Test concurrent token generation
  - [x] Verify database performance with indexes
  - [x] Test security headers and HTTPS enforcement
- [x] 5.5 Verify all tests pass and system integration
  - [x] Run complete test suite (unit, integration, E2E)
  - [x] Verify all tests tagged appropriately (@smoke, @critical)
  - [x] Test in both development and production-like environments
  - [x] Document any remaining known issues or limitations

## Additional Considerations

- **Security Review**: All password reset functionality should undergo security review before deployment
- **Documentation**: Update API documentation with new endpoints and security considerations
- **Monitoring**: Set up alerts for suspicious password reset activity patterns
- **Cleanup Job**: Consider implementing background job to clean up expired tokens
- **Email Templates**: Design user-friendly email templates with proper branding
- **Localization**: Consider multi-language support for email templates if required