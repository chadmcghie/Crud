# Workflow Specification

This is the workflow implementation specification for the spec detailed in @docs/03-Development/specs/2025-10-06-cicd-workflow-optimization/spec.md

## Workflow Files Overview

| File | Status | Changes Required | Lines Modified |
|------|--------|------------------|----------------|
| `dependabot-ci.yml` | **NEW** | Create complete workflow | ~250 lines |
| `feature-branch.yml` | **MODIFY** | Add path filtering | ~74 lines |
| `pr-to-dev.yml` | **MODIFY** | Add caching + actor check | ~29 lines |
| `pr-to-main.yml` | **MODIFY** | Add caching only | ~9 lines |
| `deploy-dev.yml` | **MODIFY** | Add caching only | ~9 lines |
| `deploy-production.yml` | **MODIFY** | Add caching only | ~9 lines |

## 1. New Workflow: dependabot-ci.yml

### Purpose
Fast, targeted validation for Dependabot dependency updates without running full pipeline (formatting, CodeQL, integration tests).

### Trigger Configuration
```yaml
on:
  pull_request:
    branches: [dev, main]
```

### Job Structure

#### Job 1: check-actor
- **Purpose**: Gate to ensure workflow only runs for Dependabot
- **Output**: `is-dependabot` boolean
- **Logic**: `github.actor == 'dependabot[bot]'`

#### Job 2: detect-dependency-type
- **Needs**: `check-actor`
- **Condition**: `needs.check-actor.outputs.is-dependabot == 'true'`
- **Action**: `dorny/paths-filter@v3`
- **Outputs**:
  - `is-dotnet`: Checks `**/*.csproj`, `**/packages.config`, `global.json`
  - `is-angular`: Checks `src/Angular/package*.json`
  - `is-e2e`: Checks `test/Tests.E2E.NG/package*.json`
  - `is-github-actions`: Checks `.github/workflows/**/*.yml`

#### Job 3: dotnet-dependency-check
- **Condition**: `needs.detect-dependency-type.outputs.is-dotnet == 'true'`
- **Steps**:
  1. Checkout code
  2. Setup .NET with caching
  3. Restore dependencies (verify resolution)
  4. Run unit tests only: `dotnet test test/Tests.Unit.Backend`

#### Job 4: angular-dependency-check
- **Condition**: `needs.detect-dependency-type.outputs.is-angular == 'true'`
- **Steps**:
  1. Checkout code
  2. Setup Node.js with npm caching
  3. Install dependencies: `npm ci`
  4. Run unit tests: `npm test -- --watch=false --browsers=ChromeHeadless`

#### Job 5: e2e-dependency-check
- **Condition**: `needs.detect-dependency-type.outputs.is-e2e == 'true'`
- **Steps**:
  1. Checkout code
  2. Setup Node.js with npm caching
  3. Add Playwright browser caching (see Caching Strategy below)
  4. Install dependencies: `npm ci`
  5. Install Playwright: `npx playwright install chromium`
  6. Verify installation: `npx playwright --version`

#### Job 6: github-actions-check
- **Condition**: `needs.detect-dependency-type.outputs.is-github-actions == 'true'`
- **Steps**:
  1. Checkout code (validates workflow syntax automatically)
  2. Output validation confirmation

#### Job 7: dependabot-summary
- **Needs**: All check jobs
- **Condition**: `always() && needs.check-actor.outputs.is-dependabot == 'true'`
- **Steps**:
  1. Create GitHub Step Summary table showing each dependency type status
  2. Exit with error code 1 if any check failed

## 2. Enhanced Workflow: feature-branch.yml

### New Job: detect-changes

Add as first job in workflow:

```yaml
jobs:
  detect-changes:
    runs-on: ubuntu-latest
    outputs:
      backend: ${{ steps.filter.outputs.backend }}
      frontend: ${{ steps.filter.outputs.frontend }}
    steps:
      - uses: actions/checkout@v4
      - uses: dorny/paths-filter@v3
        id: filter
        with:
          filters: |
            backend:
              - 'src/Api/**'
              - 'src/App/**'
              - 'src/Domain/**'
              - 'src/Infrastructure/**'
              - 'test/Tests.Unit.Backend/**'
              - 'test/Tests.Integration.Backend/**'
            frontend:
              - 'src/Angular/**'
              - 'test/Tests.Integration.NG/**'
```

### Modified Job: build-codeql-artifacts

**Update dependencies**: `needs: [code-quality, detect-changes]`

**Update matrix strategy**:
```yaml
strategy:
  matrix:
    language: ${{
      (needs.detect-changes.outputs.backend == 'true' && needs.detect-changes.outputs.frontend == 'true' && fromJSON('["csharp", "javascript"]')) ||
      (needs.detect-changes.outputs.backend == 'true' && fromJSON('["csharp"]')) ||
      (needs.detect-changes.outputs.frontend == 'true' && fromJSON('["javascript"]')) ||
      fromJSON('["csharp", "javascript"]')
    }}
```

### Modified Job: backend-unit-tests

**Update dependencies**: `needs: [build-codeql-artifacts, detect-changes]`

**Add condition**: `if: needs.detect-changes.outputs.backend == 'true'`

### Modified Job: frontend-unit-tests

**Update dependencies**: `needs: [build-codeql-artifacts, detect-changes]`

**Add condition**: `if: needs.detect-changes.outputs.frontend == 'true'`

### Modified Job: feature-branch-summary

**Update dependencies**: Add `detect-changes` to needs array

**Update summary logic**:
- Show detected changes (backend/frontend)
- Display skipped tests with "⏭️ Skipped" status
- Only fail if executed tests failed (not skipped tests)

## 3. Optimized Workflow: pr-to-dev.yml

### New Job: check-actor

Add as first job:

```yaml
check-actor:
  runs-on: ubuntu-latest
  outputs:
    is-dependabot: ${{ steps.check.outputs.is-dependabot }}
  steps:
    - id: check
      run: |
        if [[ "${{ github.actor }}" == "dependabot[bot]" ]]; then
          echo "is-dependabot=true" >> $GITHUB_OUTPUT
        else
          echo "is-dependabot=false" >> $GITHUB_OUTPUT
        fi
```

### Modified Job: wait-for-feature-workflow

**Update dependencies**: `needs: check-actor`

**Add condition**: `if: needs.check-actor.outputs.is-dependabot == 'false'`

### Modified Job: integration-tests

**Update dependencies**: `needs: [check-actor, wait-for-feature-workflow]`

**Update condition**:
```yaml
if: |
  (needs.check-actor.outputs.is-dependabot == 'false' && needs.wait-for-feature-workflow.outputs.should-run == 'true') ||
  (needs.check-actor.outputs.is-dependabot == 'true')
```

### Modified Job: smoke-tests

**Update dependencies**: `needs: [check-actor, wait-for-feature-workflow]`

**Update condition**: Same as integration-tests

**Add Playwright caching** (before install step):
```yaml
- name: Cache Playwright browsers
  uses: actions/cache@v4
  with:
    path: ~/.cache/ms-playwright
    key: playwright-${{ runner.os }}-${{ hashFiles('test/Tests.E2E.NG/package-lock.json') }}
    restore-keys: |
      playwright-${{ runner.os }}-

- name: Install Playwright browsers
  working-directory: ./test/Tests.E2E.NG
  run: npx playwright install chromium  # Remove --with-deps
```

## 4. Caching Strategy (All E2E Workflows)

### Files to Update
- `pr-to-dev.yml`
- `pr-to-main.yml`
- `deploy-dev.yml`
- `deploy-production.yml`

### Cache Configuration

**Add before Playwright install step**:
```yaml
- name: Cache Playwright browsers
  uses: actions/cache@v4
  with:
    path: ~/.cache/ms-playwright
    key: playwright-${{ runner.os }}-${{ hashFiles('test/Tests.E2E.NG/package-lock.json') }}
    restore-keys: |
      playwright-${{ runner.os }}-
```

**Modify install step**:
```yaml
- name: Install Playwright browsers
  working-directory: ./test/Tests.E2E.NG
  run: |
    echo "🎭 Installing Playwright browsers..."
    npx playwright install chromium  # ← REMOVE --with-deps flag
    echo "✅ Playwright browsers installed"
```

### Cache Behavior

- **Cache Key**: Unique per `package-lock.json` hash (invalidates when Playwright version changes)
- **Restore Keys**: Allows partial match if exact key not found
- **Cache Hit**: Restores browsers in ~10-30 seconds
- **Cache Miss**: Downloads/installs in ~3-4 minutes (vs 9 min with --with-deps)
- **Cache Location**: `~/.cache/ms-playwright` (GitHub runner default)

## Branch Protection Updates

### For `dev` Branch

**Required Status Checks**:
- `Dependabot CI / dependabot-summary` (runs only for Dependabot PRs)
- `Feature Branch / feature-branch-summary` (runs only for human PRs)
- `PR to Dev - Integration & Smoke Tests / integration-tests` (runs for all PRs)
- `PR to Dev - Integration & Smoke Tests / smoke-tests` (runs for all PRs)

**Note**: GitHub automatically handles checks that don't run (they don't appear and aren't required)

### For `main` Branch

**No Changes Required** - Existing checks remain valid

## Implementation Phases

### Phase 1: Playwright Caching (Quick Win - 30 min)
1. Add cache steps to pr-to-dev.yml
2. Add cache steps to pr-to-main.yml
3. Add cache steps to deploy-dev.yml
4. Add cache steps to deploy-production.yml
5. Remove --with-deps from all install commands

### Phase 2: Dependabot Workflow (2 hours)
1. Create dependabot-ci.yml
2. Add check-actor to pr-to-dev.yml
3. Update wait-for-feature-workflow condition
4. Update integration-tests and smoke-tests conditions

### Phase 3: Path Filtering (2-3 hours)
1. Add detect-changes job to feature-branch.yml
2. Update build-codeql-artifacts matrix
3. Add conditions to backend-unit-tests and frontend-unit-tests
4. Update feature-branch-summary logic

### Phase 4: Branch Protection (15 min)
1. Update dev branch protection rules
2. Verify status checks configuration

## Testing Validation

### Dependabot PR Test
1. Trigger Dependabot rebase: `@dependabot rebase`
2. Verify dependabot-ci.yml runs
3. Verify unit tests execute
4. Verify pr-to-dev.yml skips wait-for-feature-workflow
5. Confirm total time ~6-7 minutes

### Path Filtering Test
1. **Frontend-only**: Change only `src/Angular/**` files
   - Verify backend tests show "Skipped"
   - Confirm time ~1-2 min
2. **Backend-only**: Change only `src/Api/**` files
   - Verify frontend tests show "Skipped"
   - Confirm time ~8-9 min
3. **Mixed**: Change both stacks
   - Verify all tests run
   - Confirm time ~10 min

### Caching Test
1. First PR: Check logs for "Playwright browsers installed" (~3-4 min)
2. Second PR: Check logs for "Cache restored" (~30 sec)
3. Change package-lock.json: Verify cache miss and rebuild
