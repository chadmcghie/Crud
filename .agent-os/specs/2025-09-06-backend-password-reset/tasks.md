# Spec Tasks

These are the tasks to be completed for the spec detailed in @.agent-os/specs/2025-09-06-backend-password-reset/spec.md

> Created: 2025-09-07
> Status: Ready for Implementation

## Tasks

### 1. Password Reset Token Entity and Repository (Domain Layer)
**Issue:** #127
**Estimated:** 4 hours
**Priority:** High

- [ ] 1.1 Write unit tests for PasswordResetToken entity
  - [ ] Test token generation with proper randomness
  - [ ] Test expiration logic (default 24 hours)
  - [ ] Test IsExpired property functionality
  - [ ] Test token validation rules
- [ ] 1.2 Implement PasswordResetToken domain entity
  - [ ] Create entity with UserId, Token, ExpiresAt, CreatedAt properties
  - [ ] Implement token generation using cryptographically secure random
  - [ ] Add expiration validation logic
  - [ ] Ensure entity follows domain layer patterns
- [ ] 1.3 Create IPasswordResetTokenRepository interface in App layer
  - [ ] Define CreateAsync method
  - [ ] Define GetByTokenAsync method
  - [ ] Define DeleteAsync method
  - [ ] Define DeleteExpiredTokensAsync method
- [ ] 1.4 Implement PasswordResetTokenRepository in Infrastructure layer
  - [ ] Create EF Core entity configuration
  - [ ] Implement repository methods with proper error handling
  - [ ] Add database indexes for performance
  - [ ] Configure entity in DbContext
- [ ] 1.5 Create and run database migration
  - [ ] Generate EF Core migration for PasswordResetToken table
  - [ ] Review migration script for correctness
  - [ ] Update database schema
- [ ] 1.6 Verify all tests pass and code quality
  - [ ] Run unit tests and ensure 100% pass rate
  - [ ] Run dotnet format for code formatting
  - [ ] Verify Clean Architecture boundaries are maintained

### 2. Email Service Infrastructure
**Issue:** #128
**Estimated:** 3 hours
**Priority:** High

- [ ] 2.1 Write unit tests for email service interface
  - [ ] Test SendPasswordResetEmailAsync method signature
  - [ ] Test email content generation
  - [ ] Test error handling scenarios
  - [ ] Mock email service behavior for testing
- [ ] 2.2 Create IEmailService interface in App layer
  - [ ] Define SendPasswordResetEmailAsync method
  - [ ] Include proper method signatures with email, token parameters
  - [ ] Add cancellation token support
  - [ ] Define email template structure
- [ ] 2.3 Implement MockEmailService for development
  - [ ] Create implementation that logs email details
  - [ ] Store sent emails in memory for testing verification
  - [ ] Implement realistic delays to simulate email sending
  - [ ] Add configuration for mock behavior
- [ ] 2.4 Configure service registration in dependency injection
  - [ ] Register IEmailService in Program.cs
  - [ ] Configure different implementations for different environments
  - [ ] Set up service lifetime appropriately
  - [ ] Add configuration section for email settings
- [ ] 2.5 Verify all tests pass and integration works
  - [ ] Run unit tests for email service
  - [ ] Test service resolution through DI container
  - [ ] Verify mock service functionality
  - [ ] Run dotnet format for code formatting

### 3. Forgot Password Endpoint and Business Logic
**Issue:** #129
**Estimated:** 5 hours
**Priority:** High

- [ ] 3.1 Write unit tests for forgot password handler
  - [ ] Test valid email scenarios
  - [ ] Test non-existent user scenarios
  - [ ] Test rate limiting behavior
  - [ ] Test email service integration
  - [ ] Test token generation and storage
- [ ] 3.2 Implement ForgotPasswordCommand and Handler using MediatR
  - [ ] Create command with email validation
  - [ ] Implement handler with business logic
  - [ ] Add user lookup functionality
  - [ ] Integrate password reset token creation
  - [ ] Add proper error handling and logging
- [ ] 3.3 Create API endpoint with rate limiting
  - [ ] Create ForgotPasswordController endpoint
  - [ ] Implement rate limiting (max 3 requests per 15 minutes per IP)
  - [ ] Add request validation and model binding
  - [ ] Configure proper HTTP status codes
  - [ ] Add API documentation attributes
- [ ] 3.4 Integrate with email service
  - [ ] Connect handler to IEmailService
  - [ ] Generate password reset URLs with tokens
  - [ ] Handle email sending failures gracefully
  - [ ] Add retry logic for transient failures
- [ ] 3.5 Add comprehensive logging and monitoring
  - [ ] Log forgot password attempts
  - [ ] Log email sending success/failures
  - [ ] Add metrics for monitoring
  - [ ] Implement security event logging
- [ ] 3.6 Verify all tests pass and endpoint functionality
  - [ ] Run unit tests with 100% coverage
  - [ ] Test API endpoint manually
  - [ ] Verify rate limiting works correctly
  - [ ] Run integration tests

### 4. Reset Password and Token Validation Endpoints
**Issue:** #130
**Estimated:** 6 hours
**Priority:** High

- [ ] 4.1 Write unit tests for reset password handler
  - [ ] Test valid token scenarios
  - [ ] Test expired token scenarios
  - [ ] Test invalid token scenarios
  - [ ] Test password validation rules
  - [ ] Test successful password update
- [ ] 4.2 Write unit tests for token validation handler
  - [ ] Test valid token validation
  - [ ] Test expired token validation
  - [ ] Test non-existent token validation
  - [ ] Test malformed token scenarios
- [ ] 4.3 Implement ResetPasswordCommand and Handler
  - [ ] Create command with token and new password
  - [ ] Add password strength validation
  - [ ] Implement token verification logic
  - [ ] Add password hashing using existing UserService
  - [ ] Clean up used tokens after successful reset
- [ ] 4.4 Implement ValidateResetTokenQuery and Handler
  - [ ] Create query to check token validity
  - [ ] Return token status and expiration info
  - [ ] Handle all edge cases gracefully
  - [ ] Add proper error responses
- [ ] 4.5 Create API endpoints with security measures
  - [ ] Create ResetPasswordController endpoints
  - [ ] Implement HTTPS-only requirements
  - [ ] Add CSRF protection measures
  - [ ] Configure proper CORS settings
  - [ ] Add rate limiting (max 5 attempts per hour per IP)
- [ ] 4.6 Add security hardening
  - [ ] Implement constant-time token comparison
  - [ ] Add protection against timing attacks
  - [ ] Log all reset attempts for security monitoring
  - [ ] Implement account lockout on multiple failures
- [ ] 4.7 Verify all tests pass and security measures work
  - [ ] Run comprehensive unit test suite
  - [ ] Test all security scenarios
  - [ ] Verify rate limiting effectiveness
  - [ ] Run integration tests

### 5. Integration and E2E Testing
**Issue:** #131
**Estimated:** 4 hours
**Priority:** Medium

- [ ] 5.1 Write integration tests for all endpoints
  - [ ] Test forgot password endpoint with real database
  - [ ] Test reset password endpoint integration
  - [ ] Test token validation endpoint
  - [ ] Test database cleanup of expired tokens
  - [ ] Test email service integration
- [ ] 5.2 Create E2E test scenarios using Playwright
  - [ ] Test complete password reset workflow
  - [ ] Test expired token handling
  - [ ] Test invalid token scenarios
  - [ ] Test rate limiting behavior
  - [ ] Test error message display
- [ ] 5.3 Test email workflow end-to-end
  - [ ] Verify email content generation
  - [ ] Test reset URL construction
  - [ ] Validate email template rendering
  - [ ] Test mock email service in development
- [ ] 5.4 Performance and security testing
  - [ ] Load test password reset endpoints
  - [ ] Test concurrent token generation
  - [ ] Verify database performance with indexes
  - [ ] Test security headers and HTTPS enforcement
- [ ] 5.5 Verify all tests pass and system integration
  - [ ] Run complete test suite (unit, integration, E2E)
  - [ ] Verify all tests tagged appropriately (@smoke, @critical)
  - [ ] Test in both development and production-like environments
  - [ ] Document any remaining known issues or limitations

## Additional Considerations

- **Security Review**: All password reset functionality should undergo security review before deployment
- **Documentation**: Update API documentation with new endpoints and security considerations
- **Monitoring**: Set up alerts for suspicious password reset activity patterns
- **Cleanup Job**: Consider implementing background job to clean up expired tokens
- **Email Templates**: Design user-friendly email templates with proper branding
- **Localization**: Consider multi-language support for email templates if required