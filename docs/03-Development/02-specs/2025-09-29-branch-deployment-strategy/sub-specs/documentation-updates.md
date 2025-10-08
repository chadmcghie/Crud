# Documentation Updates Specification

This document details all documentation updates required for the spec detailed in @docs/03-Development/specs/2025-09-29-branch-deployment-strategy/spec.md

## Overview

Three documentation files require updates to reflect the accurate 3-stage pipeline architecture and eliminate references to the unused staging branch.

---

## 1. Update .github/BRANCH_PROTECTION_RULES.md

**File:** `.github/BRANCH_PROTECTION_RULES.md`

### Changes Required

#### Section: Branch Structure (lines 7-12)
**Current:**
```markdown
- **`dev`** - Default branch, integration branch for features
- **`staging`** - Pre-production branch, full E2E testing environment
- **`main`** - Production branch, requires management approval
```

**Update To:**
```markdown
- **`dev`** - Default branch, integration branch for features, deploys to dev environment
- **`main`** - Production branch, requires management approval, deploys to production
```

**Remove:**
- Reference to `staging` branch

#### Section: Branch Flow (lines 14-20)
**Current:**
```markdown
The strict branch flow is: `feature/bugfix → dev → staging → main`
```

**Update To:**
```markdown
The strict branch flow is: `feature/bugfix → dev → main`
```

#### Section: GitHub Branch Protection Settings
**Remove Entire Section:** "For `staging` branch" (lines 58-79)

**Keep:**
- Section for `dev` branch (lines 24-56)
- Section for `main` branch (lines 81-109)

#### Section: Workflow Summary (lines 111-155)
**Update subsection:** "Developer Workflow" (lines 113-133)

**Line 130-133 Current:**
```markdown
4. **After merge to `dev`**
   - Triggers `deploy-staging.yml`
   - Runs full E2E test suite
   - Automatically deploys to staging environment
```

**Update To:**
```markdown
4. **After merge to `dev`**
   - Triggers `deploy-dev.yml`
   - Runs full E2E test suite
   - Automatically deploys to dev environment
```

**Update subsection:** "Release Workflow" (lines 135-154)

**Remove lines 141-145:**
```markdown
2. **Create PR from `dev` to `staging`**
   - Requires 1 approval
   - Branch flow validation ensures PR is from `dev`
   - All staging tests must have passed
```

**Update lines 146-150:**
**Current:**
```markdown
3. **Create PR from `staging` to `main`**
   - Requires 2 approvals (including management)
   - Branch flow validation ensures PR is from `staging`
   - Triggers final production tests
```

**Update To:**
```markdown
2. **Create PR from `dev` to `main`**
   - Requires 2 approvals (including management)
   - Branch flow validation ensures PR is from `dev`
   - Triggers production readiness validation
```

**Update lines 151-154:**
**Current:**
```markdown
4. **After merge to `main`**
   - Triggers `deploy-production.yml`
   - Requires environment approval (production-approval gate)
   - Deploys to production
```

**Update To:**
```markdown
3. **After merge to `main`**
   - Triggers `deploy-production.yml`
   - Requires environment approval (production-approval gate)
   - Deploys to production
   - Runs post-deployment smoke tests
```

#### Section: Environment Protection Rules (lines 156-172)
**Remove lines 160-163:**
```markdown
**staging**
- No required reviewers (automated deployment)
- Can deploy from `dev` branch only
- Runs full E2E test suite automatically
```

**Update lines 165-168:**
**Current:**
```markdown
**production-approval**
- Required reviewers: Management team
- Wait timer: 0-5 minutes (optional)
- Can deploy from `main` branch only
```

**Keep As Is** (already correct)

#### Section: Automated Workflow Triggers (lines 174-183)
**Update table:**

**Current:**
```markdown
| Event | Workflow | Purpose | Tests Run |
|-------|----------|---------|--------------|
| Push to `feature/*` | feature-branch-tests.yml | Quick validation | Unit tests |
| PR to `dev` or `main` | pr-validation.yml | PR validation | Smoke tests (~2-5 min) |
| PR to `staging` or `main` | enforce-branch-flow.yml | Branch flow enforcement | N/A - validation only |
| Push to `dev` | deploy-staging.yml | Deploy to staging | Full E2E suite |
| Push to `staging` | N/A | No auto-deploy | Tests already passed |
| Push to `main` | deploy-production.yml | Deploy to production | Final smoke tests |
```

**Update To:**
```markdown
| Event | Workflow | Purpose | Tests Run |
|-------|----------|---------|--------------|
| Push to `feature/*` | feature-branch.yml | Quick validation | Unit tests |
| PR to `dev` | pr-to-dev.yml | PR validation | Integration + smoke E2E (~5-10 min) |
| Push to `dev` | deploy-dev.yml | Deploy to dev environment | Full E2E suite (~15-20 min) |
| PR to `main` | pr-to-main.yml | Production readiness | Integration + critical E2E (~10-15 min) |
| Push to `main` | deploy-production.yml | Deploy to production | Smoke tests + health checks (~2-5 min) |
```

#### Section: Testing Strategy (lines 185-206)
**Update subsection:** "Progressive Validation Approach" (lines 187-206)

**Lines 198-201 Current:**
```markdown
3. **Dev to Staging** (automatic on merge)
   - Full E2E test suite
   - Complete validation in staging environment
   - Real-world integration testing
```

**Update To:**
```markdown
3. **Dev Environment Deployment** (automatic on merge to dev)
   - Full E2E test suite
   - Complete validation in dev environment
   - Comprehensive pre-production testing
```

**Lines 203-206 Current:**
```markdown
4. **Staging to Production** (via main)
   - Tests already passed in staging
   - Final smoke tests for safety
   - Management approval required
```

**Update To:**
```markdown
4. **Production Deployment** (automatic on merge to main)
   - Tests already passed in dev
   - Post-deployment smoke tests for safety
   - Health checks and monitoring validation
```

---

## 2. Update docs/06-devops/development-workflow.md

**File:** `docs/06-devops/development-workflow.md`

### Changes Required

#### Section: Branch Types Table (lines 28-36)
**Remove line:**
```markdown
| `dev` | Integration branch | - | `main` |
```

**Should only have:**
```markdown
| Branch Pattern | Purpose | Base Branch | Merges To |
|----------------|---------|-------------|-----------|
| `feature/*` | New features | `dev` | `dev` |
| `bugfix/*` | Bug fixes | `dev` | `dev` |
| `hotfix/*` | Emergency fixes | `main` | `main` & `dev` |
| `dev` | Integration branch, deploys to dev environment | - | `main` |
| `main` | Production branch, deploys to production | - | - |
```

#### Section: Merging to Dev (lines 210-221)
**Lines 218-221 Current:**
```markdown
**After Merge**:
- Branch auto-deleted (if configured)
- Triggers `deploy-staging.yml`
- Auto-deploys to staging environment
```

**Update To:**
```markdown
**After Merge**:
- Branch auto-deleted (if configured)
- Triggers `deploy-dev.yml`
- Auto-deploys to dev environment
- Runs full E2E test suite
```

#### Section: Working with Staging (lines 223-245)
**Rename Section To:** "Working with Dev Environment"

**Lines 225-230 Current:**
```markdown
### Staging Deployment

After merging to `dev`:
1. Staging deployment starts automatically
2. Takes 5-10 minutes
3. Available at: `https://staging.your-app.com`
```

**Update To:**
```markdown
### Dev Environment Deployment

After merging to `dev`:
1. Dev environment deployment starts automatically
2. Takes 20-30 minutes (includes full E2E test suite)
3. Available at: `https://dev.your-app.com`
```

**Lines 232-238 Current:**
```markdown
### Testing in Staging

**Verification Steps**:
1. Test new features
2. Verify integrations
3. Check performance
4. Review with stakeholders
```

**Update To:**
```markdown
### Testing in Dev Environment

**Verification Steps**:
1. Automated full E2E test suite runs first
2. Manual testing of new features
3. Verify integrations
4. Check performance
5. Review with stakeholders before promoting to production
```

**Lines 240-245 Current:**
```markdown
### Reporting Issues

If issues found in staging:
1. Create bugfix branch from `dev`
2. Fix the issue
3. Follow standard PR process
```

**Update To:**
```markdown
### Reporting Issues

If issues found in dev environment:
1. Create bugfix branch from `dev`
2. Fix the issue
3. Follow standard PR process
4. After merge, deployment re-runs with full E2E suite
```

#### Section: Production Deployment (lines 247-281)
**Lines 249-260 Current:**
```markdown
### Creating Production PR

When staging is approved:

```bash
# Ensure dev is up to date
git checkout dev
git pull origin dev

# Create PR via GitHub UI
# Base: main ← Compare: dev
```

**Requirements**:
- Management approval (2 reviewers)
- Staging sign-off completed
- No critical issues
```

**Update To:**
```markdown
### Creating Production PR

When dev environment is validated:

```bash
# Ensure dev is up to date
git checkout dev
git pull origin dev

# Create PR via GitHub UI
# Base: main ← Compare: dev
```

**Requirements**:
- Management approval (2 reviewers)
- Dev environment testing completed and signed off
- Full E2E test suite passed
- No critical issues
```

---

## 3. Update CLAUDE.md

**File:** `CLAUDE.md`

### Changes Required

#### Section: Architecture Overview (near top of file)
**Add or update** section describing branch → environment mapping:

**Add after "Architecture" section:**
```markdown
## Branch and Deployment Strategy

This project uses a 3-stage pipeline:
- **Feature branches** → Development work, no deployment
- **dev branch** → Deploys to dev environment (dev.your-app.com)
- **main branch** → Deploys to production (your-app.com)

### Deployment Triggers
- Merge to `dev` → Triggers `deploy-dev.yml` → Deploys to dev environment → Runs full E2E suite
- Merge to `main` → Triggers `deploy-production.yml` → Deploys to production → Runs smoke tests

See @docs/03-Development/specs/2025-09-29-branch-deployment-strategy/ for complete specification.
```

#### Section: Testing (if exists)
**Update** to clarify when full E2E tests run:

```markdown
## Testing Strategy

- **Feature branches**: Unit tests only (~3-5 min)
- **PR to dev**: Integration + smoke E2E tests (~5-10 min)
- **Dev deployment**: Full E2E test suite (~15-20 min)
- **PR to main**: Production readiness validation (~10-15 min)
- **Production deployment**: Smoke tests + health checks (~2-5 min)
```

#### Section: Known Issues & Workarounds
**Remove** any references to staging confusion (if exists)

---

## 4. Implementation Checklist

### Documentation Update Order
1. [ ] Update `.github/BRANCH_PROTECTION_RULES.md` first (reference document)
2. [ ] Update `docs/06-devops/development-workflow.md` second (developer guide)
3. [ ] Update `CLAUDE.md` third (project overview)
4. [ ] Review all three files for any remaining "staging" references
5. [ ] Verify all links still work
6. [ ] Verify all file paths are correct

### Validation Steps
- [ ] Search all three files for the word "staging" - should only appear in historical context or as "staging environment" when referring to dev
- [ ] Verify branch flow diagrams/descriptions show: feature → dev → main
- [ ] Verify workflow trigger descriptions match actual workflow files
- [ ] Verify test execution descriptions match technical-spec.md

---

## 5. Content Consistency Rules

### Terminology Standards
- **dev branch** → always refers to the git branch named `dev`
- **dev environment** → always refers to the deployed environment (dev.your-app.com)
- **main branch** → always refers to the git branch named `main`
- **production environment** → always refers to the deployed production (your-app.com)
- **staging** → DO NOT USE unless referring to historical context

### Branch Flow Description
Always use: `feature → dev → main`
Never use: `feature → dev → staging → main`

### Workflow File Names
- `feature-branch.yml` (not feature-branch-tests.yml)
- `pr-to-dev.yml` (not pr-validation.yml)
- `deploy-dev.yml` (not deploy-staging.yml)
- `pr-to-main.yml` (correct)
- `deploy-production.yml` (correct)

### Test Suite Names
- **Smoke tests**: `test:smoke` (~2-3 min)
- **Critical tests**: `test:critical` (~5-10 min)
- **Full E2E suite**: `test:extended` (~15-20 min)

---

## 6. Review Checklist

Before considering documentation updates complete:

- [ ] All references to "staging branch" removed or clarified as "dev environment"
- [ ] All workflow file names match actual files
- [ ] All test durations are accurate
- [ ] Branch protection rules reflect 3-stage pipeline
- [ ] Environment protection rules updated
- [ ] No broken cross-references between documents
- [ ] Terminology is consistent across all three files
- [ ] No contradictions between documents
- [ ] All URLs reference correct environments (dev.your-app.com, your-app.com)