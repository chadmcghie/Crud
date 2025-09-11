# Agent OS Commands for Windows PowerShell
# Dot-source this file to access Agent OS commands in PowerShell

# Get the directory where this script is located
$AOS_TOOLS_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$AOS_ROOT_DIR = Split-Path -Parent (Split-Path -Parent $AOS_TOOLS_DIR)
$AOS_DIR = Join-Path $AOS_ROOT_DIR ".agent-os"

# Helper function to print colored output
function AOS-Print {
  param(
    [string]$Message,
    [string]$Color = "White"
  )
  Write-Host $Message -ForegroundColor $Color
}

# Helper function to check if we're in a project with Agent OS
function AOS-CheckProject {
  if (-not (Test-Path $AOS_DIR)) {
    AOS-Print "Error: Agent OS not found in current project" "Red"
    AOS-Print "Run 'aos-init' to initialize Agent OS" "Yellow"
    return $false
  }
  return $true
}

# Helper function to get current spec
function AOS-GetCurrentSpec {
  $specDir = Join-Path $AOS_DIR "specs"
  if (-not (Test-Path $specDir)) {
    return $null
  }
  
  # Find the most recent spec directory
  $latestSpec = Get-ChildItem -Path $specDir -Directory | 
    Sort-Object Name -Descending | 
    Select-Object -First 1
  
  if ($latestSpec) {
    return $latestSpec.Name
  }
  return $null
}

# Initialize Agent OS in a new project
function aos-init {
  AOS-Print "Initializing Agent OS..." "Cyan"
  
  # Check if Agent OS already exists
  if (Test-Path $AOS_DIR) {
    AOS-Print "Agent OS already initialized in this project" "Yellow"
    return
  }
  
  # Create directory structure
  $directories = @(
    "instructions",
    "standards",
    "specs",
    "learning",
    "status",
    "tools",
    "templates"
  )
  
  foreach ($dir in $directories) {
    New-Item -Path (Join-Path $AOS_DIR $dir) -ItemType Directory -Force | Out-Null
  }
  
  AOS-Print "Agent OS initialized successfully!" "Green"
  AOS-Print "Next steps:" "Blue"
  AOS-Print "  1. Run 'aos-analyze' to analyze your codebase"
  AOS-Print "  2. Run 'aos-spec <feature-name>' to create your first specification"
}

# Analyze the current codebase
function aos-analyze {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Analyzing codebase..." "Cyan"
  AOS-Print "This would invoke: claude --command /analyze-product" "Yellow"
  Write-Output "Please run: claude --command /analyze-product"
}

# Create a new specification
function aos-spec {
  param(
    [string]$SpecName
  )
  
  if (-not (AOS-CheckProject)) { return }
  
  if ([string]::IsNullOrWhiteSpace($SpecName)) {
    AOS-Print "Error: Please provide a specification name" "Red"
    AOS-Print "Usage: aos-spec <feature-name>"
    return
  }
  
  AOS-Print "Creating specification: $SpecName" "Cyan"
  AOS-Print "This would invoke: claude --command /create-spec $SpecName" "Yellow"
  Write-Output "Please run: claude --command /create-spec `"$SpecName`""
}

# List all specifications
function aos-spec-list {
  if (-not (AOS-CheckProject)) { return }
  
  $specDir = Join-Path $AOS_DIR "specs"
  if (-not (Test-Path $specDir)) {
    AOS-Print "No specifications found" "Yellow"
    return
  }
  
  AOS-Print "Available specifications:" "Cyan"
  
  Get-ChildItem -Path $specDir -Directory | ForEach-Object {
    $name = $_.Name
    $tasksFile = Join-Path $_.FullName "tasks.md"
    
    if (Test-Path $tasksFile) {
      $content = Get-Content $tasksFile -Raw
      $completed = ([regex]::Matches($content, "^\- \[x\]", "Multiline")).Count
      $total = ([regex]::Matches($content, "^\- \[.\]", "Multiline")).Count
      AOS-Print "  - $name ($completed/$total tasks completed)"
    } else {
      AOS-Print "  - $name (no tasks created)"
    }
  }
}

# Create tasks from specification
function aos-tasks {
  if (-not (AOS-CheckProject)) { return }
  
  $currentSpec = AOS-GetCurrentSpec
  if (-not $currentSpec) {
    AOS-Print "Error: No specification found" "Red"
    AOS-Print "Run 'aos-spec <feature-name>' to create a specification first" "Yellow"
    return
  }
  
  AOS-Print "Creating tasks for spec: $currentSpec" "Cyan"
  AOS-Print "This would invoke: claude --command /create-tasks" "Yellow"
  Write-Output "Please run: claude --command /create-tasks"
}

# Check task status
function aos-tasks-status {
  if (-not (AOS-CheckProject)) { return }
  
  $currentSpec = AOS-GetCurrentSpec
  if (-not $currentSpec) {
    AOS-Print "Error: No specification found" "Red"
    return
  }
  
  $tasksFile = Join-Path (Join-Path (Join-Path $AOS_DIR "specs") $currentSpec) "tasks.md"
  if (-not (Test-Path $tasksFile)) {
    AOS-Print "No tasks file found for $currentSpec" "Yellow"
    return
  }
  
  $content = Get-Content $tasksFile -Raw
  $completed = ([regex]::Matches($content, "^\- \[x\]", "Multiline")).Count
  $total = ([regex]::Matches($content, "^\- \[.\]", "Multiline")).Count
  $percentage = if ($total -gt 0) { [math]::Round(($completed / $total) * 100) } else { 0 }
  
  AOS-Print "Task Status for ${currentSpec}:" "Cyan"
  AOS-Print "  Completed: $completed"
  AOS-Print "  Total: $total"
  AOS-Print "  Progress: $percentage%"
  
  # Show any blocking issues
  if ($content -match "Blocking issue:") {
    AOS-Print "`nBlocking issues found:" "Yellow"
    $content -split "`n" | Where-Object { $_ -match "Blocking issue:" } | ForEach-Object {
      AOS-Print "  $_" "Red"
    }
  }
}

# Execute tasks
function aos-execute {
  param(
    [string]$TaskNumber
  )
  
  if (-not (AOS-CheckProject)) { return }
  
  $currentSpec = AOS-GetCurrentSpec
  if (-not $currentSpec) {
    AOS-Print "Error: No specification found" "Red"
    return
  }
  
  if ($TaskNumber) {
    AOS-Print "Executing task $TaskNumber for spec: $currentSpec" "Cyan"
    AOS-Print "This would invoke: claude --command /execute-tasks $TaskNumber" "Yellow"
    Write-Output "Please run: claude --command /execute-tasks $TaskNumber"
  } else {
    AOS-Print "Executing next task for spec: $currentSpec" "Cyan"
    AOS-Print "This would invoke: claude --command /execute-tasks" "Yellow"
    Write-Output "Please run: claude --command /execute-tasks"
  }
}

# Run tests for current feature
function aos-test {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Running tests for current feature..." "Cyan"
  
  # Check for test commands in package.json or project files
  if (Test-Path "package.json") {
    npm test
  } elseif (Test-Path "*.sln") {
    dotnet test
  } elseif (Test-Path "Makefile") {
    make test
  } else {
    AOS-Print "No test command found. Please run tests manually." "Yellow"
  }
}

# Review current work
function aos-review {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Reviewing current work..." "Cyan"
  
  $hasIssues = $false
  
  # Check for .NET formatting
  if ((Get-ChildItem -Filter "*.sln" -ErrorAction SilentlyContinue) -or 
      (Get-ChildItem -Filter "*.csproj" -ErrorAction SilentlyContinue)) {
    AOS-Print "Running .NET formatter..." "Blue"
    $result = & dotnet format --verify-no-changes 2>&1
    if ($LASTEXITCODE -ne 0) {
      AOS-Print "Code formatting issues found. Run 'dotnet format' to fix." "Yellow"
      $hasIssues = $true
    }
  }
  
  # Check for npm/yarn
  if (Test-Path "package.json") {
    if (Test-Path "yarn.lock") {
      AOS-Print "Running yarn lint..." "Blue"
      yarn lint
      if ($LASTEXITCODE -ne 0) {
        $hasIssues = $true
      }
    } else {
      AOS-Print "Running npm lint..." "Blue"
      npm run lint
      if ($LASTEXITCODE -ne 0) {
        $hasIssues = $true
      }
    }
  }
  
  if (-not $hasIssues) {
    AOS-Print "Review complete! No issues found." "Green"
  } else {
    AOS-Print "Review complete. Please fix the issues above." "Yellow"
  }
}

# Git workflow
function aos-git {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Starting git workflow..." "Cyan"
  AOS-Print "This would invoke: claude --command /git-workflow" "Yellow"
  Write-Output "Please run: claude --command /git-workflow"
}

# Create semantic commit
function aos-commit {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Creating semantic commit..." "Cyan"
  
  # Check for unstaged changes
  $unstaged = git diff --quiet 2>&1
  if ($LASTEXITCODE -ne 0) {
    AOS-Print "You have unstaged changes. Add them first with 'git add'" "Yellow"
    return
  }
  
  # Check for staged changes
  $staged = git diff --cached --quiet 2>&1
  if ($LASTEXITCODE -eq 0) {
    AOS-Print "No staged changes to commit" "Yellow"
    return
  }
  
  AOS-Print "This would analyze changes and create a semantic commit" "Yellow"
  Write-Output "Please run: claude --command /commit"
}

# Create pull request
function aos-pr {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Creating pull request..." "Cyan"
  
  # Check if gh CLI is available
  $ghExists = Get-Command gh -ErrorAction SilentlyContinue
  if (-not $ghExists) {
    AOS-Print "GitHub CLI (gh) not found. Please install it first." "Red"
    return
  }
  
  AOS-Print "This would create a PR with spec details" "Yellow"
  Write-Output "Please run: claude --command /create-pr"
}

# Review specification
function aos-review-spec {
  if (-not (AOS-CheckProject)) { return }
  
  $currentSpec = AOS-GetCurrentSpec
  if (-not $currentSpec) {
    AOS-Print "Error: No specification found" "Red"
    return
  }
  
  AOS-Print "Reviewing specification: $currentSpec" "Cyan"
  
  $specFile = Join-Path (Join-Path (Join-Path $AOS_DIR "specs") $currentSpec) "spec.md"
  if (-not (Test-Path $specFile)) {
    AOS-Print "Spec file not found" "Red"
    return
  }
  
  $content = Get-Content $specFile -Raw
  
  # Check for required sections
  $missingSections = @()
  
  if ($content -notmatch "## Problem Statement") {
    $missingSections += "Problem Statement"
  }
  if ($content -notmatch "## Proposed Solution") {
    $missingSections += "Proposed Solution"
  }
  if ($content -notmatch "## Implementation Plan") {
    $missingSections += "Implementation Plan"
  }
  if ($content -notmatch "## Testing Strategy") {
    $missingSections += "Testing Strategy"
  }
  
  if ($missingSections.Count -eq 0) {
    AOS-Print "Specification structure looks good!" "Green"
  } else {
    AOS-Print "Missing sections:" "Yellow"
    foreach ($section in $missingSections) {
      AOS-Print "  - $section"
    }
  }
}

# Show help
function aos-help {
  param(
    [string]$Command
  )
  
  if ([string]::IsNullOrWhiteSpace($Command)) {
    AOS-Print "Agent OS Commands" "Cyan"
    AOS-Print ""
    AOS-Print "Essential Commands:"
    AOS-Print "  aos-init          Initialize Agent OS in a new project"
    AOS-Print "  aos-help          Show this help message"
    AOS-Print "  aos-execute       Execute tasks for current spec"
    AOS-Print "  aos-review        Review and validate current work"
    AOS-Print ""
    AOS-Print "Workflow Commands:"
    AOS-Print "  aos-spec          Create a new specification"
    AOS-Print "  aos-tasks         Create tasks from specification"
    AOS-Print "  aos-git           Complete git workflow"
    AOS-Print ""
    AOS-Print "Analysis Commands:"
    AOS-Print "  aos-analyze       Analyze codebase structure"
    AOS-Print "  aos-status        Show current status"
    AOS-Print "  aos-spec-list     List all specifications"
    AOS-Print ""
    AOS-Print "For detailed help on a command, use: aos-help <command>"
  } else {
    switch ($Command) {
      "init" {
        AOS-Print "aos-init - Initialize Agent OS" "Cyan"
        AOS-Print "Creates the .agent-os directory structure in your project"
      }
      "spec" {
        AOS-Print "aos-spec <name> - Create a new specification" "Cyan"
        AOS-Print "Creates a new feature specification with the given name"
      }
      "execute" {
        AOS-Print "aos-execute [task-number] - Execute tasks" "Cyan"
        AOS-Print "Executes the next task or a specific task number"
      }
      default {
        AOS-Print "No help available for: $Command" "Yellow"
      }
    }
  }
}

# Show current status
function aos-status {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Agent OS Status" "Cyan"
  AOS-Print ""
  
  # Current spec
  $currentSpec = AOS-GetCurrentSpec
  if ($currentSpec) {
    AOS-Print "Current Spec: $currentSpec"
    
    # Task status
    $tasksFile = Join-Path (Join-Path (Join-Path $AOS_DIR "specs") $currentSpec) "tasks.md"
    if (Test-Path $tasksFile) {
      $content = Get-Content $tasksFile -Raw
      $completed = ([regex]::Matches($content, "^\- \[x\]", "Multiline")).Count
      $total = ([regex]::Matches($content, "^\- \[.\]", "Multiline")).Count
      AOS-Print "Tasks: $completed/$total completed"
    }
  } else {
    AOS-Print "Current Spec: None"
  }
  
  # Git status
  AOS-Print ""
  $branch = git branch --show-current 2>$null
  if ($branch) {
    AOS-Print "Git Branch: $branch"
  } else {
    AOS-Print "Git Branch: not in git repo"
  }
  
  # Recent activity
  $statusFile = Join-Path (Join-Path $AOS_DIR "status") "current-status.md"
  if (Test-Path $statusFile) {
    AOS-Print ""
    AOS-Print "Recent Activity:"
    Get-Content $statusFile -Tail 5 | ForEach-Object {
      AOS-Print "  $_"
    }
  }
}

# Clean temporary files
function aos-clean {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Cleaning Agent OS temporary files..." "Cyan"
  
  # Clean learning cache
  $cacheDir = Join-Path (Join-Path $AOS_DIR "learning") ".cache"
  if (Test-Path $cacheDir) {
    Remove-Item -Path $cacheDir -Recurse -Force
    AOS-Print "  Cleared learning cache"
  }
  
  # Clean status tracking
  $tmpFile = Join-Path (Join-Path $AOS_DIR "status") ".tmp"
  if (Test-Path $tmpFile) {
    Remove-Item -Path $tmpFile -Force
    AOS-Print "  Cleared temporary status"
  }
  
  AOS-Print "Cleanup complete!" "Green"
}

# Update Agent OS
function aos-update {
  if (-not (AOS-CheckProject)) { return }
  
  AOS-Print "Updating Agent OS..." "Cyan"
  AOS-Print "This would fetch latest Agent OS updates" "Yellow"
  AOS-Print "Manual update: Check https://github.com/anthropics/agent-os for updates"
}

# Define aliases
Set-Alias -Name aos -Value aos-help
# GitHub Workflow Best Practices Command
function aos-github-workflow {
  param(
    [string]$Action = "help"
  )
  
  if (-not (AOS-CheckProject)) { return }
  
  switch ($Action.ToLower()) {
    "help" {
      AOS-Print "GitHub Workflow Best Practices" "Cyan"
      AOS-Print "==============================" "Cyan"
      AOS-Print ""
      AOS-Print "Commands:" "Yellow"
      AOS-Print "  aos-github-workflow help          - Show this help"
      AOS-Print "  aos-github-workflow check         - Check current workflow status"
      AOS-Print "  aos-github-workflow fix-issue <#> - Fix improperly closed issue"
      AOS-Print "  aos-github-workflow validate      - Validate PR-issue relationships"
      AOS-Print ""
      AOS-Print "Best Practices:" "Yellow"
      AOS-Print "  • Never close issues directly when work is done"
      AOS-Print "  • Reference issues in PRs with 'Closes #XXX'"
      AOS-Print "  • Let GitHub close issues automatically when PR merges"
      AOS-Print "  • This maintains proper project board flow"
    }
    
    "check" {
      AOS-Print "Checking GitHub workflow status..." "Yellow"
      gh issue list --state open --limit 5
      AOS-Print ""
      AOS-Print "Recent PRs:" "Yellow"
      gh pr list --state open --limit 5
    }
    
    "validate" {
      AOS-Print "Validating PR-issue relationships..." "Yellow"
      $openPRs = gh pr list --state open --json number,title,body
      foreach ($pr in $openPRs) {
        if ($pr.body -match "Closes #(\d+)") {
          AOS-Print "✅ PR #$($pr.number) properly references issue #$($matches[1])" "Green"
        } else {
          AOS-Print "⚠️  PR #$($pr.number) missing issue reference" "Yellow"
        }
      }
    }
    
    default {
      if ($Action.StartsWith("fix-issue")) {
        $issueNumber = $Action.Split(" ")[1]
        if ($issueNumber) {
          AOS-Print "Fixing issue #$issueNumber workflow..." "Yellow"
          gh issue reopen $issueNumber --comment "Reopening to properly close via PR workflow"
          AOS-Print "Issue #$issueNumber reopened. Now reference it in your PR with 'Closes #$issueNumber'" "Green"
        } else {
          AOS-Print "Usage: aos-github-workflow fix-issue <number>" "Red"
        }
      } else {
        AOS-Print "Unknown action: $Action" "Red"
        AOS-Print "Run 'aos-github-workflow help' for available commands" "Yellow"
      }
    }
  }
}

Set-Alias -Name aosi -Value aos-init
Set-Alias -Name aoss -Value aos-spec
Set-Alias -Name aost -Value aos-tasks
Set-Alias -Name aose -Value aos-execute
Set-Alias -Name aosr -Value aos-review
Set-Alias -Name aosg -Value aos-git
Set-Alias -Name aosst -Value aos-status
Set-Alias -Name aosgh -Value aos-github-workflow

# Print success message
AOS-Print "Agent OS commands loaded successfully!" "Green"
AOS-Print "Type 'aos-help' to see available commands"