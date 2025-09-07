# Branch Protection Setup Guide

This directory contains scripts to configure and verify branch protection rules that **completely prevent direct commits** to protected branches.

## 🚨 What These Scripts Do

The scripts configure GitHub branch protection to:
- **Block ALL direct commits** (even from repository admins)
- **Require pull requests** with approvals
- **Enforce status checks** (tests must pass)
- **Prevent branch deletion**
- **Block force pushes**
- **Enforce branch flow**: `feature → dev → staging → main`

## 📋 Prerequisites

1. **GitHub CLI** must be installed:
   ```bash
   # Windows (winget)
   winget install GitHub.cli
   
   # Windows (Chocolatey)
   choco install gh
   
   # macOS
   brew install gh
   ```

2. **Authenticate with GitHub**:
   ```bash
   gh auth login
   ```

3. **Repository admin access** is required to configure branch protection

## 🚀 Quick Start

### 1. Enable Branch Protection (Recommended)

```powershell
# Navigate to the scripts directory
cd .github/scripts

# Run the setup script
.\setup-branch-protection.ps1

# This will protect: dev, staging, and main branches
```

### 2. Verify Protection

```powershell
# Check if protection is working
.\verify-protection.ps1

# For detailed information
.\verify-protection.ps1 -Detailed
```

## 📝 Script Options

### setup-branch-protection.ps1

```powershell
# Dry run - see what would be configured without making changes
.\setup-branch-protection.ps1 -DryRun

# Skip confirmation prompt
.\setup-branch-protection.ps1 -Force

# Custom repository (if not in chadmcghie/Crud)
.\setup-branch-protection.ps1 -Repository "owner/repo"
```

### verify-protection.ps1

```powershell
# Basic verification
.\verify-protection.ps1

# Show all status checks and details
.\verify-protection.ps1 -Detailed

# Check different repository
.\verify-protection.ps1 -Repository "owner/repo"
```

## 🔒 Protection Rules Per Branch

### DEV Branch
- **Required Approvals**: 1
- **Required Status Checks**:
  - PR Validation Summary
  - Build Solution
  - Code Quality & Auto-Format
  - Backend Unit Tests
  - Frontend Unit Tests
  - Backend Integration Tests
  - E2E Smoke Tests (Quick Validation)
- **Additional**: Dismisses stale reviews, requires up-to-date branches

### STAGING Branch
- **Required Approvals**: 1
- **Required Status Checks**:
  - PR Validation Summary
  - Validate PR Source Branch (ensures PR from dev only)
- **Additional**: Dismisses stale reviews, requires up-to-date branches

### MAIN Branch
- **Required Approvals**: 2
- **Required Status Checks**:
  - Full Test Suite Before Production
  - Validate PR Source Branch (ensures PR from staging only)
  - Production Deployment Gate
- **Additional**: Dismisses stale reviews, requires up-to-date branches

## 🧪 Testing Protection

After running the setup script, test that direct commits are blocked:

```bash
# This should FAIL with a protection error
git checkout dev
echo "test" > test.txt
git add test.txt
git commit -m "Test direct commit"
git push origin dev
# Expected: Error about protected branch

# Clean up the failed commit
git reset --hard HEAD~1
```

## 🔄 Correct Workflow After Protection

Once protection is enabled, all changes must go through pull requests:

```bash
# 1. Create feature branch
git checkout dev
git pull origin dev
git checkout -b feature/my-feature

# 2. Make changes and push
git add .
git commit -m "feat: add new feature"
git push origin feature/my-feature

# 3. Create PR via GitHub CLI
gh pr create --base dev --title "Add new feature" --body "Description"

# 4. After approval and tests pass, merge via GitHub UI or:
gh pr merge --squash
```

## ⚠️ Important Notes

1. **Administrators Included**: Even repository admins cannot bypass these rules
2. **No Direct Commits**: You cannot push directly to protected branches anymore
3. **PR Required**: All changes must go through pull requests
4. **Tests Must Pass**: Merging is blocked until all status checks pass
5. **Branch Flow Enforced**: 
   - PRs to staging must come from dev
   - PRs to main must come from staging

## 🆘 Troubleshooting

### "GitHub CLI not found"
- Install GitHub CLI from https://cli.github.com/
- Ensure it's in your PATH

### "Not authenticated"
```bash
gh auth login
# Follow the prompts to authenticate
```

### "Permission denied"
- You need admin access to the repository
- Check your permissions: `gh api repos/chadmcghie/Crud/collaborators`

### "Branch not found"
- Ensure dev, staging, and main branches exist
- Create missing branches if needed:
  ```bash
  git checkout -b staging
  git push origin staging
  ```

### Protection not working
1. Run `.\verify-protection.ps1` to check status
2. Look for any ✗ marks indicating missing protection
3. Re-run `.\setup-branch-protection.ps1` if needed
4. Check GitHub Settings → Branches to verify visually

## 🔧 Manual Configuration

If you prefer to configure manually via GitHub UI:

1. Go to **Settings → Branches**
2. Click **Add rule**
3. Enter branch name pattern (e.g., `dev`)
4. Configure settings as documented in `.github/BRANCH_PROTECTION_RULES.md`
5. **Critical**: Enable "Include administrators" to prevent bypassing

## 📚 Additional Resources

- [Full Branch Protection Documentation](../BRANCH_PROTECTION_RULES.md)
- [GitHub Branch Protection API](https://docs.github.com/en/rest/branches/branch-protection)
- [GitHub CLI Documentation](https://cli.github.com/manual/)

## 💡 Tips

- Run `.\verify-protection.ps1` regularly to ensure protection remains active
- Consider adding commit signing requirement for production (main) branch
- Set up CODEOWNERS file for additional review requirements
- Use branch protection rules as part of your security compliance

---

**Remember**: Once enabled, these protections ensure code quality and prevent accidental direct pushes to critical branches. This is a one-way door - make sure your team understands the PR workflow before enabling!