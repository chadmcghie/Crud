# API Specification

> Parent Spec: Health Check Consolidation
> Created: 2025-09-29

## Endpoint Summary

| Endpoint | Purpose | Used By | Response Format |
|----------|---------|---------|----------------|
| `GET /health` | Liveness probe | Kubernetes, Load Balancers | Standard health check JSON |
| `GET /health/ready` | Readiness probe | Kubernetes, Playwright | Standard health check JSON |
| `GET /health/detailed` | Diagnostics | Operations, Support | Detailed JSON with metadata |

## Endpoint Specifications

### GET /health (Liveness Probe)

**Purpose**: Determine if the application is alive and responsive. If this check fails, the orchestrator should restart the application.

**Tags**: Filters health checks tagged with `"live"`

**Response Codes**:
- `200 OK` - Application is alive
- `503 Service Unavailable` - Application is unhealthy

**Response Format** (Healthy):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0234567",
      "description": "Database ready for operations"
    }
  }
}
```

**Response Format** (Unhealthy):
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "database": {
      "status": "Unhealthy",
      "duration": "00:00:00.0123456",
      "description": "Database not ready",
      "exception": "Error message here"
    }
  }
}
```

**Example Usage**:
```bash
curl http://localhost:5172/health
```

**Kubernetes Configuration**:
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 5172
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

---

### GET /health/ready (Readiness Probe)

**Purpose**: Determine if the application is ready to accept traffic. If this check fails, the orchestrator should stop sending requests but NOT restart the application.

**Tags**: Filters health checks tagged with `"ready"`

**Response Codes**:
- `200 OK` - Application is ready for traffic
- `503 Service Unavailable` - Application is not ready (still warming up or temporarily unavailable)

**Response Format**: Same as `/health` endpoint

**Key Difference from /health**: This endpoint ensures EF Core has completed model compilation by executing `CountAsync()` during the health check, preventing cold-start timeouts on first requests.

**Example Usage**:
```bash
curl http://localhost:5172/health/ready
```

**Kubernetes Configuration**:
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 5172
  initialDelaySeconds: 10
  periodSeconds: 5
  timeoutSeconds: 3
  successThreshold: 1
  failureThreshold: 3
```

**Playwright Configuration** (already in place):
```typescript
webServer: [{
  url: 'http://localhost:5172/health/ready',
  timeout: 120000
}]
```

---

### GET /health/detailed (Diagnostic Endpoint)

**Purpose**: Provide comprehensive diagnostic information for troubleshooting production issues. Includes environment details, database provider information, and application metadata.

**Tags**: All health checks (no filtering)

**Response Codes**:
- `200 OK` - Application is healthy
- `503 Service Unavailable` - Application is unhealthy

**Response Format**:
```json
{
  "status": "Healthy",
  "environment": "Production",
  "databaseProvider": "SQLite",
  "timestamp": "2025-09-29T18:00:00.0000000Z",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 23.4567,
      "description": "Database ready for operations",
      "exception": null
    }
  ],
  "application": {
    "name": "CRUD API",
    "version": "1.0.0",
    "framework": ".NET 8"
  }
}
```

**Example Usage**:
```bash
curl http://localhost:5172/health/detailed
```

**Use Cases**:
- Production troubleshooting
- Monitoring dashboard integration
- Manual health verification during deployments

---

## Deprecated Endpoints

The following endpoints will be **removed** during this consolidation:

| Old Endpoint | Replacement | Migration Notes |
|-------------|-------------|----------------|
| `GET /health/quick` | `GET /health` | Quick check replaced by standard liveness probe |
| `GET /health/system` | `GET /health` | System health merged into liveness endpoint |
| `GET /api/health` | `GET /health/detailed` | Detailed info moved to standard naming convention |

**Migration Timeline**:
- Controllers marked for deletion in this spec implementation
- Old endpoints will return 404 after deployment
- Update any external monitors or scripts before deployment

---

## Response Format Standards

All health check endpoints follow ASP.NET Core Health Checks JSON format:

**Common Fields**:
- `status`: "Healthy", "Degraded", or "Unhealthy"
- `totalDuration`: Time taken to run all health checks
- `entries`: Dictionary of individual health check results

**Entry Fields**:
- `status`: Individual check status
- `duration`: Time taken for this specific check
- `description`: Human-readable description
- `exception`: Error message if unhealthy (optional)

**HTTP Status Mapping**:
- Healthy → 200 OK
- Degraded → 200 OK (with status field indicating degradation)
- Unhealthy → 503 Service Unavailable

---

## Testing Strategy

### Integration Test Updates

Update tests to verify new endpoint behavior:

**Test Cases**:
1. `/health` returns 200 when database is healthy
2. `/health/ready` returns 200 after EF Core warm-up
3. `/health/detailed` returns comprehensive JSON with all metadata
4. All endpoints return 503 when database is unreachable
5. Response format matches ASP.NET Core standard

**Files to Update**:
```
test/Tests.Integration.Backend/SmokeTests/HealthEndpointSmokeTests.cs
test/Tests.Integration.Backend/Configuration/MultiEnvironmentSmokeTests.cs
test/Tests.Integration.Backend/SmokeTests/SmokeTestPerformanceValidation.cs
```

### E2E Test Verification

Verify Playwright still works with `/health/ready`:
- No changes needed to playwright.config.ts
- Verify E2E tests don't timeout on first POST request
- Confirm readiness check prevents cold-start issues

---

## Documentation Updates

After implementation, update the following documentation:

1. **API Documentation**: Update endpoint reference with new structure
2. **Deployment Guide**: Add Kubernetes manifest examples
3. **Troubleshooting Guide**: Document health check usage for support team
4. **CLAUDE.md**: Update health check information in project overview
