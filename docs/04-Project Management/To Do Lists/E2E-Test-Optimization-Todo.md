# E2E Test Optimization Todo List
*Created: 2025-08-28*  
*Status: Active*  
*Priority: High*

## Executive Summary
After thorough analysis, we've determined that parallel E2E test execution is incompatible with our monolithic architecture using SQLite and EF Core. We're pivoting to optimized serial execution with a focus on speed and reliability.

## Critical Decision
**Abandoning parallel E2E tests in favor of fast, reliable serial execution**
- Rationale: Shared server architecture cannot provide true isolation without massive complexity
- Trade-off: Accepting longer test runs for 100% reliability
- Goal: Optimize serial execution to achieve < 10 minute full suite runs

---

## Phase 1: Immediate Fixes (Day 1-2)
**Goal: Stop the bleeding - make tests reliable**

### ✅ Database Cleanup Fix [CRITICAL]
- [ ] Replace EF Core ExecuteDeleteAsync with file deletion approach
- [ ] Implement retry logic for SQLite busy errors
- [ ] Add connection recycling before cleanup
- [ ] Test cleanup reliability with 10 consecutive runs
- **File**: `src/Infrastructure/Services/DatabaseTestService.cs`
- **Assignee**: Backend Team
- **Due**: Day 1

### ✅ Switch to Single Worker
- [ ] Update all playwright.config files to use `workers: 1`
- [ ] Remove worker pooling code
- [ ] Clean up worker isolation fixtures
- [ ] Update package.json scripts
- **Files**: `test/Tests.E2E.NG/playwright.config*.ts`
- **Assignee**: QA Team
- **Due**: Day 1

### ✅ Single Browser for Local Development
- [ ] Comment out Firefox and WebKit from default config
- [ ] Create separate cross-browser config for CI only
- [ ] Update developer documentation
- **File**: `test/Tests.E2E.NG/playwright.config.ts`
- **Assignee**: QA Team
- **Due**: Day 1

---

## Phase 2: Server Optimization (Day 3-4)
**Goal: Start servers once, not 18 times**

### ✅ Global Server Management
- [ ] Create global-setup.ts that starts servers once
- [ ] Implement proper health checks with retry
- [ ] Add global-teardown.ts for cleanup
- [ ] Remove per-test-file server startup
- **File**: `test/Tests.E2E.NG/tests/setup/global-setup.ts`
- **Assignee**: DevOps Team
- **Due**: Day 3

### ✅ Optimize Angular Dev Server
- [ ] Disable HMR for tests
- [ ] Disable file watching
- [ ] Disable live reload
- [ ] Consider pre-built Angular for CI
- **File**: `src/Angular/angular.json`
- **Assignee**: Frontend Team
- **Due**: Day 3

### ✅ Lean API Configuration
- [ ] Create Testing environment configuration
- [ ] Disable Swagger in test mode
- [ ] Minimize logging (errors only)
- [ ] Disable unnecessary middleware
- **File**: `src/Api/appsettings.Testing.json`
- **Assignee**: Backend Team
- **Due**: Day 4

---

## Phase 3: Test Categorization (Day 5-6)
**Goal: Run only necessary tests**

### ✅ Tag Existing Tests
- [ ] Add @smoke tags to critical path tests (login, CRUD operations)
- [ ] Add @critical tags to important features
- [ ] Add @extended tags to edge cases
- [ ] Add @api-only tags to tests that don't need browser
- **Files**: All `tests/**/*.spec.ts`
- **Assignee**: QA Team
- **Due**: Day 5

### ✅ Create Test Suites
- [ ] playwright.config.smoke.ts (2 min target)
- [ ] playwright.config.critical.ts (5 min target)
- [ ] playwright.config.full.ts (10 min target)
- [ ] playwright.config.api.ts (API only, no browser)
- **Location**: `test/Tests.E2E.NG/`
- **Assignee**: QA Team
- **Due**: Day 5

### ✅ Update Package.json Scripts
- [ ] Add test:smoke script
- [ ] Add test:critical script
- [ ] Add test:api script
- [ ] Add test:changed script (for pre-commit)
- **File**: `test/Tests.E2E.NG/package.json`
- **Assignee**: QA Team
- **Due**: Day 6

---

## Phase 4: Test Speed Optimization (Week 2)
**Goal: Make individual tests faster**

### ✅ Reduce Wait Times
- [ ] Replace waitForTimeout with waitForResponse
- [ ] Replace arbitrary delays with explicit conditions
- [ ] Use Promise.all for parallel API calls within tests
- [ ] Implement batch API operations for test setup
- **Assignee**: QA Team
- **Due**: Week 2, Day 1-2

### ✅ Optimize Test Data Setup
- [ ] Create seed data endpoints for common scenarios
- [ ] Implement database snapshots for complex states
- [ ] Reduce redundant data creation
- [ ] Cache authentication tokens
- **Assignee**: Backend + QA Teams
- **Due**: Week 2, Day 2-3

### ✅ Browser Optimization
- [ ] Disable animations in test mode
- [ ] Skip unnecessary resource loading
- [ ] Reduce screenshot/video collection
- [ ] Optimize viewport size
- **Assignee**: QA Team
- **Due**: Week 2, Day 3

---

## Phase 5: CI/CD Pipeline Update (Week 2)
**Goal: Fast feedback, staged execution**

### ✅ GitHub Actions Workflow
- [ ] Stage 1: Smoke tests (2 min, fail fast)
- [ ] Stage 2: Critical tests (5 min, on PR)
- [ ] Stage 3: Full suite (10 min, on merge)
- [ ] Stage 4: Cross-browser (15 min, nightly)
- **File**: `.github/workflows/e2e-tests.yml`
- **Assignee**: DevOps Team
- **Due**: Week 2, Day 4

### ✅ Test Result Reporting
- [ ] Implement test result aggregation
- [ ] Add failure notifications
- [ ] Create performance tracking dashboard
- [ ] Set up flaky test detection
- **Assignee**: DevOps Team
- **Due**: Week 2, Day 5

---

## Phase 6: Documentation & Training (Week 2)
**Goal: Ensure team adoption**

### ✅ Update Documentation
- [ ] Update testing strategy document
- [ ] Document new test categorization
- [ ] Create troubleshooting guide
- [ ] Update developer onboarding
- **Location**: `docs/05-Quality Control/Testing/`
- **Assignee**: Tech Lead
- **Due**: Week 2, Day 5

### ✅ Team Training
- [ ] Conduct workshop on new test approach
- [ ] Create video tutorial for common tasks
- [ ] Document best practices
- [ ] Set up office hours for questions
- **Assignee**: QA Lead
- **Due**: Week 2, End

---

## Success Metrics

### Performance Targets
- [ ] Smoke tests: < 2 minutes
- [ ] Critical tests: < 5 minutes  
- [ ] Full suite: < 10 minutes
- [ ] Cross-browser: < 15 minutes

### Reliability Targets
- [ ] 100% pass rate for 10 consecutive runs
- [ ] Zero database cleanup failures
- [ ] < 5% flaky test rate
- [ ] 90% reduction in CI failures

### Developer Experience
- [ ] 2-minute feedback loop for critical changes
- [ ] Easy test debugging
- [ ] Clear test categorization
- [ ] Predictable test behavior

---

## Risk Mitigation

### Known Risks
1. **Serial execution too slow**
   - Mitigation: Aggressive test categorization, run only smoke tests locally
   
2. **Single point of failure with shared servers**
   - Mitigation: Robust health checks, automatic restart capability
   
3. **Database file locks in CI**
   - Mitigation: File deletion approach, retry logic

4. **Team resistance to serial testing**
   - Mitigation: Show improved reliability, faster feedback with smoke tests

---

## Rollback Plan
If serial optimization fails to achieve targets:
1. Investigate PostgreSQL migration for better isolation
2. Consider containerized test environments
3. Evaluate cloud-based testing services
4. Reduce E2E test coverage in favor of integration tests

---

## Notes
- All dates assume start date of next Monday
- Weekend work not required or expected
- Daily standups to track progress
- Escalation path: QA Lead → Tech Lead → Engineering Manager

## Sign-off
- [ ] QA Lead approval
- [ ] Tech Lead approval
- [ ] Engineering Manager approval
- [ ] Product Owner informed