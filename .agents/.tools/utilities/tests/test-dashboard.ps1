# Test suite for Agent OS Command Dashboard
# Tests the functionality of aos-commands.ps1 and dashboard.md

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
    Write-Host "✅ $TestName" -ForegroundColor Green
    $TestResults.Passed++
  } else {
    Write-Host "❌ $TestName" -ForegroundColor Red
    if ($ErrorMessage) {
      Write-Host "   Error: $ErrorMessage" -ForegroundColor Yellow
    }
    $TestResults.Failed++
    $TestResults.Errors += "${TestName}: ${ErrorMessage}"
  }
}

function Test-FileExists {
  param(
    [string]$FilePath,
    [string]$Description
  )
  
  $exists = Test-Path $FilePath
  Write-TestResult -TestName $Description -Success $exists -ErrorMessage "File not found: $FilePath"
  return $exists
}

function Test-CommandFunction {
  param(
    [string]$FunctionName,
    [string]$ScriptPath
  )
  
  try {
    # Source the script
    . $ScriptPath
    
    # Check if function exists
    $functionExists = Get-Command -Name $FunctionName -ErrorAction SilentlyContinue
    
    if ($functionExists) {
      Write-TestResult -TestName "Function $FunctionName exists" -Success $true
      return $true
    } else {
      Write-TestResult -TestName "Function $FunctionName exists" -Success $false -ErrorMessage "Function not defined"
      return $false
    }
  } catch {
    Write-TestResult -TestName "Function $FunctionName exists" -Success $false -ErrorMessage $_.Exception.Message
    return $false
  }
}

function Test-DashboardContent {
  param(
    [string]$DashboardPath
  )
  
  if (-not (Test-Path $DashboardPath)) {
    Write-TestResult -TestName "Dashboard content validation" -Success $false -ErrorMessage "Dashboard file not found"
    return $false
  }
  
  $content = Get-Content $DashboardPath -Raw
  
  # Check for required sections
  $requiredSections = @(
    "# Agent OS Command Dashboard",
    "## Quick Commands",
    "## Installation",
    "## Command Reference"
  )
  
  $allSectionsFound = $true
  foreach ($section in $requiredSections) {
    if ($content -notmatch [regex]::Escape($section)) {
      Write-TestResult -TestName "Dashboard contains '$section'" -Success $false -ErrorMessage "Section missing"
      $allSectionsFound = $false
    } else {
      Write-TestResult -TestName "Dashboard contains '$section'" -Success $true
    }
  }
  
  return $allSectionsFound
}

function Test-ShellScriptSyntax {
  param(
    [string]$ScriptPath
  )
  
  if (-not (Test-Path $ScriptPath)) {
    Write-TestResult -TestName "Shell script syntax check" -Success $false -ErrorMessage "Script not found"
    return $false
  }
  
  # For bash scripts, we can check basic syntax if bash is available
  if ($ScriptPath -match "\.sh$") {
    $bashExists = Get-Command bash -ErrorAction SilentlyContinue
    if ($bashExists) {
      try {
        $result = & bash -n $ScriptPath 2>&1
        if ($LASTEXITCODE -eq 0) {
          Write-TestResult -TestName "Bash script syntax valid" -Success $true
          return $true
        } else {
          Write-TestResult -TestName "Bash script syntax valid" -Success $false -ErrorMessage $result
          return $false
        }
      } catch {
        Write-TestResult -TestName "Bash script syntax valid" -Success $false -ErrorMessage $_.Exception.Message
        return $false
      }
    } else {
      Write-Host "⚠️  Skipping bash syntax check (bash not available)" -ForegroundColor Yellow
      return $true
    }
  }
  
  # For PowerShell scripts
  if ($ScriptPath -match "\.ps1$") {
    try {
      $errors = $null
      $tokens = $null
      $ast = [System.Management.Automation.Language.Parser]::ParseFile($ScriptPath, [ref]$tokens, [ref]$errors)
      
      if ($errors.Count -eq 0) {
        Write-TestResult -TestName "PowerShell script syntax valid" -Success $true
        return $true
      } else {
        $errorMsg = ($errors | ForEach-Object { $_.Message }) -join "; "
        Write-TestResult -TestName "PowerShell script syntax valid" -Success $false -ErrorMessage $errorMsg
        return $false
      }
    } catch {
      Write-TestResult -TestName "PowerShell script syntax valid" -Success $false -ErrorMessage $_.Exception.Message
      return $false
    }
  }
  
  return $true
}

function Test-CommandAlias {
  param(
    [string]$AliasName,
    [string]$ExpectedCommand,
    [string]$ScriptPath
  )
  
  try {
    # Source the script
    . $ScriptPath
    
    # Check if alias exists
    $alias = Get-Alias -Name $AliasName -ErrorAction SilentlyContinue
    
    if ($alias) {
      Write-TestResult -TestName "Alias '$AliasName' exists" -Success $true
      return $true
    } else {
      Write-TestResult -TestName "Alias '$AliasName' exists" -Success $false -ErrorMessage "Alias not defined"
      return $false
    }
  } catch {
    # Alias might be a function, not an alias
    $function = Get-Command -Name $AliasName -ErrorAction SilentlyContinue
    if ($function) {
      Write-TestResult -TestName "Command '$AliasName' exists" -Success $true
      return $true
    } else {
      Write-TestResult -TestName "Command '$AliasName' exists" -Success $false -ErrorMessage "Neither alias nor function found"
      return $false
    }
  }
}

# Main test execution
Write-Host "`n🧪 Running Agent OS Command Dashboard Tests" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# Test 1: Check if dashboard.md exists
Write-Host "`n📋 Testing Dashboard Documentation:" -ForegroundColor Yellow
$dashboardPath = Join-Path (Join-Path $PSScriptRoot "..") "dashboard.md"
Test-FileExists -FilePath $dashboardPath -Description "dashboard.md exists"

# Test 2: Validate dashboard content
if (Test-Path $dashboardPath) {
  Test-DashboardContent -DashboardPath $dashboardPath
}

# Test 3: Check if PowerShell script exists
Write-Host "`n🔧 Testing PowerShell Script:" -ForegroundColor Yellow
$ps1Path = Join-Path (Join-Path $PSScriptRoot "..") "aos-commands.ps1"
Test-FileExists -FilePath $ps1Path -Description "aos-commands.ps1 exists"

# Test 4: Validate PowerShell script syntax
if (Test-Path $ps1Path) {
  Test-ShellScriptSyntax -ScriptPath $ps1Path
  
  # Test 5: Check for required functions
  Write-Host "`n📦 Testing PowerShell Functions:" -ForegroundColor Yellow
  $requiredFunctions = @(
    "aos-spec",
    "aos-tasks", 
    "aos-execute",
    "aos-review",
    "aos-git",
    "aos-help"
  )
  
  foreach ($func in $requiredFunctions) {
    Test-CommandFunction -FunctionName $func -ScriptPath $ps1Path
  }
  
  # Test 6: Check for aliases
  Write-Host "`n🔗 Testing Command Aliases:" -ForegroundColor Yellow
  Test-CommandAlias -AliasName "aos" -ExpectedCommand "aos-help" -ScriptPath $ps1Path
}

# Test 7: Check if Bash script exists
Write-Host "`n🐧 Testing Bash Script:" -ForegroundColor Yellow
$shPath = Join-Path (Join-Path $PSScriptRoot "..") "aos-commands.sh"
Test-FileExists -FilePath $shPath -Description "aos-commands.sh exists"

# Test 8: Validate Bash script syntax
if (Test-Path $shPath) {
  Test-ShellScriptSyntax -ScriptPath $shPath
}

# Test 9: Check installation instructions
Write-Host "`n📝 Testing Installation Instructions:" -ForegroundColor Yellow
if (Test-Path $dashboardPath) {
  $content = Get-Content $dashboardPath -Raw
  $hasWindowsInstructions = ($content -match "PowerShell") -or ($content -match "Windows") -or ($content -match "\.ps1")
  $hasUnixInstructions = ($content -match "bash") -or ($content -match "zsh") -or ($content -match "\.bashrc") -or ($content -match "\.zshrc")
  
  Write-TestResult -TestName "Windows installation instructions present" -Success $hasWindowsInstructions
  Write-TestResult -TestName "Unix/Linux installation instructions present" -Success $hasUnixInstructions
}

# Summary
Write-Host "`n" "=" * 50 -ForegroundColor Cyan
Write-Host "📊 Test Summary:" -ForegroundColor Cyan
Write-Host "   Passed: $($TestResults.Passed)" -ForegroundColor Green
Write-Host "   Failed: $($TestResults.Failed)" -ForegroundColor $(if ($TestResults.Failed -gt 0) { "Red" } else { "Green" })

if ($TestResults.Failed -gt 0) {
  Write-Host "`n❌ Test suite failed with $($TestResults.Failed) error(s)" -ForegroundColor Red
  if ($Verbose) {
    Write-Host "`nError Details:" -ForegroundColor Yellow
    $TestResults.Errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
  }
  exit 1
} else {
  Write-Host "`n✅ All tests passed!" -ForegroundColor Green
  exit 0
}