# Branch Protection Rules Configuration

This document outlines the recommended branch protection rules for the CI/CD pipeline.

## Branch Structure

- **`dev`** - Default branch, integration branch for features
- **`main`** - Production branch, requires management approval
- **`feature/*`** - Feature development branches
- **`bugfix/*`** - Bug fix branches  
- **`hotfix/*`** - Emergency production fixes

## GitHub Branch Protection Settings

### For `dev` branch:

1. Go to Settings → Branches → Add rule
2. Branch name pattern: `dev`
3. Configure these settings:

**Protect matching branches**
- ✅ Require a pull request before merging
  - ✅ Require approvals: **1**
  - ✅ Dismiss stale pull request approvals when new commits are pushed
  - ✅ Require review from CODEOWNERS (if applicable)
  
**Require status checks to pass before merging**
- ✅ Require branches to be up to date before merging
- Select these required status checks:
  - `PR Validation Summary`
  - `Backend Unit Tests`
  - `Frontend Unit Tests`
  - `Backend Integration Tests`
  - `End-to-End Tests`

**Additional settings**
- ✅ Require conversation resolution before merging
- ✅ Require signed commits (optional, for enhanced security)
- ✅ Include administrators
- ✅ Allow force pushes → **Disabled**
- ✅ Allow deletions → **Disabled**

### For `main` branch:

1. Go to Settings → Branches → Add rule
2. Branch name pattern: `main`
3. Configure these settings:

**Protect matching branches**
- ✅ Require a pull request before merging
  - ✅ Require approvals: **2** (including management)
  - ✅ Dismiss stale pull request approvals when new commits are pushed
  - ✅ Restrict who can approve (add management team)
  
**Require status checks to pass before merging**
- ✅ Require branches to be up to date before merging
- Select these required status checks:
  - `Smoke Tests`
  
**Restrict who can push to matching branches**
- Add specific users/teams who can merge to production
- Typically: Lead developers, DevOps team, Management

**Additional settings**
- ✅ Require conversation resolution before merging
- ✅ Require signed commits (recommended for production)
- ✅ Include administrators
- ✅ Allow force pushes → **Disabled**
- ✅ Allow deletions → **Disabled**
- ✅ Lock branch (optional, for critical periods)

## Workflow Summary

### Developer Workflow

1. **Create feature branch from `dev`**
   ```bash
   git checkout dev
   git pull origin dev
   git checkout -b feature/your-feature-name
   ```

2. **Develop and commit changes**
   - Push commits triggers `feature-branch-tests.yml` (quick tests)
   
3. **Create PR to `dev`**
   - Triggers `pr-validation.yml` (full test suite)
   - Requires 1 approval
   - All tests must pass

4. **After merge to `dev`**
   - Triggers `deploy-staging.yml`
   - Automatically deploys to staging environment

### Release Workflow

1. **Management reviews staging**
   - Test features in staging environment
   - Approve for production

2. **Create PR from `dev` to `main`**
   - Requires 2 approvals (including management)
   - Triggers smoke tests

3. **After merge to `main`**
   - Triggers `deploy-production.yml`
   - Requires environment approval
   - Deploys to production

## Environment Protection Rules

### In GitHub Settings → Environments:

**staging**
- No required reviewers
- Can deploy from `dev` branch only

**production-approval**
- Required reviewers: Management team
- Wait timer: 0-5 minutes (optional)
- Can deploy from `main` branch only

**production**
- Required reviewers: DevOps team
- Can deploy from `main` branch only

## Automated Workflow Triggers

| Event | Workflow | Purpose |
|-------|----------|---------|
| Push to `feature/*` | feature-branch-tests.yml | Quick validation |
| PR to `dev` | pr-validation.yml | Full test suite |
| Push to `dev` | deploy-staging.yml | Deploy to staging |
| PR to `main` | Smoke tests in deploy-production.yml | Final validation |
| Push to `main` | deploy-production.yml | Deploy to production |

## Security Considerations

1. **Secrets Management**
   - Store deployment credentials in GitHub Secrets
   - Use environment-specific secrets
   - Rotate credentials regularly

2. **Code Signing**
   - Consider requiring signed commits for `main`
   - Use GPG keys for commit verification

3. **Audit Trail**
   - All deployments are logged in GitHub Actions
   - Production deployments create GitHub Releases
   - Consider integrating with external audit systems

## Rollback Procedures

1. **Staging Rollback**
   - Re-run previous successful staging deployment
   - Or push fix to `dev` branch

2. **Production Rollback**
   - Use GitHub Actions to re-run previous production deployment
   - Or create hotfix branch from `main`, fix, and fast-track through pipeline

## Monitoring and Alerts

Configure these notifications:
- Deployment failures → DevOps team
- Production deployments → All stakeholders
- Test failures on `dev` → Development team
- Security scan failures → Security team

## Next Steps

1. Configure branch protection rules in GitHub
2. Set up environment protection rules
3. Add deployment credentials to GitHub Secrets
4. Update deployment steps in workflows with actual deployment commands
5. Configure notification webhooks (Slack, Teams, etc.)
6. Test the complete pipeline with a sample feature