# Blocking Issues Registry

## Active Issues

| ID | Created | Spec | Category | Description | Status | Priority |
|---|---|---|---|---|---|---|
| BI-2025-10-08-001 | 2025-10-08 | all-future-specs | test/functionality/configuration | E2E test cleanup and timeout failures - soft-delete conflicts | active | critical |

## Resolved Issues

| ID | Created | Resolved | Spec | Category | Description | Resolution |
|---|---|---|---|---|---|---|
| BI-2025-09-23-012 | 2025-09-23 | 2025-09-29 | database | database | Foreign key cascade delete configuration prevents constraint validation | Test file removed during integration test cleanup - referenced test no longer exists |
| BI-2025-09-23-011 | 2025-09-23 | 2025-09-29 | api-contract | development | API contract validation failure after authorization changes | All integration tests passing (445/446, 1 skipped for technical debt) |
| BI-2025-09-24-004 | 2025-09-24 | 2025-09-25 | e2e-testing | test | E2E configuration validation timeout failures | Fixed by comprehensive E2E test ecosystem fix (BI-2025-09-25-001) |
| BI-2025-09-24-003 | 2025-09-24 | 2025-09-25 | e2e-testing | performance | E2E API timeout errors | Fixed by comprehensive E2E test ecosystem fix (BI-2025-09-25-001) |
| BI-2025-09-24-002 | 2025-09-24 | 2025-09-25 | e2e-testing | validation | E2E phone number validation failures | Fixed by comprehensive E2E test ecosystem fix (BI-2025-09-25-001) |
| BI-2025-09-24-001 | 2025-09-24 | 2025-09-25 | e2e-testing | configuration | E2E environment detection test failures | Fixed by comprehensive E2E test ecosystem fix (BI-2025-09-25-001) |
| BI-2025-09-23-012 | 2025-09-23 | 2025-09-25 | e2e-testing | test | E2E test TypeError during teardown | Fixed by comprehensive E2E test ecosystem fix (BI-2025-09-25-001) |
| BI-2025-09-23-009 | 2025-09-23 | 2025-09-25 | integration-testing | integration | Multi-provider integration test failures (18 tests) | Fixed - all integration tests now passing (445/446, 1 skipped for technical debt) |
| BI-2025-09-10-001 | 2025-09-10 | 2025-09-29 | e2e-testing | test | E2E test failures in staging deployment pipeline | Eliminated staging branch, implemented 3-stage pipeline (dev → main) |
| BI-2025-09-25-001 | 2025-09-25 | 2025-09-25 | test-ecosystem-stability | test | Test ecosystem instability and regression loops | Fixed unit test architecture - mocked external dependencies, resolved authentication regression loop |
| BI-2025-09-23-006 | 2025-09-23 | 2025-09-23 | integration-test-troubleshooting | test | E2E Playwright config error | Superseded by BI-2025-09-23-008 with more specific TypeError diagnosis |
| BI-2025-09-23-008 | 2025-09-23 | 2025-09-23 | troubleshooting | test | E2E TypeError undefined object | Fixed E2E test authorization and environment detection |
| BI-2025-09-23-* | 2025-09-23 | 2025-09-23 | troubleshooting | various | Multiple integration test issues | Authorization bypass, configuration validation, deserialization, EF InMemory, constraints |
| BI-2025-09-22-* | 2025-09-22 | 2025-09-22 | test-reporting | test | Test reporting workflow misalignment | Fixed workflow configuration |
| BI-2025-09-11-* | 2025-09-11 | 2025-09-11 | middleware | test | Conditional request middleware and authorization conflicts | Fixed middleware configuration |
| BI-2025-09-09-* | 2025-09-09 | 2025-09-09 | testing | test | Auth interceptor failures, password reset isolation, test delay anti-pattern | Fixed test isolation and removed delays |
| BI-2025-09-08-* | 2025-09-08 | 2025-09-08 | infrastructure | compilation | Cache service DI registration, RequestHandlerDelegate error | Fixed DI registration and compilation |
| BI-2025-08-31-* | 2025-08-31 | 2025-08-31 | e2e | test | E2E tests CI failure | Fixed CI configuration |
| BI-2025-08-30-* | 2025-08-30 | 2025-08-30 | authentication | test | Auth integration test failures, smoke test auth failure | Fixed authentication configuration |

## Issue Status Definitions

- **active**: Currently blocking development
- **investigating**: Analysis in progress
- **resolved**: Issue fixed and verified
- **workaround**: Temporary solution in place
- **deferred**: Not currently blocking, scheduled for later

## Escalation Guidelines

- **Critical**: Blocks multiple teams, security issues, production down
- **High**: Blocks current sprint, affects core functionality
- **Medium**: Blocks specific features, workarounds available
- **Low**: Minor impact, can be scheduled for future sprints