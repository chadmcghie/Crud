# Spec Tasks

> Parent Issue: #262
> Created: 2025-09-29
> Status: Not Started

## Tasks

### 1. Create Custom Detailed Response Writer
**Priority**: High
**Estimated**: 2 hours

- [ ] 1.1 Create `WriteDetailedHealthResponse` method in Program.cs
- [ ] 1.2 Include environment information (IWebHostEnvironment)
- [ ] 1.3 Include database provider from configuration
- [ ] 1.4 Include application metadata (name, version, framework)
- [ ] 1.5 Format health check entries with name, status, duration, description, exception
- [ ] 1.6 Return JSON with proper indentation
- [ ] 1.7 Test response format manually

### 2. Update Health Check Registration and Endpoint Mapping
**Priority**: High
**Estimated**: 3 hours

- [ ] 2.1 Update `AddHealthChecks()` to tag DatabaseHealthCheck with both "ready" and "live"
- [ ] 2.2 Remove old `/health/system` endpoint mapping
- [ ] 2.3 Add new `/health` endpoint with "live" tag filter
- [ ] 2.4 Keep `/health/ready` endpoint with "ready" tag filter
- [ ] 2.5 Add new `/health/detailed` endpoint with custom response writer
- [ ] 2.6 Verify no endpoint conflicts or duplicate routes
- [ ] 2.7 Test all three endpoints return expected responses

### 3. Update Integration Tests
**Priority**: High
**Estimated**: 4 hours

- [ ] 3.1 Update HealthEndpointSmokeTests.cs to expect new response format
- [ ] 3.2 Update MultiEnvironmentSmokeTests.cs health check assertions
- [ ] 3.3 Update SmokeTestPerformanceValidation.cs health endpoint calls
- [ ] 3.4 Change assertions from `{"status": "Healthy"}` to ASP.NET Core format
- [ ] 3.5 Verify tests still validate health status correctly
- [ ] 3.6 Run full integration test suite to verify no regressions
- [ ] 3.7 Verify 445/446 tests still passing

### 4. Delete Deprecated Controllers
**Priority**: Medium
**Estimated**: 1 hour

- [ ] 4.1 Delete HealthController.cs
- [ ] 4.2 Delete ApiHealthController.cs
- [ ] 4.3 Verify no other files reference these controllers
- [ ] 4.4 Run full solution build to ensure no compilation errors
- [ ] 4.5 Verify application still starts successfully

### 5. Verify E2E Tests Still Pass
**Priority**: High
**Estimated**: 2 hours

- [ ] 5.1 Run Playwright smoke tests locally
- [ ] 5.2 Verify `/health/ready` endpoint still works as expected
- [ ] 5.3 Verify no timeout issues on first POST request
- [ ] 5.4 Run full E2E test suite
- [ ] 5.5 Verify 45/45 E2E tests still passing

### 6. Update Documentation
**Priority**: Medium
**Estimated**: 3 hours

- [ ] 6.1 Update API documentation with new endpoint structure
- [ ] 6.2 Document response formats for each endpoint
- [ ] 6.3 Add Kubernetes deployment manifest examples
- [ ] 6.4 Update CLAUDE.md with new health check information
- [ ] 6.5 Create troubleshooting guide for operations team
- [ ] 6.6 Document migration path from old endpoints

## Completion Criteria

- [ ] All three health endpoints working: `/health`, `/health/ready`, `/health/detailed`
- [ ] HealthController.cs and ApiHealthController.cs deleted
- [ ] All integration tests passing (445/446)
- [ ] All E2E tests passing (45/45)
- [ ] Documentation updated
- [ ] No compilation errors or warnings

## Estimated Total Time

15 hours

## Dependencies

- None - can start immediately

## Risks

- Integration test format changes may require multiple iterations
- Detailed response format must match previous ApiHealthController format for monitoring tools
- Deployment requires coordination with operations team to update external monitors
