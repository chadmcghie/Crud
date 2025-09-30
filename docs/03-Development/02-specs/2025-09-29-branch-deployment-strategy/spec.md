# Spec Requirements Document

> Spec: 3-Stage Branch and Deployment Strategy
> Created: 2025-09-29
> GitHub Issue: #258 - Implement 3-Stage Branch and Deployment Strategy

## Overview

Establish a clear 3-stage CI/CD pipeline (feature → dev → main) with proper branch-to-environment naming alignment and progressive testing strategy appropriate for an early-stage project. This eliminates confusion between branch names and environment names while ensuring comprehensive validation before production deployment.

## User Stories

### Developer Workflow Clarity

As a developer, I want branch names to match environment names, so that I understand exactly where my code will be deployed when I merge.

**Workflow:**
1. Developer creates feature branch from `dev`
2. Push triggers unit tests and code quality checks (fast feedback)
3. Developer creates PR to `dev` branch
4. PR triggers integration tests and smoke E2E tests
5. After merge to `dev`, code automatically deploys to **dev environment** (not confusingly named "staging")
6. Full E2E test suite runs against deployed dev environment
7. After validation, developer creates PR from `dev` to `main`
8. PR triggers production readiness checks
9. After merge to `main`, code automatically deploys to **production environment**

**Problem Solved:** Eliminates confusion where `deploy-staging.yml` deploys from `dev` branch, and an unused `staging` branch exists serving no purpose.

### Testing Strategy Transparency

As a QA engineer, I want to understand what tests run at each stage of the pipeline, so that I know when comprehensive validation occurs and can plan manual testing accordingly.

**Workflow:**
1. Feature branches run only unit tests (fast feedback, 3-5 min)
2. PRs to dev run integration + smoke E2E tests (validation gate, 5-10 min)
3. Dev environment deployment runs **full E2E test suite** (comprehensive validation, 15-20 min)
4. PRs to main run production readiness checks (final gate, 10-15 min)
5. Production deployment runs post-deployment smoke tests (safety check, 2-3 min)

**Problem Solved:** Currently, dev deployment only runs smoke tests, leaving gaps in pre-production validation.

### Documentation Accuracy

As a team member, I want documentation to accurately reflect the actual branch and deployment strategy, so that I can follow correct procedures and understand the CI/CD pipeline.

**Workflow:**
1. Review `.github/BRANCH_PROTECTION_RULES.md` and see accurate 3-stage flow
2. Review `docs/06-devops/development-workflow.md` and see correct branch → environment mapping
3. Review `CLAUDE.md` and see correct deployment triggers
4. No references to unused `staging` branch causing confusion

**Problem Solved:** Current documentation references a 4-stage flow with staging branch that doesn't match actual implementation.

## Spec Scope

1. **Rename Workflow File** - Rename `deploy-staging.yml` to `deploy-dev.yml` and update all internal references from "staging" to "dev"

2. **Upgrade Dev Testing** - Change dev deployment test command from `npm run test:smoke` to `npm run test:extended` for comprehensive E2E validation

3. **Create Production Workflow** - Create new `deploy-production.yml` workflow that triggers on push to `main` branch with appropriate production deployment and validation

4. **Remove Unused Branch** - Delete the `staging` branch from repository (both local and remote) as it serves no purpose in the 3-stage model

5. **Update Documentation** - Update `.github/BRANCH_PROTECTION_RULES.md`, `docs/06-devops/development-workflow.md`, and `CLAUDE.md` to reflect accurate 3-stage pipeline

## Out of Scope

- Implementing actual deployment steps (placeholders remain until hosting is configured)
- Creating a 4-stage pipeline with separate staging environment
- Setting up monitoring and alerting systems (placeholder references remain)
- Implementing blue-green deployment (mentioned in docs but not required for initial implementation)
- Configuring GitHub branch protection rules (documented but manual setup)

## Expected Deliverable

1. **Clear Workflow Files** - `deploy-dev.yml` and `deploy-production.yml` exist with proper naming, and no references to "staging" in dev workflow

2. **Comprehensive Dev Testing** - Dev environment deployment runs full E2E test suite (test:extended) taking 15-20 minutes, providing thorough pre-production validation

3. **Accurate Documentation** - All three documentation files (BRANCH_PROTECTION_RULES.md, development-workflow.md, CLAUDE.md) accurately describe the 3-stage pipeline with correct branch → environment mappings

4. **Clean Repository** - Staging branch no longer exists in the repository, eliminating confusion about branch structure