# API Specification

This is the API specification for the spec detailed in @docs/03-development/02-specs/2025-09-20-multi-config-e2e-testing/spec.md

## Endpoints

### GET /health/config

**Purpose:** Validate configuration loading and provide environment information for test verification
**Parameters:** None
**Response:** JSON object with configuration status and environment details
**Errors:** 500 if configuration loading fails

```json
{
  "status": "healthy",
  "environment": "Testing",
  "databaseProvider": "SQLite",
  "configurationSummary": {
    "hasValidConnectionString": true,
    "externalServicesConfigured": ["EmailService", "LoggingService"],
    "securitySettings": {
      "jwtConfigured": true,
      "corsConfigured": true
    }
  },
  "timestamp": "2025-09-20T10:30:00Z"
}
```

### POST /api/test/reset-database

**Purpose:** Reset test database to known state for E2E testing across configurations
**Parameters:**
- `resetToken` (header): Test-only authentication token
- `environment` (body): Target environment configuration

**Response:** Success confirmation with database state
**Errors:**
- 401 if reset token invalid or missing
- 403 if not in testing environment
- 500 if database reset fails

```json
{
  "success": true,
  "environment": "Testing",
  "databaseProvider": "SQLite",
  "tablesReset": ["People", "Roles", "Walls", "Windows"],
  "seedDataApplied": true,
  "timestamp": "2025-09-20T10:30:00Z"
}
```

### GET /api/test/config-validation

**Purpose:** Validate that current configuration can support E2E testing
**Parameters:** None
**Response:** Configuration validation results
**Errors:** 500 if configuration is invalid for testing

```json
{
  "isTestable": true,
  "environment": "Testing",
  "validationResults": {
    "databaseConnectable": true,
    "testDataSeeded": true,
    "externalServicesMocked": true,
    "securityConfigValid": true
  },
  "warnings": [],
  "errors": [],
  "timestamp": "2025-09-20T10:30:00Z"
}
```

## Controllers

### HealthController Enhancement
- Extend existing health controller with configuration-specific health checks
- Add environment detection and configuration validation
- Provide secure configuration summary without exposing secrets

### TestController (Testing Environment Only)
- New controller active only in Testing environment
- Database reset functionality with proper authentication
- Configuration validation endpoints for E2E test setup
- Middleware validation to ensure proper test environment isolation

## Purpose

These endpoints enable E2E tests to:
1. **Verify Environment State** - Confirm the application is running with expected configuration
2. **Reset Test State** - Ensure clean state between test runs across different configurations
3. **Validate Configuration** - Confirm configuration is properly loaded and testable
4. **Enable Configuration Matrix Testing** - Support automated testing across multiple environment configurations