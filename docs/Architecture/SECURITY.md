# Security Configuration Guide

## Required Environment Variables for Production

### JWT Authentication
- **JWT_SECRET**: JWT signing secret (minimum 32 characters, recommended 64+ characters)
  - Example: `export JWT_SECRET="your-super-secure-jwt-secret-key-here-at-least-32-chars"`
  - Security: This must be a cryptographically secure random string

### Database Testing (Testing/Development environments only)
- **TEST_RESET_TOKEN**: Token required for database reset operations
  - Example: `export TEST_RESET_TOKEN="your-secure-test-token"`
  - Security: Only required in non-production environments

## Security Features Implemented

### 1. JWT Token Security
- Removed hardcoded secrets from configuration files
- Environment variable-based configuration with fallback validation
- Automatic secure key generation for development if not configured
- Minimum key length validation (32 characters)

### 2. Cookie Security
- HTTPOnly cookies for refresh tokens
- Secure flag based on HTTPS context
- SameSite=Strict for CSRF protection
- Proper expiration handling

### 3. Security Headers
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Strict-Transport-Security (HTTPS only)
- Content-Security-Policy

### 4. Rate Limiting
- Authentication endpoints: 5 requests per 15 minutes
- Registration endpoints: 3 requests per hour
- Token refresh: 10 requests per 15 minutes
- Database operations: 10 requests per minute

### 5. Error Handling
- Removed detailed error messages from API responses
- Proper logging for debugging without information disclosure
- Generic error responses for external consumption

### 6. Database Security
- Enhanced token validation for test endpoints
- IP-based access control for testing environments
- Environment-based endpoint availability
- Secure token generation for development

## Deployment Checklist

### Before Production Deployment:
1. ✅ Set `JWT_SECRET` environment variable
2. ✅ Configure secure connection strings via environment variables
3. ✅ Ensure HTTPS is enabled
4. ✅ Review and adjust CORS policies
5. ✅ Remove or secure development/testing endpoints
6. ✅ Configure proper logging levels
7. ✅ Set appropriate rate limiting rules
8. ✅ Review Content Security Policy settings

### Security Monitoring:
- Monitor authentication failure rates
- Track rate limiting violations
- Monitor for unusual database access patterns
- Review security headers in production

## Development vs Production

### Development
- Uses fallback JWT secret generation if not configured
- Less restrictive logging levels
- Swagger UI enabled
- Cookie secure flag based on HTTPS context

### Production
- Requires explicit JWT_SECRET environment variable
- Minimal logging to reduce information disclosure
- Swagger UI disabled
- Strict security headers enforced
- Database testing endpoints unavailable

## Common Security Issues Resolved

1. **Hard-coded credentials** → Environment variables
2. **Information disclosure** → Generic error messages
3. **Missing security headers** → Comprehensive header middleware
4. **Insecure cookies** → Context-aware security flags
5. **No rate limiting** → Endpoint-specific rate limits
6. **Weak access controls** → Enhanced authentication/authorization