# Setup Branch Protection Rules for Crud Repository
# This script configures branch protection for dev, staging, and main branches
# Requirements: GitHub CLI (gh) must be installed and authenticated

param(
    [string]$Repository = "chadmcghie/Crud",
    [switch]$DryRun = $false,
    [switch]$Force = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Branch Protection Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if gh CLI is installed
try {
    $ghVersion = gh --version
    Write-Host "[OK] GitHub CLI found: $($ghVersion[0])" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] GitHub CLI (gh) is not installed or not in PATH" -ForegroundColor Red
    Write-Host "  Please install from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check if authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Not authenticated with GitHub" -ForegroundColor Red
    Write-Host "  Run: gh auth login" -ForegroundColor Yellow
    exit 1
}
Write-Host "[OK] Authenticated with GitHub" -ForegroundColor Green
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

# Function to create or update branch protection
function Set-BranchProtection {
    param(
        [string]$Branch,
        [string]$RequiredApprovals,
        [string[]]$RequiredStatusChecks,
        [bool]$RequireSignedCommits = $false,
        [bool]$DismissStaleReviews = $true,
        [bool]$RequireUpToDate = $true
    )
    
    Write-Host "Configuring protection for branch: $Branch" -ForegroundColor Cyan
    
    # Build the API payload
    $payload = @{
        required_status_checks = if ($RequiredStatusChecks.Count -gt 0) {
            @{
                strict = $RequireUpToDate
                contexts = $RequiredStatusChecks
            }
        } else { $null }
        
        enforce_admins = $true
        
        required_pull_request_reviews = @{
            required_approving_review_count = [int]$RequiredApprovals
            dismiss_stale_reviews = $DismissStaleReviews
            require_code_owner_reviews = $false
            require_last_push_approval = $false
        }
        
        restrictions = $null
        
        allow_force_pushes = $false
        allow_deletions = $false
        block_creations = $false
        required_conversation_resolution = $true
        lock_branch = $false
        allow_fork_syncing = $false
        required_signatures = $RequireSignedCommits
    }
    
    $jsonPayload = $payload | ConvertTo-Json -Depth 10 -Compress
    
    if ($DryRun) {
        Write-Host "  Would apply the following protection:" -ForegroundColor Gray
        Write-Host "  - Required approvals: $RequiredApprovals" -ForegroundColor Gray
        Write-Host "  - Required status checks: $($RequiredStatusChecks -join ', ')" -ForegroundColor Gray
        Write-Host "  - Dismiss stale reviews: $DismissStaleReviews" -ForegroundColor Gray
        Write-Host "  - Require up-to-date: $RequireUpToDate" -ForegroundColor Gray
        Write-Host "  - Require signed commits: $RequireSignedCommits" -ForegroundColor Gray
        Write-Host "  - Include administrators: true" -ForegroundColor Gray
        Write-Host "  - Allow force pushes: false" -ForegroundColor Gray
        Write-Host "  - Allow deletions: false" -ForegroundColor Gray
        return
    }
    
    try {
        # Use gh api to set branch protection
        $result = $jsonPayload | gh api `
            --method PUT `
            --input - `
            "repos/$Repository/branches/$Branch/protection" `
            2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] Protection applied successfully" -ForegroundColor Green
        } else {
            Write-Host "  [ERROR] Failed to apply protection" -ForegroundColor Red
            Write-Host "    Error: $result" -ForegroundColor Red
        }
    } catch {
        Write-Host "  [ERROR] Error applying protection: $_" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Main execution
Write-Host "Repository: $Repository" -ForegroundColor White
Write-Host ""

if (-not $Force) {
    Write-Host "This script will configure branch protection for:" -ForegroundColor Yellow
    Write-Host "  - dev (default branch)" -ForegroundColor White
    Write-Host "  - staging (pre-production)" -ForegroundColor White
    Write-Host "  - main (production)" -ForegroundColor White
    Write-Host ""
    Write-Host "This will PREVENT all direct commits to these branches!" -ForegroundColor Yellow
    Write-Host ""
    
    $confirmation = Read-Host "Do you want to continue? (yes/no)"
    if ($confirmation -ne "yes") {
        Write-Host "Aborted by user" -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Configure dev branch
Set-BranchProtection `
    -Branch "dev" `
    -RequiredApprovals 1 `
    -RequiredStatusChecks @(
        "PR Validation Summary",
        "Build Solution",
        "Code Quality and Auto-Format",
        "Backend Unit Tests",
        "Frontend Unit Tests",
        "Backend Integration Tests",
        "E2E Smoke Tests (Quick Validation)"
    ) `
    -RequireSignedCommits $false `
    -DismissStaleReviews $true `
    -RequireUpToDate $true

# Configure staging branch
Set-BranchProtection `
    -Branch "staging" `
    -RequiredApprovals 1 `
    -RequiredStatusChecks @(
        "PR Validation Summary",
        "Validate PR Source Branch"
    ) `
    -RequireSignedCommits $false `
    -DismissStaleReviews $true `
    -RequireUpToDate $true

# Configure main branch
Set-BranchProtection `
    -Branch "main" `
    -RequiredApprovals 2 `
    -RequiredStatusChecks @(
        "Full Test Suite Before Production",
        "Validate PR Source Branch",
        "Production Deployment Gate"
    ) `
    -RequireSignedCommits $false `
    -DismissStaleReviews $true `
    -RequireUpToDate $true

if (-not $DryRun) {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Branch protection setup complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Run ./verify-protection.ps1 to verify the setup" -ForegroundColor White
    Write-Host "2. Try to push directly to dev/staging/main (should fail)" -ForegroundColor White
    Write-Host "3. Create a PR to test the workflow" -ForegroundColor White
    Write-Host ""
    Write-Host "To enable commit signing for main branch:" -ForegroundColor Yellow
    Write-Host "  Re-run with: ./setup-branch-protection.ps1 -RequireSignedCommits" -ForegroundColor White
} else {
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Dry run complete - no changes made" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To apply these changes, run without -DryRun flag:" -ForegroundColor White
    Write-Host "  ./setup-branch-protection.ps1" -ForegroundColor Cyan
}
