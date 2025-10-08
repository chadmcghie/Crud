# Spec Requirements Document

> Spec: CI/CD Workflow Optimization - Dependabot Support & Performance
> Created: 2025-10-06
> GitHub Issue: #276 - CI/CD Workflow Optimization - Dependabot Support & Performance

## Overview

Optimize GitHub Actions workflows to enable Dependabot PRs to run automated tests, implement intelligent path filtering to skip irrelevant tests, and add Playwright browser caching to reduce E2E test execution time from 23 minutes to under 8 minutes for most changes.

## User Stories

### Automated Dependency Testing

As a developer, I want Dependabot PRs to automatically run appropriate tests, so that I can confidently merge dependency updates without manual testing.

When Dependabot creates a PR for a dependency update (npm, NuGet, or GitHub Actions), the CI/CD pipeline automatically detects the dependency type, runs relevant unit tests, and validates the update doesn't break existing functionality. This eliminates the current gap where Dependabot PRs bypass all automated testing.

### Fast Feedback on Focused Changes

As a developer, I want the CI/CD pipeline to only run tests relevant to my changes, so that I get faster feedback and can iterate more quickly.

When I create a PR with only frontend changes, the pipeline skips backend unit tests, CodeQL analysis for C#, and backend builds. This reduces wait time from 23 minutes to 7-8 minutes. Similarly, backend-only changes skip frontend testing, providing faster validation focused on what actually changed.

### Efficient E2E Testing

As a developer, I want E2E tests to use cached Playwright browsers, so that test execution is faster and more reliable.

When E2E tests run, Playwright browsers are restored from cache in 30 seconds instead of downloading and installing for 9 minutes on every run. The cache is invalidated only when Playwright version changes in package-lock.json, ensuring 90%+ cache hit rate.

## Spec Scope

1. **Dedicated Dependabot Workflow** - Create `dependabot-ci.yml` that detects dependency type and runs targeted unit tests in 2-3 minutes
2. **Path-Based Test Filtering** - Add `dorny/paths-filter` to `feature-branch.yml` to conditionally skip backend or frontend tests based on changed files
3. **Playwright Browser Caching** - Implement `actions/cache@v4` for `~/.cache/ms-playwright` across all E2E workflows (pr-to-dev, pr-to-main, deploy-dev, deploy-production)
4. **Conditional Workflow Execution** - Add actor-based logic to skip duplicate work when Dependabot workflows handle testing
5. **Performance Optimization** - Remove `--with-deps` flag from Playwright install to reduce first-run time from 9 to 3-4 minutes

## Out of Scope

- Rewriting existing test suites or test commands
- Changing the orchestrated workflow architecture (code-quality → build → tests → summary)
- Splitting workflows into separate files (maintaining current structure)
- Using Docker containers for browser management
- Implementing custom Playwright Docker images
- Modifying branch protection to use GitHub Actions path filtering (using third-party action instead)

## Expected Deliverable

1. **Dependabot PRs run automated tests** - All Dependabot PRs (npm, NuGet, GitHub Actions) trigger appropriate test workflows and complete in 6-7 minutes
2. **Frontend-only changes complete in under 8 minutes** - PRs with only `src/Angular/**` changes skip backend tests and finish in 7-8 minutes (vs 23 min currently)
3. **Backend-only changes complete in under 16 minutes** - PRs with only backend changes skip frontend tests and finish in 14-15 minutes (vs 23 min currently)
4. **Playwright browser caching achieves 90%+ hit rate** - E2E workflows restore browsers from cache in 30 seconds for 90%+ of runs (vs 9 min download every time)
5. **All existing test coverage maintained** - No reduction in test coverage, all tests still run when relevant files change
