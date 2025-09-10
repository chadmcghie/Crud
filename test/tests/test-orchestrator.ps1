# Test suite for Agent Orchestrator
# Tests the orchestrator agent's ability to analyze requests and delegate to appropriate agents

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

function Test-OrchestratorFileExists {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  $exists = Test-Path $orchestratorPath
  Write-TestResult -TestName "Orchestrator agent file exists" -Success $exists -ErrorMessage "File not found: $orchestratorPath"
  return $exists
}

function Test-OrchestratorStructure {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "Orchestrator structure validation" -Success $false -ErrorMessage "File not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for required sections
  $requiredSections = @(
    "## Purpose",
    "## Capabilities",
    "## Request Analysis",
    "## Agent Selection",
    "## Delegation Patterns",
    "## Error Handling"
  )
  
  $allSectionsFound = $true
  foreach ($section in $requiredSections) {
    if ($content -notmatch [regex]::Escape($section)) {
      Write-TestResult -TestName "Orchestrator contains '$section'" -Success $false -ErrorMessage "Section missing"
      $allSectionsFound = $false
    } else {
      Write-TestResult -TestName "Orchestrator contains '$section'" -Success $true
    }
  }
  
  return $allSectionsFound
}

function Test-AgentRegistry {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "Agent registry validation" -Success $false -ErrorMessage "File not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for essential agents in registry
  $requiredAgents = @(
    "test-runner",
    "file-creator",
    "git-workflow",
    "project-manager",
    "context-fetcher"
  )
  
  $allAgentsFound = $true
  foreach ($agent in $requiredAgents) {
    if ($content -match $agent) {
      Write-TestResult -TestName "Registry contains '$agent' agent" -Success $true
    } else {
      Write-TestResult -TestName "Registry contains '$agent' agent" -Success $false -ErrorMessage "Agent not found in registry"
      $allAgentsFound = $false
    }
  }
  
  return $allAgentsFound
}

function Test-RequestPatterns {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "Request patterns validation" -Success $false -ErrorMessage "File not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for request pattern examples
  $patterns = @(
    "test",
    "create",
    "commit",
    "review",
    "debug"
  )
  
  $patternsFound = 0
  foreach ($pattern in $patterns) {
    if ($content -match "(?i)$pattern") {
      $patternsFound++
    }
  }
  
  $success = $patternsFound -ge 3
  Write-TestResult -TestName "Request patterns defined (found $patternsFound/5)" -Success $success
  
  return $success
}

function Test-DependencyGraph {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "Dependency graph validation" -Success $false -ErrorMessage "File not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for dependency definitions
  $hasDependencies = ($content -match "(?i)depend") -or ($content -match "(?i)prerequisite") -or ($content -match "(?i)require")
  Write-TestResult -TestName "Dependency relationships defined" -Success $hasDependencies
  
  return $hasDependencies
}

function Test-ErrorHandling {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "Error handling validation" -Success $false -ErrorMessage "File not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for error handling scenarios
  $errorScenarios = @(
    "agent.*fail",
    "timeout",
    "unavailable",
    "fallback",
    "retry"
  )
  
  $scenariosFound = 0
  foreach ($scenario in $errorScenarios) {
    if ($content -match "(?i)$scenario") {
      $scenariosFound++
    }
  }
  
  $success = $scenariosFound -ge 2
  Write-TestResult -TestName "Error scenarios handled (found $scenariosFound/5)" -Success $success
  
  return $success
}

function Test-WorkflowExamples {
  $orchestratorPath = Join-Path (Split-Path -Parent $PSScriptRoot) "orchestrator.md"
  
  if (-not (Test-Path $orchestratorPath)) {
    Write-TestResult -TestName "Workflow examples validation" -Success $false -ErrorMessage "File not found"
    return $false
  }
  
  $content = Get-Content $orchestratorPath -Raw
  
  # Check for workflow examples
  $hasExamples = ($content -match "(?i)example") -or ($content -match "(?i)workflow") -or ($content -match "(?i)scenario")
  Write-TestResult -TestName "Workflow examples provided" -Success $hasExamples
  
  return $hasExamples
}

# Main test execution
Write-Host "`nRunning Agent Orchestrator Tests" -ForegroundColor Cyan
Write-Host ("=" * 50) -ForegroundColor Cyan

# Test 1: Check if orchestrator.md exists
Write-Host "`nTesting Orchestrator File:" -ForegroundColor Yellow
Test-OrchestratorFileExists

# Test 2: Validate orchestrator structure
Write-Host "`nTesting Orchestrator Structure:" -ForegroundColor Yellow
Test-OrchestratorStructure

# Test 3: Check agent registry
Write-Host "`nTesting Agent Registry:" -ForegroundColor Yellow
Test-AgentRegistry

# Test 4: Check request patterns
Write-Host "`nTesting Request Patterns:" -ForegroundColor Yellow
Test-RequestPatterns

# Test 5: Check dependency graph
Write-Host "`nTesting Dependency Graph:" -ForegroundColor Yellow
Test-DependencyGraph

# Test 6: Check error handling
Write-Host "`nTesting Error Handling:" -ForegroundColor Yellow
Test-ErrorHandling

# Test 7: Check workflow examples
Write-Host "`nTesting Workflow Examples:" -ForegroundColor Yellow
Test-WorkflowExamples

# Summary
Write-Host "`n" ("=" * 50) -ForegroundColor Cyan
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "   Passed: $($TestResults.Passed)" -ForegroundColor Green
if ($TestResults.Failed -gt 0) {
  Write-Host "   Failed: $($TestResults.Failed)" -ForegroundColor Red
  Write-Host "`n[FAILED] Test suite failed with $($TestResults.Failed) error(s)" -ForegroundColor Red
  if ($Verbose) {
    Write-Host "`nError Details:" -ForegroundColor Yellow
    $TestResults.Errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
  }
  exit 1
} else {
  Write-Host "   Failed: $($TestResults.Failed)" -ForegroundColor Green
  Write-Host "`nAll tests passed!" -ForegroundColor Green
  exit 0
}