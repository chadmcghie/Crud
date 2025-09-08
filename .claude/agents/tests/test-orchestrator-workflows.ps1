# Test suite for Agent Orchestrator Workflows
# Tests the orchestrator against sample real-world workflows

param(
  [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$TestResults = @{
  Passed = 0
  Failed = 0
  Errors = @()
}

function Write-TestResult {
  param(
    [string]$TestName,
    [bool]$Success,
    [string]$ErrorMessage = ""
  )
  
  if ($Success) {
    Write-Host "[PASS] $TestName" -ForegroundColor Green
    $TestResults.Passed++
  } else {
    Write-Host "[FAIL] $TestName" -ForegroundColor Red
    if ($ErrorMessage) {
      Write-Host "   Error: $ErrorMessage" -ForegroundColor Yellow
    }
    $TestResults.Failed++
    $TestResults.Errors += "${TestName}: ${ErrorMessage}"
  }
}

function Test-WorkflowAnalysis {
  param(
    [string]$Request,
    [string[]]$ExpectedAgents,
    [string]$WorkflowName
  )
  
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "$WorkflowName workflow analysis" -Success $false -ErrorMessage "Orchestrator not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check if the orchestrator can handle this type of request
  $keywords = $Request.ToLower() -split '\s+'
  $matchedAgents = @()
  
  # Simple keyword matching against the orchestrator content
  foreach ($agent in $ExpectedAgents) {
    if ($content -match "(?i)$agent") {
      $matchedAgents += $agent
    }
  }
  
  $success = $matchedAgents.Count -eq $ExpectedAgents.Count
  $resultMsg = if ($success) {
    "All expected agents found"
  } else {
    "Found $($matchedAgents.Count)/$($ExpectedAgents.Count) agents"
  }
  
  Write-TestResult -TestName "$WorkflowName workflow ($resultMsg)" -Success $success
  
  if ($Verbose -and -not $success) {
    Write-Host "   Expected: $($ExpectedAgents -join ', ')" -ForegroundColor Gray
    Write-Host "   Found: $($matchedAgents -join ', ')" -ForegroundColor Gray
  }
  
  return $success
}

# Test Workflow 1: Feature Implementation
Write-Host "`nTesting Feature Implementation Workflow:" -ForegroundColor Yellow
$featureRequest = "Create a new authentication component with tests and commit it"
$featureAgents = @("context-fetcher", "file-creator", "test-runner", "commit-strategist")
Test-WorkflowAnalysis -Request $featureRequest -ExpectedAgents $featureAgents -WorkflowName "Feature Implementation"

# Test Workflow 2: Bug Fix
Write-Host "`nTesting Bug Fix Workflow:" -ForegroundColor Yellow
$bugRequest = "The login tests are failing, debug and fix them"
$bugAgents = @("test-runner", "troubleshoot-with-history", "context-fetcher", "file-creator")
Test-WorkflowAnalysis -Request $bugRequest -ExpectedAgents $bugAgents -WorkflowName "Bug Fix"

# Test Workflow 3: Documentation Update
Write-Host "`nTesting Documentation Workflow:" -ForegroundColor Yellow
$docRequest = "Update the API documentation and commit the changes"
$docAgents = @("context-fetcher", "file-creator", "commit-strategist")
Test-WorkflowAnalysis -Request $docRequest -ExpectedAgents $docAgents -WorkflowName "Documentation Update"

# Test Workflow 4: Status Check
Write-Host "`nTesting Status Check Workflow:" -ForegroundColor Yellow
$statusRequest = "What's the current project status?"
$statusAgents = @("project-manager", "test-runner", "git-workflow")
Test-WorkflowAnalysis -Request $statusRequest -ExpectedAgents $statusAgents -WorkflowName "Status Check"

# Test Workflow 5: Commit and Push
Write-Host "`nTesting Commit and Push Workflow:" -ForegroundColor Yellow
$commitRequest = "Commit these changes and create a PR"
$commitAgents = @("commit-strategist", "git-workflow", "project-manager")
Test-WorkflowAnalysis -Request $commitRequest -ExpectedAgents $commitAgents -WorkflowName "Commit and Push"

# Test Workflow 6: Test Execution
Write-Host "`nTesting Test Execution Workflow:" -ForegroundColor Yellow
$testRequest = "Run all tests and verify they pass"
$testAgents = @("test-runner")
Test-WorkflowAnalysis -Request $testRequest -ExpectedAgents $testAgents -WorkflowName "Test Execution"

# Test Workflow 7: Debugging
Write-Host "`nTesting Debugging Workflow:" -ForegroundColor Yellow
$debugRequest = "Debug the build error and troubleshoot the issue"
$debugAgents = @("troubleshoot-with-history", "context-fetcher")
Test-WorkflowAnalysis -Request $debugRequest -ExpectedAgents $debugAgents -WorkflowName "Debugging"

# Test Workflow 8: Blocking Issue Documentation
Write-Host "`nTesting Blocking Issue Workflow:" -ForegroundColor Yellow
$blockingRequest = "Document this blocking issue"
$blockingAgents = @("document-blocking-issue", "context-fetcher")
Test-WorkflowAnalysis -Request $blockingRequest -ExpectedAgents $blockingAgents -WorkflowName "Blocking Issue"

# Test Agent Conflict Resolution
Write-Host "`nTesting Agent Conflict Resolution:" -ForegroundColor Yellow
function Test-ConflictResolution {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  $content = Get-Content $orchestratorPath -Raw
  
  # Check that commit is only in commit-strategist, not in git-workflow triggers
  $gitWorkflowSection = $content -match "git-workflow:[\s\S]*?triggers: \[([^\]]+)\]"
  if ($gitWorkflowSection) {
    $gitTriggers = $Matches[1]
    $hasCommitInGit = $gitTriggers -match "commit"
    Write-TestResult -TestName "Git-workflow doesn't have 'commit' trigger" -Success (-not $hasCommitInGit)
  }
  
  # Check that commit-strategist has commit as a trigger
  $commitStrategistSection = $content -match "commit-strategist:[\s\S]*?triggers: \[([^\]]+)\]"
  if ($commitStrategistSection) {
    $commitTriggers = $Matches[1]
    $hasCommitInStrategist = $commitTriggers -match "commit"
    Write-TestResult -TestName "Commit-strategist has 'commit' trigger" -Success $hasCommitInStrategist
  }
}
Test-ConflictResolution

# Test Dependency Order
Write-Host "`nTesting Dependency Order:" -ForegroundColor Yellow
function Test-DependencyOrder {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for dependency graph section
  $hasDependencyGraph = $content -match "(?i)dependency graph"
  Write-TestResult -TestName "Dependency graph is defined" -Success $hasDependencyGraph
  
  # Check for key dependencies
  $hasContextFirst = $content -match "context-fetcher.*should run before"
  Write-TestResult -TestName "Context-fetcher runs first rule" -Success $hasContextFirst
  
  $hasCommitBeforeGit = $content -match "commit-strategist.*before.*git-workflow"
  Write-TestResult -TestName "Commit before git push rule" -Success $hasCommitBeforeGit
}
Test-DependencyOrder

# Test Error Handling Coverage
Write-Host "`nTesting Error Handling Coverage:" -ForegroundColor Yellow
function Test-ErrorHandlingCoverage {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  $content = Get-Content $orchestratorPath -Raw
  
  $errorScenarios = @(
    "Agent Unavailable",
    "Agent Timeout", 
    "Agent Failure",
    "Dependency Failure"
  )
  
  foreach ($scenario in $errorScenarios) {
    $hasScenario = $content -match [regex]::Escape($scenario)
    Write-TestResult -TestName "Handles '$scenario' scenario" -Success $hasScenario
  }
  
  # Check for recovery strategies
  $recoveryStrategies = @(
    "Retry Logic",
    "Fallback",
    "Manual Intervention"
  )
  
  foreach ($strategy in $recoveryStrategies) {
    $hasStrategy = $content -match $strategy
    Write-TestResult -TestName "Has '$strategy' recovery strategy" -Success $hasStrategy
  }
}
Test-ErrorHandlingCoverage

# Summary
Write-Host "`n" ("=" * 50) -ForegroundColor Cyan
Write-Host "Workflow Test Summary:" -ForegroundColor Cyan
Write-Host "   Passed: $($TestResults.Passed)" -ForegroundColor Green
if ($TestResults.Failed -gt 0) {
  Write-Host "   Failed: $($TestResults.Failed)" -ForegroundColor Red
  Write-Host "`n[FAILED] Workflow tests failed with $($TestResults.Failed) error(s)" -ForegroundColor Red
  if ($Verbose) {
    Write-Host "`nError Details:" -ForegroundColor Yellow
    $TestResults.Errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
  }
  exit 1
} else {
  Write-Host "   Failed: $($TestResults.Failed)" -ForegroundColor Green
  Write-Host "`nAll workflow tests passed!" -ForegroundColor Green
  exit 0
}