# Tasks

> Spec: 3-Stage Branch and Deployment Strategy
> Created: 2025-09-29
> GitHub Issue: #258

## Task Status Legend
- 🔲 Not Started
- 🔄 In Progress
- ✅ Completed
- ⏸️ Blocked

---

## Task 1: Rename and Update deploy-staging.yml to deploy-dev.yml ✅

**Description:** Rename the workflow file and update all internal references from "staging" to "dev" throughout the file.

**Reference:** @sub-specs/technical-spec.md (Section 1 - Workflow File Restructuring)

**Subtasks:**
- 1.1 🔲 Rename file: `.github/workflows/deploy-staging.yml` → `.github/workflows/deploy-dev.yml`
- 1.2 🔲 Update workflow name: `name: Deploy to Staging` → `name: Deploy to Dev`
- 1.3 🔲 Update all job names (build-staging → build-dev, staging-smoke-tests → dev-e2e-tests, etc.)
- 1.4 🔲 Update all artifact names (staging-build-* → dev-build-*, staging-artifacts-* → dev-artifacts-*)
- 1.5 🔲 Update environment references (staging → dev, staging.your-app.com → dev.your-app.com)
- 1.6 🔲 Update all comments and echo messages (replace "staging" with "dev" throughout)
- 1.7 🔲 **CRITICAL:** Change line ~164 test command from `npm run test:smoke` to `npm run test:extended`
- 1.8 🔲 Update test duration comments to reflect ~15-20 minutes for full E2E suite
- 1.9 🔲 Verify trigger remains `on: push: branches: [dev]` (already correct)

**Acceptance Criteria:**
- [ ] File renamed to deploy-dev.yml
- [ ] No references to "staging" remain in file (except URLs if needed)
- [ ] Test command runs `npm run test:extended`
- [ ] All job and artifact names use "dev" prefix
- [ ] Comments accurately reflect full E2E suite execution

---

## Task 2: Create deploy-production.yml Workflow ✅

**Description:** Create new production deployment workflow that triggers on push to main branch.

**Reference:** @sub-specs/technical-spec.md (Section 1 - Create deploy-production.yml)

**Subtasks:**
- 2.1 🔲 Create new file: `.github/workflows/deploy-production.yml`
- 2.2 🔲 Set up workflow trigger for push to `main` branch and workflow_dispatch
- 2.3 🔲 Implement `check-commit` job (skip if Copilot commit)
- 2.4 🔲 Implement `build-production` job with production configuration
- 2.5 🔲 Configure Angular build with `--configuration=production`
- 2.6 🔲 Implement `production-smoke-tests` job running `npm run test:smoke` (~2-3 min)
- 2.7 🔲 Implement `prepare-production-artifacts` job for production packaging
- 2.8 🔲 Implement `deploy-production` job with health checks and deployment steps
- 2.9 🔲 Implement `notify-deployment` job with deployment summary
- 2.10 🔲 Set artifact retention to 30 days for production (longer than dev's 7 days)

**Acceptance Criteria:**
- [ ] Workflow file exists at correct path
- [ ] Triggers on push to main branch
- [ ] Runs smoke tests only (not full suite)
- [ ] Includes production-specific build configuration
- [ ] Has appropriate health checks and notifications
- [ ] Artifact naming uses "production-" prefix

---

## Task 3: Delete Unused Staging Branch ⏸️

**Description:** Remove the staging branch from both remote and local repositories.

**Reference:** @sub-specs/technical-spec.md (Section 2 - Branch Cleanup)

**Status:** ⏸️ **BLOCKED** - Staging branch is protected and cannot be deleted via git command

**Required Manual Actions:**
1. Go to GitHub → Settings → Branches
2. Remove branch protection rule for `staging` branch
3. Then run: `git push origin --delete staging`
4. Delete local branch if exists: `git branch -d staging`

**Subtasks:**
- 3.1 ✅ Verified no active work exists on staging branch
- 3.2 ⏸️ Delete remote staging branch - BLOCKED by branch protection
- 3.3 🔲 Delete local staging branch if exists: `git branch -d staging`
- 3.4 🔲 Verify branch no longer exists in repository

**Acceptance Criteria:**
- [ ] Branch protection removed from staging branch
- [ ] Staging branch does not exist in remote repository
- [ ] Staging branch does not exist in local repository
- [ ] No errors when attempting to verify deletion

**Note:** This task must be completed manually after branch protection is removed from GitHub Settings.

---

## Task 4: Update .github/BRANCH_PROTECTION_RULES.md ✅

**Description:** Update branch protection rules documentation to reflect 3-stage pipeline.

**Reference:** @sub-specs/documentation-updates.md (Section 1)

**Subtasks:**
- 4.1 🔲 Update "Branch Structure" section - remove staging branch reference
- 4.2 🔲 Update "Branch Flow" section - change to `feature/bugfix → dev → main`
- 4.3 🔲 Remove entire "For staging branch" section (lines 58-79)
- 4.4 🔲 Update "Developer Workflow" - change deploy-staging.yml to deploy-dev.yml
- 4.5 🔲 Update "Release Workflow" - remove staging step, update to 2-step process (dev → main)
- 4.6 🔲 Update "Environment Protection Rules" - remove staging environment
- 4.7 🔲 Update "Automated Workflow Triggers" table with correct workflow names and timings
- 4.8 🔲 Update "Testing Strategy" section to reflect dev environment deployment running full E2E

**Acceptance Criteria:**
- [ ] No references to staging branch remain
- [ ] Branch flow shows 3-stage pipeline
- [ ] Workflow file names match actual files
- [ ] Test durations are accurate
- [ ] Environment references are correct (dev and production only)

---

## Task 5: Update docs/06-devops/development-workflow.md ✅

**Description:** Update development workflow documentation to reflect accurate 3-stage pipeline.

**Reference:** @sub-specs/documentation-updates.md (Section 2)

**Subtasks:**
- 5.1 🔲 Update "Branch Types" table to clarify dev branch deploys to dev environment
- 5.2 🔲 Update "Merging to Dev" section - change deploy-staging.yml to deploy-dev.yml
- 5.3 🔲 Rename "Working with Staging" section to "Working with Dev Environment"
- 5.4 🔲 Update deployment description - change from staging to dev environment
- 5.5 🔲 Update deployment duration to 20-30 minutes (includes full E2E suite)
- 5.6 🔲 Update testing steps to mention automated full E2E suite runs first
- 5.7 🔲 Update "Production Deployment" section - change "staging approved" to "dev environment validated"
- 5.8 🔲 Update requirements to mention full E2E test suite passed

**Acceptance Criteria:**
- [ ] "Staging" terminology replaced with "dev environment"
- [ ] Workflow file names are correct
- [ ] Deployment durations are accurate
- [ ] Testing process is accurately described
- [ ] Branch flow is clear and correct

---

## Task 6: Update CLAUDE.md ✅

**Description:** Update project overview documentation with branch and deployment strategy.

**Reference:** @sub-specs/documentation-updates.md (Section 3)

**Subtasks:**
- 6.1 🔲 Add "Branch and Deployment Strategy" section after Architecture
- 6.2 🔲 Document 3-stage pipeline structure
- 6.3 🔲 Document deployment triggers (dev → deploy-dev.yml, main → deploy-production.yml)
- 6.4 🔲 Add reference to spec documentation
- 6.5 🔲 Add/update "Testing Strategy" section with test distribution
- 6.6 🔲 Remove any staging confusion references from "Known Issues"

**Acceptance Criteria:**
- [ ] Clear branch → environment mapping documented
- [ ] Deployment triggers are explicit
- [ ] Testing strategy shows when each test type runs
- [ ] Reference to spec included
- [ ] No confusing staging references remain

---

## Task 7: Validation and Testing ✅

**Description:** Validate all changes work correctly and documentation is consistent.

**Reference:** @sub-specs/technical-spec.md (Section 10 - Validation Criteria)

**Subtasks:**
- 7.1 🔲 Verify deploy-dev.yml exists and deploy-staging.yml does not
- 7.2 🔲 Verify deploy-production.yml exists and is properly configured
- 7.3 🔲 Verify staging branch does not exist in remote repository
- 7.4 🔲 Search all workflow files for any remaining "staging" references (should be none in deploy-dev.yml)
- 7.5 🔲 Search all documentation for "staging" - verify only appropriate usage remains
- 7.6 🔲 Verify workflow naming consistency across all three documentation files
- 7.7 🔲 Verify test suite names and durations are consistent
- 7.8 🔲 Run local validation if possible (syntax check workflows)

**Acceptance Criteria:**
- [ ] All technical acceptance criteria from spec met
- [ ] All functional acceptance criteria from spec met
- [ ] Documentation is internally consistent
- [ ] No broken references or incorrect workflow names
- [ ] Ready for PR creation

---

## Summary

**Total Tasks:** 7 parent tasks, 52 subtasks
**Estimated Effort:** 3-4 hours
**GitHub Issue:** #258
**Spec Location:** @docs/03-Development/specs/2025-09-29-branch-deployment-strategy/

**Implementation Order:**
1. Create deploy-production.yml (Task 2) - new file, least risk
2. Rename and update deploy-staging.yml (Task 1) - workflow changes
3. Delete staging branch (Task 3) - cleanup
4. Update documentation (Tasks 4-6) - documentation consistency
5. Validation (Task 7) - final checks