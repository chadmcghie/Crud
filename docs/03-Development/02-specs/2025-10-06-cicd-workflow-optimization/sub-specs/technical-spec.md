# Technical Specification

This is the technical specification for the spec detailed in @docs/03-Development/specs/2025-10-06-cicd-workflow-optimization/spec.md

## Technical Requirements

### 1. Dependabot Workflow Architecture

- **New Workflow File**: `.github/workflows/dependabot-ci.yml`
- **Trigger Condition**: `pull_request` event to `dev` and `main` branches with actor check `github.actor == 'dependabot[bot]'`
- **Path Detection**: Use `dorny/paths-filter@v3` action to detect dependency type changes:
  - `.csproj`, `packages.config`, `global.json` → .NET dependencies
  - `src/Angular/package*.json` → Angular dependencies
  - `test/Tests.E2E.NG/package*.json` → E2E test dependencies
  - `.github/workflows/**/*.yml` → GitHub Actions dependencies
- **Conditional Jobs**: Execute only jobs relevant to detected dependency type
- **Test Strategy**: Run only unit tests (skip integration tests, skip code formatting, skip CodeQL)
- **Target Execution Time**: 2-3 minutes total

### 2. Path Filtering for Human PRs

- **Implementation Location**: `.github/workflows/feature-branch.yml`
- **New Job**: `detect-changes` using `dorny/paths-filter@v3`
- **Filter Patterns**:
  ```yaml
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
- **Job Conditions**:
  - `backend-unit-tests`: `if: needs.detect-changes.outputs.backend == 'true'`
  - `frontend-unit-tests`: `if: needs.detect-changes.outputs.frontend == 'true'`
  - `build-codeql-artifacts`: Conditional matrix based on detected changes
- **Summary Updates**: Display skipped vs executed tests in summary job

### 3. Playwright Browser Caching

- **Cache Implementation**: `actions/cache@v4` action
- **Cache Path**: `~/.cache/ms-playwright` (Linux/macOS GitHub runners)
- **Cache Key**: `playwright-${{ runner.os }}-${{ hashFiles('test/Tests.E2E.NG/package-lock.json') }}`
- **Restore Keys**: `playwright-${{ runner.os }}-` (partial match fallback)
- **Install Command**: `npx playwright install chromium` (remove `--with-deps` flag)
- **Performance Impact**:
  - Cache hit: ~10-30 seconds
  - Cache miss (first run): ~3-4 minutes (down from 9 minutes)
  - Expected cache hit rate: 90%+ (Playwright version changes infrequently)
- **Workflow Files to Modify**:
  - `.github/workflows/pr-to-dev.yml`
  - `.github/workflows/pr-to-main.yml`
  - `.github/workflows/deploy-dev.yml`
  - `.github/workflows/deploy-production.yml`

### 4. Conditional Workflow Execution

- **pr-to-dev.yml Updates**:
  - Add `check-actor` job to detect Dependabot PRs
  - Skip `wait-for-feature-workflow` job if `github.actor == 'dependabot[bot]'`
  - Update `integration-tests` condition to run for Dependabot OR completed feature workflow
  - Update `smoke-tests` condition similarly
- **Dependency Chain**:
  - Human PR: `wait-for-feature-workflow` → `integration-tests` + `smoke-tests`
  - Dependabot PR: Skip wait → immediate `integration-tests` + `smoke-tests`

### 5. Build Matrix Optimization

- **Current**: Always builds both C# and JavaScript in CodeQL matrix
- **Optimized**: Conditional matrix based on path detection
  ```yaml
  matrix:
    language: ${{
      (backend && frontend && ['csharp', 'javascript']) ||
      (backend && ['csharp']) ||
      (frontend && ['javascript']) ||
      ['csharp', 'javascript']
    }}
  ```
- **Performance Impact**: Saves ~2-3 minutes when only one language changed

### 6. Summary Job Enhancements

- **feature-branch.yml Summary Updates**:
  - Display detected changes (backend/frontend)
  - Show skipped vs executed tests
  - Clear indication of path-filtered results
- **dependabot-ci.yml Summary**:
  - Display validated dependency types
  - Show unit test results per dependency type
  - Link to full integration tests in pr-to-dev.yml

## Performance Targets

| Scenario | Current Time | Target Time | Improvement |
|----------|--------------|-------------|-------------|
| Dependabot PR | 0 min (no tests) | 6-7 min | ∞% (now runs tests) |
| Frontend-only PR | 23 min | 7-8 min | 66% faster |
| Backend-only PR | 23 min | 14-15 min | 35% faster |
| Full changes PR | 23 min | 16 min | 30% faster |
| Playwright install (cached) | 9 min | 30 sec | 85% faster |

## Integration Requirements

- **No Breaking Changes**: All modifications maintain backward compatibility
- **Existing Orchestration Preserved**: Code-quality → Build → Tests → Summary pipeline unchanged
- **Branch Protection Compatible**: New checks integrate with existing required status checks
- **Cache Invalidation**: Automatic via package-lock.json hash changes
- **Fallback Behavior**: Path filter defaults to running all tests if detection fails
