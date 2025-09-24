# ADR-0005: CI/CD Dependency Management Strategy

## Status
Accepted

## Context
Our staging smoke tests were failing with timeout errors. Initial assumptions were:
- Timeout values too short
- Performance issues in CI
- Server startup problems

After investigation (prompted by "did you read the log or are you just guessing?"), the actual issue was that Angular dependencies were never installed in the staging workflow, preventing the Angular dev server from starting.

This revealed a broader issue: inconsistent dependency management across different CI/CD workflows.

## Decision
Every CI/CD job that runs tests requiring application servers must explicitly install all necessary dependencies, even if using Playwright's webServer configuration.

Specifically:
1. Backend tests need .NET dependencies (`dotnet restore`)
2. Frontend tests need Angular dependencies (`npm ci` in `src/Angular`)
3. E2E tests need both application dependencies AND test dependencies

## Rationale
1. **Explicit is better than implicit**: Don't assume dependencies exist
2. **Workflow independence**: Each job should be self-contained
3. **Debugging clarity**: Missing dependencies should fail fast with clear errors
4. **Avoid hidden failures**: Playwright's `stdout: 'ignore'` can hide startup failures

## Implementation

### For E2E Tests in CI
```yaml
# Setup Node.js with caching
- name: Setup Node.js
  uses: actions/setup-node@v4
  with:
    node-version: ${{ env.NODE_VERSION }}
    cache: 'npm'
    cache-dependency-path: 'src/Angular/package-lock.json'

# CRITICAL: Install Angular dependencies
- name: Install Angular dependencies
  working-directory: ./src/Angular
  run: npm ci

# Install E2E test dependencies
- name: Install E2E dependencies
  working-directory: ./test/Tests.E2E.NG
  run: npm ci
```

### For Debugging
Consider removing `stdout: 'ignore'` temporarily when debugging:
```typescript
webServer: {
  command: 'npm start',
  stdout: process.env.DEBUG ? 'pipe' : 'ignore', // Show output when debugging
  stderr: process.env.DEBUG ? 'pipe' : 'ignore',
}
```

## Consequences

### Positive
- Failures are obvious and immediate (missing deps = build failure)
- Consistent behavior across all workflows
- Easier to debug CI failures
- No mysterious timeout errors from missing servers

### Negative
- Slightly longer CI execution time (installing deps)
- More verbose workflow files
- Duplicate dependency installation across jobs

### Neutral
- May benefit from caching strategies to minimize impact
- Could explore artifact sharing between jobs

## Alternatives Considered

### 1. Rely on Playwright webServer to handle everything
- **Rejected**: webServer can't install missing dependencies
- **Learning**: webServer assumes deps exist, it just starts servers

### 2. Share node_modules via artifacts
- **Rejected**: Complex, can cause version conflicts
- **Note**: Could revisit for optimization if CI time becomes issue

### 3. Pre-built Docker images with deps
- **Rejected**: Maintenance overhead, version drift risk
- **Note**: Valid for stable, long-term projects

### 4. Assume previous jobs installed deps
- **Rejected**: Creates hidden job dependencies
- **Learning**: This was our mistake - assuming deps from build job

## Investigation Process
This decision came from a critical lesson: **read the logs, don't guess**.

### What we assumed:
- Timeout issue → increase timeouts
- Performance issue → optimize tests
- Server startup issue → fix server config

### What actually happened:
1. Read the workflow file
2. Noticed `npm ci` was only run for E2E tests, not Angular
3. Realized Playwright couldn't start Angular without node_modules
4. Found PR workflow had the step, staging workflow didn't

## Related
- Issue: Staging smoke test failures (GitHub Actions run #17572689978)
- File: `.github/workflows/deploy-staging.yml`
- Fix: Added Angular dependency installation (commit d84c954)

## Notes
This pattern should be checked in all workflows:
- `pr-validation.yml` ✓ (has Angular deps)
- `deploy-staging.yml` ✓ (fixed)
- `deploy-production.yml` (needs review)
- Any future test workflows

Consider adding a workflow template or shared action to standardize dependency setup.