# Technical Specification

This is the technical specification for the spec detailed in @docs/03-Development/specs/2025-09-29-branch-deployment-strategy/spec.md

## Technical Requirements

### 1. Workflow File Restructuring

#### Rename deploy-staging.yml to deploy-dev.yml
- **File:** `.github/workflows/deploy-staging.yml` → `.github/workflows/deploy-dev.yml`
- **Changes Required:**
  - Update workflow name: `name: Deploy to Staging` → `name: Deploy to Dev`
  - Keep trigger: `on: push: branches: [dev]` (already correct)
  - Update all job names:
    - `build-staging` → `build-dev`
    - `staging-smoke-tests` → `dev-e2e-tests`
    - `prepare-staging-artifacts` → `prepare-dev-artifacts`
    - `deploy-staging` → `deploy-dev`
  - Update all artifact names:
    - `staging-build-` → `dev-build-`
    - `staging-artifacts-` → `dev-artifacts-`
    - `staging-smoke-tests-` → `dev-e2e-test-results-`
  - Update all environment references:
    - `staging` environment → `dev` environment
    - `staging.your-app.com` → `dev.your-app.com`
  - Update all comments and echo messages:
    - Replace "staging" with "dev" throughout
  - **CRITICAL:** Line 164 test command change:
    - FROM: `npm run test:smoke`
    - TO: `npm run test:extended`
    - Update comments to reflect full E2E suite execution (~15-20 min)

#### Create deploy-production.yml
- **File:** `.github/workflows/deploy-production.yml` (NEW)
- **Trigger:**
  ```yaml
  on:
    push:
      branches: [main]
    workflow_dispatch:
  ```
- **Structure:** Mirror `deploy-dev.yml` with these key differences:
  - Environment: `production`
  - Build configuration: Production settings
  - Angular build: `--configuration=production`
  - Tests: `npm run test:smoke` (not full suite - already validated in dev)
  - Deployment: Production-specific deployment steps
  - Health checks: Production endpoints
- **Jobs Required:**
  1. `check-commit` - Skip if Copilot commit
  2. `build-production` - Build with production configuration
  3. `production-smoke-tests` - Quick validation (~2-3 min)
  4. `prepare-production-artifacts` - Package for production
  5. `deploy-production` - Deploy to production with health checks
  6. `notify-deployment` - Notification summary

### 2. Branch Cleanup

#### Delete Staging Branch
- **Command:** `git push origin --delete staging`
- **Verification:** Confirm branch no longer exists in remote repository
- **Local Cleanup:** `git branch -d staging` (if exists locally)

### 3. Testing Strategy Implementation

#### Current Test Distribution
- **Feature push:** Unit tests only (~3-5 min)
- **PR to dev:** Integration + smoke E2E (~5-10 min)
- **Dev deployment:** Smoke tests only (~2-3 min) ❌ INSUFFICIENT
- **PR to main:** Integration + critical E2E (~10-15 min)
- **Production deployment:** None ❌ MISSING

#### New Test Distribution
- **Feature push:** Unit tests only (~3-5 min) ✅ No change
- **PR to dev:** Integration + smoke E2E (~5-10 min) ✅ No change
- **Dev deployment:** **Full E2E suite** (~15-20 min) ✅ UPGRADED
- **PR to main:** Integration + critical E2E (~10-15 min) ✅ No change
- **Production deployment:** Smoke tests + health checks (~2-3 min) ✅ NEW

#### Test Command Specifications

**test:smoke** (2-3 minutes)
- Tests tagged with `@smoke`
- ~10-15 critical path tests
- Used in: PR validation, production deployment

**test:critical** (5-10 minutes)
- Tests tagged with `@critical` (includes `@smoke`)
- ~25-30 important flow tests
- Used in: PR to main validation

**test:extended** (15-20 minutes)
- All tests (all tags: `@smoke`, `@critical`, `@extended`)
- ~50-100+ comprehensive tests
- Used in: Dev environment deployment

### 4. Workflow Dependencies and Sequencing

#### Feature → Dev Flow
```
1. Feature push → feature-branch.yml
   ├─ Unit tests
   ├─ Code quality
   └─ Build artifacts

2. PR to dev → pr-to-dev.yml
   ├─ Wait for feature workflow
   ├─ Integration tests (parallel)
   └─ Smoke E2E tests (parallel)

3. Merge to dev → deploy-dev.yml
   ├─ Build for dev
   ├─ Deploy to dev environment
   ├─ Run FULL E2E suite (test:extended)
   └─ Notify results
```

#### Dev → Production Flow
```
1. PR to main → pr-to-main.yml
   ├─ Integration tests
   ├─ Critical E2E tests
   └─ Production readiness checks

2. Merge to main → deploy-production.yml (NEW)
   ├─ Build for production
   ├─ Deploy to production environment
   ├─ Run smoke tests
   ├─ Health checks
   └─ Notify results
```

### 5. Configuration Changes

#### Environment Variables
No new environment variables required. Existing variables used:
- `CI=true`
- `ASPNETCORE_ENVIRONMENT=Testing` (for dev) / `Production` (for prod)
- `DatabaseProvider=SQLite`
- `TEST_RESET_TOKEN=test-only-token`
- `TEST_RUN_ID` (unique per run)

#### Workflow Permissions
No permission changes required. Existing permissions sufficient:
- `contents: write` (for auto-formatting)
- `actions: read` (for artifact downloads)
- `security-events: write` (for CodeQL)

### 6. Artifact Management

#### Current Artifacts
- `feature-build-{sha}` - from feature-branch.yml
- `staging-build-{sha}` - from deploy-staging.yml (to be renamed)
- `staging-artifacts-{sha}` - from deploy-staging.yml (to be renamed)

#### New Artifacts
- `feature-build-{sha}` - from feature-branch.yml (no change)
- `dev-build-{sha}` - from deploy-dev.yml (renamed)
- `dev-artifacts-{sha}` - from deploy-dev.yml (renamed)
- `production-build-{sha}` - from deploy-production.yml (NEW)
- `production-artifacts-{sha}` - from deploy-production.yml (NEW)

#### Retention Policy
- Feature artifacts: 7 days
- Dev artifacts: 7 days
- Production artifacts: 30 days (longer retention for audit trail)

### 7. Rollback Strategy

#### Dev Environment Rollback
- Manual trigger of previous successful deploy-dev.yml run
- OR push revert commit to dev branch

#### Production Environment Rollback
- Manual trigger of previous successful deploy-production.yml run
- OR create hotfix branch from main, revert changes, fast-track through pipeline

### 8. Performance Considerations

#### Pipeline Duration Impact
- **Before:**
  - Feature push: 3-5 min
  - PR to dev: 5-10 min
  - Dev deployment: 5-10 min (smoke only)
  - **Total: ~13-25 min to dev**

- **After:**
  - Feature push: 3-5 min (no change)
  - PR to dev: 5-10 min (no change)
  - Dev deployment: 20-30 min (full E2E)
  - **Total: ~28-45 min to dev**

**Rationale:** Increased dev deployment time is acceptable because:
- Occurs post-merge, not blocking developers
- Provides comprehensive pre-production validation
- Catches issues before production
- Worth the investment for quality assurance

#### Optimization Opportunities
- Parallel test execution (already implemented)
- Artifact reuse (already implemented)
- Caching strategies (already implemented)
- Test sharding (future enhancement if needed)

### 9. Migration Steps

#### Implementation Order
1. Create and test `deploy-production.yml` on a test branch
2. Rename `deploy-staging.yml` to `deploy-dev.yml` with all updates
3. Update `deploy-dev.yml` test command to `test:extended`
4. Test workflows end-to-end on feature branch
5. Update documentation (see documentation-updates.md)
6. Merge changes to dev
7. Verify dev deployment runs full E2E suite
8. Delete staging branch after successful validation

#### Rollback Plan
If issues occur:
- Keep old workflow files in git history
- Can revert workflow changes independently
- Branch deletion can be reversed via git reflog if needed within 30 days

### 10. Validation Criteria

#### Technical Acceptance Criteria
- [ ] `deploy-dev.yml` exists and `deploy-staging.yml` does not
- [ ] `deploy-production.yml` exists and triggers on main branch push
- [ ] Dev deployment runs `npm run test:extended` (verify in logs)
- [ ] Production deployment runs `npm run test:smoke` (verify in logs)
- [ ] Staging branch does not exist in remote repository
- [ ] All workflow jobs have appropriate naming (no "staging" references in dev workflow)
- [ ] Artifacts use correct naming convention (dev-* and production-*)
- [ ] Test suites complete successfully in expected timeframes

#### Functional Acceptance Criteria
- [ ] Feature branch workflow completes successfully
- [ ] PR to dev workflow completes with integration + smoke tests
- [ ] Dev deployment completes with full E2E suite
- [ ] PR to main workflow completes with production readiness checks
- [ ] Production deployment completes with smoke tests + health checks
- [ ] No broken references in workflows or documentation