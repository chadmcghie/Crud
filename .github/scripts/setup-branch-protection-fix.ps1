# Fix Branch Protection Rules for Status Checks
# This script aligns the required status checks with the actual workflow job names

Write-Host "🔧 Fixing branch protection rules for status check alignment..." -ForegroundColor Yellow

# Check if gh CLI is available
if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ GitHub CLI (gh) is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install GitHub CLI: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check if user is authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Not authenticated with GitHub CLI" -ForegroundColor Red
    Write-Host "Please run: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ GitHub CLI is ready" -ForegroundColor Green

# Get repository info
$repoInfo = gh repo view --json owner,name | ConvertFrom-Json
$owner = $repoInfo.owner.login
$repo = $repoInfo.name

Write-Host "🏗️  Configuring branch protection for: $owner/$repo" -ForegroundColor Cyan

# The exact status check names that our PR validation workflow produces
$requiredChecks = @(
    "Backend Unit Tests",
    "Frontend Unit Tests", 
    "Backend Integration Tests",
    "PR Validation Summary"
)

Write-Host "📋 Required status checks:" -ForegroundColor Cyan
$requiredChecks | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }

# Update dev branch protection
Write-Host "🛡️  Updating dev branch protection rules..." -ForegroundColor Yellow

try {
    # First, let's see what the current protection looks like
    Write-Host "📊 Current dev branch protection:" -ForegroundColor Cyan
    $currentProtection = gh api "repos/$owner/$repo/branches/dev/protection" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Dev branch protection exists" -ForegroundColor Green
    } else {
        Write-Host "⚠️  No existing protection found, will create new" -ForegroundColor Yellow
    }

    # Create the protection rule with correct status checks
    $protectionConfig = @{
        required_status_checks = @{
            strict = $true
            contexts = $requiredChecks
        }
        enforce_admins = $true
        required_pull_request_reviews = @{
            required_approving_review_count = 1
            dismiss_stale_reviews = $true
        }
        restrictions = $null
    } | ConvertTo-Json -Depth 10

    Write-Host "🔄 Applying new protection rules..." -ForegroundColor Yellow
    
    # Apply the protection
    $result = gh api --method PUT "repos/$owner/$repo/branches/dev/protection" --input - <<< $protectionConfig
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Dev branch protection updated successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to update branch protection" -ForegroundColor Red
        Write-Host "Error: $result" -ForegroundColor Red
        exit 1
    }

} catch {
    Write-Host "❌ Error updating branch protection: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 Branch protection rules have been aligned!" -ForegroundColor Green
Write-Host "The following status checks are now required for dev branch:" -ForegroundColor Cyan
$requiredChecks | ForEach-Object { Write-Host "  ✓ $_" -ForegroundColor Green }

Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Yellow
Write-Host "1. Your existing PR should now show proper status checks" -ForegroundColor White
Write-Host "2. Wait a few minutes for GitHub to refresh the PR status" -ForegroundColor White
Write-Host "3. If issues persist, close and reopen the PR to trigger status check refresh" -ForegroundColor White

Write-Host ""
Write-Host "🔍 To verify the changes:" -ForegroundColor Cyan
Write-Host "gh api repos/$owner/$repo/branches/dev/protection | jq '.required_status_checks.contexts'" -ForegroundColor Gray
