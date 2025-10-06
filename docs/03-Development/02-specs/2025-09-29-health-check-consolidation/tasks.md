# Spec Tasks

> Parent Issue: #262
> Created: 2025-09-29
> Status: ✅ COMPLETED
> Completed: 2025-09-29

## Tasks

### 1. Create Custom Detailed Response Writer
**Priority**: High
**Estimated**: 2 hours

- [x] 1.1 Create `WriteDetailedHealthResponse` method in Program.cs
- [x] 1.2 Include environment information (IWebHostEnvironment)
- [x] 1.3 Include database provider from configuration
- [x] 1.4 Include application metadata (name, version, framework)
- [x] 1.5 Format health check entries with name, status, duration, description, exception
- [x] 1.6 Return JSON with proper indentation
- [x] 1.7 Test response format manually

### 2. Update Health Check Registration and Endpoint Mapping
**Priority**: High
**Estimated**: 3 hours

- [x] 2.1 Update `AddHealthChecks()` to tag DatabaseHealthCheck with both "ready" and "live"
- [x] 2.2 Remove old `/health/system` endpoint mapping
- [x] 2.3 Add new `/health` endpoint with "live" tag filter
- [x] 2.4 Keep `/health/ready` endpoint with "ready" tag filter
- [x] 2.5 Add new `/health/detailed` endpoint with custom response writer
- [x] 2.6 Verify no endpoint conflicts or duplicate routes
- [x] 2.7 Test all three endpoints return expected responses

### 3. Update Integration Tests
**Priority**: High
**Estimated**: 4 hours

- [x] 3.1 Update HealthEndpointSmokeTests.cs to expect new response format
- [x] 3.2 Update MultiEnvironmentSmokeTests.cs health check assertions
- [x] 3.3 Update SmokeTestPerformanceValidation.cs health endpoint calls
- [x] 3.4 Change assertions from `{"status": "Healthy"}` to ASP.NET Core format
- [x] 3.5 Verify tests still validate health status correctly
- [x] 3.6 Run full integration test suite to verify no regressions
- [x] 3.7 Verify 445/446 tests still passing

### 4. Delete Deprecated Controllers
**Priority**: Medium
**Estimated**: 1 hour

- [x] 4.1 Delete HealthController.cs
- [x] 4.2 Delete ApiHealthController.cs
- [x] 4.3 Verify no other files reference these controllers
- [x] 4.4 Run full solution build to ensure no compilation errors
- [x] 4.5 Verify application still starts successfully

### 5. Verify E2E Tests Still Pass
**Priority**: High
**Estimated**: 2 hours

- [x] 5.1 Run Playwright smoke tests locally
- [x] 5.2 Verify `/health/ready` endpoint still works as expected
- [x] 5.3 Verify no timeout issues on first POST request
- [x] 5.4 Run full E2E test suite
- [x] 5.5 Verify 45/45 E2E tests still passing

### 6. Update Documentation
**Priority**: Medium
**Estimated**: 3 hours

- [x] 6.1 Update API documentation with new endpoint structure
- [x] 6.2 Document response formats for each endpoint
- [x] 6.3 Add Kubernetes deployment manifest examples
- [x] 6.4 Update CLAUDE.md with new health check information
- [x] 6.5 Create troubleshooting guide for operations team
- [x] 6.6 Document migration path from old endpoints

## Completion Criteria

- [x] All three health endpoints working: `/health`, `/health/ready`, `/health/detailed`
- [x] HealthController.cs and ApiHealthController.cs deleted
- [x] All integration tests passing (445/446)
- [x] All E2E tests passing (45/45)
- [x] Documentation updated
- [x] No compilation errors or warnings

## Estimated Total Time

15 hours

## Dependencies

- None - can start immediately

## Risks

- Integration test format changes may require multiple iterations
- Detailed response format must match previous ApiHealthController format for monitoring tools
- Deployment requires coordination with operations team to update external monitors
