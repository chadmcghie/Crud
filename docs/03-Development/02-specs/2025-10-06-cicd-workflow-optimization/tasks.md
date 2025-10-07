# Spec Tasks

> Parent Issue: #276

## Tasks

- [x] 1. Create Dependabot CI Workflow (Issue: #281)
  - [x] 1.1 Create `.github/workflows/dependabot-ci.yml` workflow file
  - [x] 1.2 Add path detection logic using `dorny/paths-filter@v3` for dependency types
  - [x] 1.3 Implement conditional jobs for .NET unit tests
  - [x] 1.4 Implement conditional jobs for Angular unit tests
  - [x] 1.5 Implement conditional jobs for E2E test unit tests
  - [x] 1.6 Add summary job to display validated dependency types
  - [x] 1.7 Test workflow with simulated Dependabot PR
  - [x] 1.8 Verify workflow completes in 2-3 minutes target

- [ ] 2. Implement Path Filtering in Feature Branch Workflow (Issue: #282)
  - [ ] 2.1 Add `detect-changes` job using `dorny/paths-filter@v3` to `feature-branch.yml`
  - [ ] 2.2 Configure filter patterns for backend and frontend paths
  - [ ] 2.3 Update `backend-unit-tests` job with conditional execution based on detected changes
  - [ ] 2.4 Update `frontend-unit-tests` job with conditional execution
  - [ ] 2.5 Optimize CodeQL build matrix to be conditional on detected changes
  - [ ] 2.6 Update summary job to display detected changes and skipped tests
  - [ ] 2.7 Test with frontend-only changes (verify backend tests skipped)
  - [ ] 2.8 Test with backend-only changes (verify frontend tests skipped)
  - [ ] 2.9 Verify all tests still run when both backend and frontend change

- [ ] 3. Add Playwright Browser Caching to All E2E Workflows (Issue: #283)
  - [ ] 3.1 Add cache step using `actions/cache@v4` to `pr-to-dev.yml` E2E job
  - [ ] 3.2 Configure cache key with `playwright-${{ runner.os }}-${{ hashFiles('test/Tests.E2E.NG/package-lock.json') }}`
  - [ ] 3.3 Update Playwright install command to remove `--with-deps` flag in `pr-to-dev.yml`
  - [ ] 3.4 Add cache step to `pr-to-main.yml` E2E job
  - [ ] 3.5 Add cache step to `deploy-dev.yml` E2E job
  - [ ] 3.6 Add cache step to `deploy-production.yml` E2E job
  - [ ] 3.7 Test cache hit scenario (verify ~30 second restore)
  - [ ] 3.8 Test cache miss scenario (verify 3-4 minute install without --with-deps)
  - [ ] 3.9 Verify all E2E tests pass with cached browsers

- [ ] 4. Update PR to Dev Workflow for Conditional Execution (Issue: #284)
  - [ ] 4.1 Add `check-actor` job to detect Dependabot PRs in `pr-to-dev.yml`
  - [ ] 4.2 Update `wait-for-feature-workflow` to skip when actor is dependabot[bot]
  - [ ] 4.3 Update `integration-tests` condition to run for Dependabot OR completed feature workflow
  - [ ] 4.4 Update `smoke-tests` condition similarly
  - [ ] 4.5 Test with Dependabot PR (verify feature workflow skipped, tests still run)
  - [ ] 4.6 Test with human PR (verify existing behavior maintained)
  - [ ] 4.7 Verify all tests pass for both scenarios

- [ ] 5. Validate Performance Targets and Integration (Issue: #285)
  - [ ] 5.1 Create test Dependabot PR and measure execution time (target: 6-7 min)
  - [ ] 5.2 Create frontend-only PR and measure execution time (target: 7-8 min)
  - [ ] 5.3 Create backend-only PR and measure execution time (target: 14-15 min)
  - [ ] 5.4 Verify Playwright cache hit rate over 5 consecutive runs (target: 90%+)
  - [ ] 5.5 Verify no reduction in test coverage (all tests run when relevant)
  - [ ] 5.6 Confirm branch protection compatibility with new workflow checks
  - [ ] 5.7 Document any deviations from performance targets with justification
  - [ ] 5.8 Update CLAUDE.md if workflow changes require documentation updates
