# Branch Protection Rules Configuration

This document outlines the recommended branch protection rules for the CI/CD pipeline.

## Branch Structure

- **`dev`** - Default branch, integration branch for features
- **`staging`** - Pre-production branch, full E2E testing environment
- **`main`** - Production branch, requires management approval
- **`feature/*`** - Feature development branches
- **`bugfix/*`** - Bug fix branches  
- **`hotfix/*`** - Emergency production fixes

## Branch Flow

The strict branch flow is: `feature/bugfix → dev → staging → main`

- Direct commits to protected branches are **prohibited**
- All changes must go through pull requests
- Branch flow is enforced by `enforce-branch-flow.yml` workflow

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

### For `staging` branch:

1. Go to Settings → Branches → Add rule
2. Branch name pattern: `staging`
3. Configure these settings:

**Protect matching branches**
- ✅ Require a pull request before merging
  - ✅ Require approvals: **1**
  - ✅ Dismiss stale pull request approvals when new commits are pushed
  
**Require status checks to pass before merging**
- ✅ Require branches to be up to date before merging
- Select these required status checks:
  - `PR Validation Summary`
  - `Validate PR Source Branch` (from enforce-branch-flow.yml)

**Additional settings**
- ✅ Require conversation resolution before merging
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
  - `Full Test Suite Before Production` (from deploy-production.yml)
  - `Validate PR Source Branch` (from enforce-branch-flow.yml)
  
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
   - Triggers `pr-validation.yml` (smoke tests for quick feedback)
   - Requires 1 approval
   - All smoke tests must pass

4. **After merge to `dev`**
   - Triggers `deploy-staging.yml`
   - Runs full E2E test suite
   - Automatically deploys to staging environment

### Release Workflow

1. **Staging validation**
   - Full E2E tests run automatically in staging
   - Team validates features in staging environment
   
2. **Create PR from `dev` to `staging`**
   - Requires 1 approval
   - Branch flow validation ensures PR is from `dev`
   - All staging tests must have passed

3. **Create PR from `staging` to `main`**
   - Requires 2 approvals (including management)
   - Branch flow validation ensures PR is from `staging`
   - Triggers final production tests

4. **After merge to `main`**
   - Triggers `deploy-production.yml`
   - Requires environment approval (production-approval gate)
   - Deploys to production

## Environment Protection Rules

### In GitHub Settings → Environments:

**staging**
- No required reviewers (automated deployment)
- Can deploy from `dev` branch only
- Runs full E2E test suite automatically

**production-approval**
- Required reviewers: Management team
- Wait timer: 0-5 minutes (optional)
- Can deploy from `main` branch only

**production**
- Required reviewers: DevOps team
- Can deploy from `main` branch only

## Automated Workflow Triggers

| Event | Workflow | Purpose | Tests Run |
|-------|----------|---------|-----------|
| Push to `feature/*` | feature-branch-tests.yml | Quick validation | Unit tests |
| PR to `dev` or `main` | pr-validation.yml | PR validation | Smoke tests (~2-5 min) |
| PR to `staging` or `main` | enforce-branch-flow.yml | Branch flow enforcement | N/A - validation only |
| Push to `dev` | deploy-staging.yml | Deploy to staging | Full E2E suite |
| Push to `staging` | N/A | No auto-deploy | Tests already passed |
| Push to `main` | deploy-production.yml | Deploy to production | Final smoke tests |

## Testing Strategy

### Progressive Validation Approach

1. **Feature Development** (feature branches)
   - Quick unit tests on push
   - Developer gets fast feedback

2. **Pull Request to Dev** 
   - Smoke tests only (~2-5 minutes)
   - Fast PR validation
   - Prevents broken code from entering dev

3. **Dev to Staging** (automatic on merge)
   - Full E2E test suite
   - Complete validation in staging environment
   - Real-world integration testing

4. **Staging to Production** (via main)
   - Tests already passed in staging
   - Final smoke tests for safety
   - Management approval required

## Security Considerations

### 1. Secrets Management
- Store deployment credentials in GitHub Secrets
- Use environment-specific secrets
- Rotate credentials regularly

### 2. GPG Commit Signing

**Setting up GPG signing:**

```bash
# 1. Generate a GPG key (if you don't have one)
gpg --full-generate-key
# Choose: (1) RSA and RSA, 4096 bits, key doesn't expire

# 2. List your GPG keys to find your key ID
gpg --list-secret-keys --keyid-format=long
# Look for the line starting with 'sec' and note the ID after the slash

# 3. Export your public key (to add to GitHub)
gpg --armor --export YOUR_KEY_ID

# 4. Configure Git to use your GPG key
git config --global user.signingkey YOUR_KEY_ID

# 5. Enable commit signing by default (optional)
git config --global commit.gpgsign true

# 6. On Windows, configure GPG program path
git config --global gpg.program "C:/Program Files/Git/usr/bin/gpg.exe"

# 7. Sign individual commits
git commit -S -m "Your signed commit message"
```

**Adding GPG key to GitHub:**
1. Go to GitHub Settings → SSH and GPG keys
2. Click "New GPG key"
3. Paste your public key (from step 3 above)
4. Save the key

**Verification:**
- Signed commits show "Verified" badge on GitHub
- Required for `main` branch (recommended)
- Optional but encouraged for `staging`

### 3. Audit Trail
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

1. **Configure branch protection rules in GitHub**
   - Apply rules for `dev`, `staging`, and `main` branches
   - Enable "Include administrators" to enforce rules for everyone
   - Set up required status checks

2. **Set up environment protection rules**
   - Configure staging and production environments
   - Add required reviewers for production

3. **Set up GPG signing (optional but recommended)**
   - Generate and configure GPG keys
   - Add public key to GitHub
   - Require signed commits for `main` branch

4. **Add deployment credentials to GitHub Secrets**
   - Store environment-specific credentials
   - Use repository or organization secrets

5. **Update deployment steps in workflows**
   - Add actual deployment commands to workflows
   - Configure environment-specific settings

6. **Configure notification webhooks**
   - Set up Slack/Teams notifications for deployments
   - Configure alerts for test failures

7. **Test the complete pipeline**
   - Create a sample feature branch
   - Follow the complete flow: feature → dev → staging → main
   - Verify all protections and tests work as expected