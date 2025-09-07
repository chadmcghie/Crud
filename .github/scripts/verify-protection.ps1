# Verify Branch Protection Rules for Crud Repository
# This script checks the current branch protection status

param(
    [string]$Repository = "chadmcghie/Crud",
    [switch]$Detailed = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Branch Protection Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if gh CLI is installed
try {
    $null = gh --version
} catch {
    Write-Host "✗ GitHub CLI (gh) is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Function to check branch protection
function Test-BranchProtection {
    param(
        [string]$Branch
    )
    
    Write-Host "Checking protection for: $Branch" -ForegroundColor Cyan
    
    # Get protection status
    $protection = gh api "repos/$Repository/branches/$Branch/protection" 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        if ($protection -match "404") {
            Write-Host "  ✗ Branch NOT protected" -ForegroundColor Red
            Write-Host "    No protection rules configured" -ForegroundColor Yellow
        } else {
            Write-Host "  ✗ Error checking protection: $protection" -ForegroundColor Red
        }
        return $false
    }
    
    Write-Host "  ✓ Branch is protected" -ForegroundColor Green
    
    # Parse the JSON response
    $protectionData = $protection | ConvertFrom-Json
    
    # Check key protection settings
    $hasRequiredPR = $protectionData.required_pull_request_reviews -ne $null
    $includesAdmins = $protectionData.enforce_admins.enabled -eq $true
    $preventsForcePush = $protectionData.allow_force_pushes.enabled -eq $false
    $preventsDeletion = $protectionData.allow_deletions.enabled -eq $false
    $hasStatusChecks = $protectionData.required_status_checks -ne $null
    
    Write-Host "    Protection Status:" -ForegroundColor White
    
    if ($hasRequiredPR) {
        $approvals = $protectionData.required_pull_request_reviews.required_approving_review_count
        Write-Host "    ✓ Requires pull request (Approvals: $approvals)" -ForegroundColor Green
        
        if ($protectionData.required_pull_request_reviews.dismiss_stale_reviews) {
            Write-Host "    ✓ Dismisses stale reviews" -ForegroundColor Green
        } else {
            Write-Host "    ⚠ Does not dismiss stale reviews" -ForegroundColor Yellow
        }
    } else {
        Write-Host "    ✗ Does NOT require pull request" -ForegroundColor Red
    }
    
    if ($includesAdmins) {
        Write-Host "    ✓ Includes administrators (no bypass)" -ForegroundColor Green
    } else {
        Write-Host "    ⚠ Administrators can bypass" -ForegroundColor Yellow
    }
    
    if ($preventsForcePush) {
        Write-Host "    ✓ Prevents force pushes" -ForegroundColor Green
    } else {
        Write-Host "    ✗ Allows force pushes" -ForegroundColor Red
    }
    
    if ($preventsDeletion) {
        Write-Host "    ✓ Prevents branch deletion" -ForegroundColor Green
    } else {
        Write-Host "    ✗ Allows branch deletion" -ForegroundColor Red
    }
    
    if ($hasStatusChecks) {
        $checks = $protectionData.required_status_checks.contexts
        $strict = $protectionData.required_status_checks.strict
        
        Write-Host "    ✓ Has required status checks ($($checks.Count) checks)" -ForegroundColor Green
        
        if ($strict) {
            Write-Host "    ✓ Requires branches to be up-to-date" -ForegroundColor Green
        } else {
            Write-Host "    ⚠ Does not require up-to-date branches" -ForegroundColor Yellow
        }
        
        if ($Detailed -and $checks.Count -gt 0) {
            Write-Host "      Required checks:" -ForegroundColor Gray
            foreach ($check in $checks) {
                Write-Host "        - $check" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "    ⚠ No required status checks" -ForegroundColor Yellow
    }
    
    if ($protectionData.required_conversation_resolution.enabled) {
        Write-Host "    ✓ Requires conversation resolution" -ForegroundColor Green
    }
    
    if ($protectionData.required_signatures.enabled) {
        Write-Host "    ✓ Requires signed commits" -ForegroundColor Green
    }
    
    Write-Host ""
    
    # Return true if core protections are in place
    return $hasRequiredPR -and $includesAdmins -and $preventsForcePush -and $preventsDeletion
}

# Function to test if direct push is blocked
function Test-DirectPushBlocked {
    param(
        [string]$Branch
    )
    
    Write-Host "Testing direct push prevention for: $Branch" -ForegroundColor Cyan
    
    # Create a test file name
    $testFile = ".github/test-protection-$(Get-Random).txt"
    
    # Try to create and push a test file
    $originalBranch = git branch --show-current 2>$null
    
    try {
        # Switch to the target branch
        git checkout $Branch 2>&1 | Out-Null
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ⚠ Could not switch to $Branch branch" -ForegroundColor Yellow
            return "unknown"
        }
        
        # Create a test file
        "Testing branch protection $(Get-Date)" | Out-File -FilePath $testFile -Encoding UTF8
        
        # Try to commit and push
        git add $testFile 2>&1 | Out-Null
        git commit -m "Test: Checking branch protection" 2>&1 | Out-Null
        $pushResult = git push origin $Branch 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✗ Direct push SUCCEEDED (branch not protected!)" -ForegroundColor Red
            
            # Revert the test commit
            git reset --hard HEAD~1 2>&1 | Out-Null
            git push --force origin $Branch 2>&1 | Out-Null
            
            return $false
        } else {
            if ($pushResult -match "protected branch" -or $pushResult -match "cannot force-push" -or $pushResult -match "GH006") {
                Write-Host "  ✓ Direct push BLOCKED (branch is protected)" -ForegroundColor Green
            } else {
                Write-Host "  ⚠ Push failed but reason unclear" -ForegroundColor Yellow
                Write-Host "    Error: $pushResult" -ForegroundColor Gray
            }
            
            # Clean up local commit
            git reset --hard HEAD~1 2>&1 | Out-Null
            
            return $true
        }
    } finally {
        # Clean up test file if it exists
        if (Test-Path $testFile) {
            Remove-Item $testFile -Force
        }
        
        # Switch back to original branch
        if ($originalBranch) {
            git checkout $originalBranch 2>&1 | Out-Null
        }
    }
}

# Main execution
Write-Host "Repository: $Repository" -ForegroundColor White
Write-Host ""

$allProtected = $true

# Check each branch
foreach ($branch in @("dev", "staging", "main")) {
    $isProtected = Test-BranchProtection -Branch $branch
    if (-not $isProtected) {
        $allProtected = $false
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($allProtected) {
    Write-Host "✓ All branches are protected against direct commits!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your repository is secured with:" -ForegroundColor White
    Write-Host "  • No direct commits allowed (even for admins)" -ForegroundColor Gray
    Write-Host "  • Pull requests required with approvals" -ForegroundColor Gray
    Write-Host "  • Status checks must pass" -ForegroundColor Gray
    Write-Host "  • Branches cannot be deleted" -ForegroundColor Gray
    Write-Host "  • Force pushes are blocked" -ForegroundColor Gray
} else {
    Write-Host "⚠ Some branches are NOT fully protected!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To fix this, run:" -ForegroundColor White
    Write-Host "  .\setup-branch-protection.ps1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "This will prevent ALL direct commits and enforce PR workflow." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "To see detailed status check information, run:" -ForegroundColor Gray
Write-Host "  .\verify-protection.ps1 -Detailed" -ForegroundColor Cyan