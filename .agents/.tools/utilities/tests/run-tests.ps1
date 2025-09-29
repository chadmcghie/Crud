# Simple test runner for Agent OS Command Dashboard
$ErrorActionPreference = "Stop"

# Get script location
$TestDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ToolsDir = Split-Path -Parent $TestDir

Write-Host "`nRunning Agent OS Command Dashboard Tests" -ForegroundColor Cyan
Write-Host ("=" * 50) -ForegroundColor Cyan

$PASSED = 0
$FAILED = 0

# Test 1: dashboard.md exists
Write-Host "`nTesting Dashboard Documentation:" -ForegroundColor Yellow
$dashboardPath = Join-Path $ToolsDir "dashboard.md"
if (Test-Path $dashboardPath) {
  Write-Host "[PASS] dashboard.md exists" -ForegroundColor Green
  $PASSED++
} else {
  Write-Host "[FAIL] dashboard.md exists" -ForegroundColor Red
  Write-Host "   File not found: $dashboardPath" -ForegroundColor Yellow
  $FAILED++
}

# Test 2: aos-commands.ps1 exists
Write-Host "`nTesting PowerShell Script:" -ForegroundColor Yellow
$ps1Path = Join-Path $ToolsDir "aos-commands.ps1"
if (Test-Path $ps1Path) {
  Write-Host "[PASS] aos-commands.ps1 exists" -ForegroundColor Green
  $PASSED++
} else {
  Write-Host "[FAIL] aos-commands.ps1 exists" -ForegroundColor Red
  Write-Host "   File not found: $ps1Path" -ForegroundColor Yellow
  $FAILED++
}

# Test 3: aos-commands.sh exists
Write-Host "`nTesting Bash Script:" -ForegroundColor Yellow
$shPath = Join-Path $ToolsDir "aos-commands.sh"
if (Test-Path $shPath) {
  Write-Host "[PASS] aos-commands.sh exists" -ForegroundColor Green
  $PASSED++
} else {
  Write-Host "[FAIL] aos-commands.sh exists" -ForegroundColor Red
  Write-Host "   File not found: $shPath" -ForegroundColor Yellow
  $FAILED++
}

# Summary
Write-Host "`n" ("=" * 50) -ForegroundColor Cyan
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "   Passed: $PASSED" -ForegroundColor Green
if ($FAILED -gt 0) {
  Write-Host "   Failed: $FAILED" -ForegroundColor Red
  Write-Host "`n[FAILED] Test suite failed with $FAILED error(s)" -ForegroundColor Red
  exit 1
} else {
  Write-Host "   Failed: $FAILED" -ForegroundColor Green
  Write-Host "`nAll tests passed!" -ForegroundColor Green
  exit 0
}