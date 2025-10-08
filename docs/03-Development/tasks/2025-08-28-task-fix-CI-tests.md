# Fix Angular Startup Timeout in E2E Tests

## Problem Analysis
The Angular server fails to start in the GitHub Actions E2E test workflow, causing a timeout at step "Wait for Angular to be ready" (line 270-273 of `.github/workflows/pr-validation.yml`). The root cause is that the Angular start script references a missing `proxy.conf.json` file.

## Tasks

- [x] 1. Fix Angular proxy configuration
  - [x] 1.1 Create proxy.conf.json file in src/Angular directory
  - [x] 1.2 Configure proxy to forward API requests to http://localhost:5172
  - [x] 1.3 Test local Angular startup with proxy configuration
  - [x] 1.4 Verify proxy settings work in CI environment

- [x] 2. Update Angular start command for CI
  - [x] 2.1 Modify package.json to add CI-specific start script without proxy
  - [x] 2.2 Update workflow to use CI start script
  - [x] 2.3 Test Angular startup without proxy in CI environment
  - [x] 2.4 Verify E2E tests can connect to both Angular and API

- [x] 3. Add error handling and debugging to workflow
  - [x] 3.1 Add Angular build output capture in workflow
  - [x] 3.2 Add error logs for Angular startup failures
  - [x] 3.3 Implement process health checks before running E2E tests
  - [x] 3.4 Verify all error scenarios are properly logged

## Recommended Solution

The most robust solution is to:
1. Create the missing proxy.conf.json for local development
2. Add a CI-specific start command that doesn't require proxy (since API runs separately in CI)
3. Update the workflow to use the CI start command

## Implementation Priority
Start with Task 2 as it directly fixes the CI/CD pipeline without affecting local development.