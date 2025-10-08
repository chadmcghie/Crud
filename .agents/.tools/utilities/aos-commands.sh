#!/bin/bash
# Agent OS Commands for Unix/Linux/macOS
# Source this file to access Agent OS commands in your shell

# Get the directory where this script is located
AOS_TOOLS_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
AOS_ROOT_DIR="$( cd "$AOS_TOOLS_DIR/../.." && pwd )"
AOS_DIR="$AOS_ROOT_DIR/.agent-os"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Helper function to print colored output
aos_print() {
  local color=$1
  shift
  echo -e "${color}$@${NC}"
}

# Helper function to check if we're in a project with Agent OS
aos_check_project() {
  if [ ! -d "$AOS_DIR" ]; then
    aos_print $RED "Error: Agent OS not found in current project"
    aos_print $YELLOW "Run 'aos-init' to initialize Agent OS"
    return 1
  fi
  return 0
}

# Helper function to get current spec
aos_get_current_spec() {
  local spec_dir="$AOS_DIR/specs"
  if [ ! -d "$spec_dir" ]; then
    return 1
  fi
  
  # Find the most recent spec directory
  local latest_spec=$(ls -d "$spec_dir"/*/ 2>/dev/null | sort -r | head -n1)
  if [ -n "$latest_spec" ]; then
    basename "$latest_spec"
  else
    return 1
  fi
}

# Initialize Agent OS in a new project
aos-init() {
  aos_print $CYAN "Initializing Agent OS..."
  
  # Check if Agent OS already exists
  if [ -d "$AOS_DIR" ]; then
    aos_print $YELLOW "Agent OS already initialized in this project"
    return 1
  fi
  
  # Create directory structure
  mkdir -p "$AOS_DIR"/{instructions,standards,specs,learning,status,tools,templates}
  
  aos_print $GREEN "Agent OS initialized successfully!"
  aos_print $BLUE "Next steps:"
  aos_print $NC "  1. Run 'aos-analyze' to analyze your codebase"
  aos_print $NC "  2. Run 'aos-spec <feature-name>' to create your first specification"
}

# Analyze the current codebase
aos-analyze() {
  aos_check_project || return 1
  
  aos_print $CYAN "Analyzing codebase..."
  aos_print $YELLOW "This would invoke: claude --command /analyze-product"
  echo "Please run: claude --command /analyze-product"
}

# Create a new specification
aos-spec() {
  aos_check_project || return 1
  
  local spec_name="$1"
  if [ -z "$spec_name" ]; then
    aos_print $RED "Error: Please provide a specification name"
    aos_print $NC "Usage: aos-spec <feature-name>"
    return 1
  fi
  
  aos_print $CYAN "Creating specification: $spec_name"
  aos_print $YELLOW "This would invoke: claude --command /create-spec $spec_name"
  echo "Please run: claude --command /create-spec \"$spec_name\""
}

# List all specifications
aos-spec-list() {
  aos_check_project || return 1
  
  local spec_dir="$AOS_DIR/specs"
  if [ ! -d "$spec_dir" ]; then
    aos_print $YELLOW "No specifications found"
    return 0
  fi
  
  aos_print $CYAN "Available specifications:"
  for spec in "$spec_dir"/*; do
    if [ -d "$spec" ]; then
      local name=$(basename "$spec")
      local tasks_file="$spec/tasks.md"
      if [ -f "$tasks_file" ]; then
        local completed=$(grep -c "^\- \[x\]" "$tasks_file" 2>/dev/null || echo 0)
        local total=$(grep -c "^\- \[.\]" "$tasks_file" 2>/dev/null || echo 0)
        aos_print $NC "  - $name ($completed/$total tasks completed)"
      else
        aos_print $NC "  - $name (no tasks created)"
      fi
    fi
  done
}

# Create tasks from specification
aos-tasks() {
  aos_check_project || return 1
  
  local current_spec=$(aos_get_current_spec)
  if [ -z "$current_spec" ]; then
    aos_print $RED "Error: No specification found"
    aos_print $YELLOW "Run 'aos-spec <feature-name>' to create a specification first"
    return 1
  fi
  
  aos_print $CYAN "Creating tasks for spec: $current_spec"
  aos_print $YELLOW "This would invoke: claude --command /create-tasks"
  echo "Please run: claude --command /create-tasks"
}

# Check task status
aos-tasks-status() {
  aos_check_project || return 1
  
  local current_spec=$(aos_get_current_spec)
  if [ -z "$current_spec" ]; then
    aos_print $RED "Error: No specification found"
    return 1
  fi
  
  local tasks_file="$AOS_DIR/specs/$current_spec/tasks.md"
  if [ ! -f "$tasks_file" ]; then
    aos_print $YELLOW "No tasks file found for $current_spec"
    return 1
  fi
  
  local completed=$(grep -c "^\- \[x\]" "$tasks_file" 2>/dev/null || echo 0)
  local total=$(grep -c "^\- \[.\]" "$tasks_file" 2>/dev/null || echo 0)
  local percentage=0
  if [ $total -gt 0 ]; then
    percentage=$((completed * 100 / total))
  fi
  
  aos_print $CYAN "Task Status for $current_spec:"
  aos_print $NC "  Completed: $completed"
  aos_print $NC "  Total: $total"
  aos_print $NC "  Progress: $percentage%"
  
  # Show any blocking issues
  if grep -q "Blocking issue:" "$tasks_file"; then
    aos_print $YELLOW "\nBlocking issues found:"
    grep "Blocking issue:" "$tasks_file" | while read -r line; do
      aos_print $RED "  $line"
    done
  fi
}

# Execute tasks
aos-execute() {
  aos_check_project || return 1
  
  local task_number="$1"
  local current_spec=$(aos_get_current_spec)
  
  if [ -z "$current_spec" ]; then
    aos_print $RED "Error: No specification found"
    return 1
  fi
  
  if [ -n "$task_number" ]; then
    aos_print $CYAN "Executing task $task_number for spec: $current_spec"
    aos_print $YELLOW "This would invoke: claude --command /execute-tasks $task_number"
    echo "Please run: claude --command /execute-tasks $task_number"
  else
    aos_print $CYAN "Executing next task for spec: $current_spec"
    aos_print $YELLOW "This would invoke: claude --command /execute-tasks"
    echo "Please run: claude --command /execute-tasks"
  fi
}

# Run tests for current feature
aos-test() {
  aos_check_project || return 1
  
  aos_print $CYAN "Running tests for current feature..."
  
  # Check for test commands in package.json or project files
  if [ -f "package.json" ]; then
    npm test
  elif [ -f "Makefile" ] && grep -q "^test:" "Makefile"; then
    make test
  else
    aos_print $YELLOW "No test command found. Please run tests manually."
  fi
}

# Review current work
aos-review() {
  aos_check_project || return 1
  
  aos_print $CYAN "Reviewing current work..."
  
  # Run available linters and formatters
  local has_issues=0
  
  # Check for .NET formatting
  if [ -f "*.sln" ] || [ -f "*.csproj" ]; then
    aos_print $BLUE "Running .NET formatter..."
    if ! dotnet format --verify-no-changes 2>/dev/null; then
      aos_print $YELLOW "Code formatting issues found. Run 'dotnet format' to fix."
      has_issues=1
    fi
  fi
  
  # Check for npm/yarn
  if [ -f "package.json" ]; then
    if [ -f "yarn.lock" ]; then
      aos_print $BLUE "Running yarn lint..."
      yarn lint || has_issues=1
    else
      aos_print $BLUE "Running npm lint..."
      npm run lint || has_issues=1
    fi
  fi
  
  if [ $has_issues -eq 0 ]; then
    aos_print $GREEN "Review complete! No issues found."
  else
    aos_print $YELLOW "Review complete. Please fix the issues above."
  fi
}

# Git workflow
aos-git() {
  aos_check_project || return 1
  
  aos_print $CYAN "Starting git workflow..."
  aos_print $YELLOW "This would invoke: claude --command /git-workflow"
  echo "Please run: claude --command /git-workflow"
}

# Create semantic commit
aos-commit() {
  aos_check_project || return 1
  
  aos_print $CYAN "Creating semantic commit..."
  
  # Check for unstaged changes
  if ! git diff --quiet; then
    aos_print $YELLOW "You have unstaged changes. Add them first with 'git add'"
    return 1
  fi
  
  # Check for staged changes
  if git diff --cached --quiet; then
    aos_print $YELLOW "No staged changes to commit"
    return 1
  fi
  
  aos_print $YELLOW "This would analyze changes and create a semantic commit"
  echo "Please run: claude --command /commit"
}

# Create pull request
aos-pr() {
  aos_check_project || return 1
  
  aos_print $CYAN "Creating pull request..."
  
  # Check if gh CLI is available
  if ! command -v gh &> /dev/null; then
    aos_print $RED "GitHub CLI (gh) not found. Please install it first."
    return 1
  fi
  
  aos_print $YELLOW "This would create a PR with spec details"
  echo "Please run: claude --command /create-pr"
}

# Review specification
aos-review-spec() {
  aos_check_project || return 1
  
  local current_spec=$(aos_get_current_spec)
  if [ -z "$current_spec" ]; then
    aos_print $RED "Error: No specification found"
    return 1
  fi
  
  aos_print $CYAN "Reviewing specification: $current_spec"
  
  local spec_file="$AOS_DIR/specs/$current_spec/spec.md"
  if [ ! -f "$spec_file" ]; then
    aos_print $RED "Spec file not found"
    return 1
  fi
  
  # Check for required sections
  local missing_sections=()
  
  grep -q "## Problem Statement" "$spec_file" || missing_sections+=("Problem Statement")
  grep -q "## Proposed Solution" "$spec_file" || missing_sections+=("Proposed Solution")
  grep -q "## Implementation Plan" "$spec_file" || missing_sections+=("Implementation Plan")
  grep -q "## Testing Strategy" "$spec_file" || missing_sections+=("Testing Strategy")
  
  if [ ${#missing_sections[@]} -eq 0 ]; then
    aos_print $GREEN "Specification structure looks good!"
  else
    aos_print $YELLOW "Missing sections:"
    for section in "${missing_sections[@]}"; do
      aos_print $NC "  - $section"
    done
  fi
}

# Show help
aos-help() {
  local command="$1"
  
  if [ -z "$command" ]; then
    aos_print $CYAN "Agent OS Commands"
    aos_print $NC ""
    aos_print $NC "Essential Commands:"
    aos_print $NC "  aos-init          Initialize Agent OS in a new project"
    aos_print $NC "  aos-help          Show this help message"
    aos_print $NC "  aos-execute       Execute tasks for current spec"
    aos_print $NC "  aos-review        Review and validate current work"
    aos_print $NC ""
    aos_print $NC "Workflow Commands:"
    aos_print $NC "  aos-spec          Create a new specification"
    aos_print $NC "  aos-tasks         Create tasks from specification"
    aos_print $NC "  aos-git           Complete git workflow"
    aos_print $NC ""
    aos_print $NC "Analysis Commands:"
    aos_print $NC "  aos-analyze       Analyze codebase structure"
    aos_print $NC "  aos-status        Show current status"
    aos_print $NC "  aos-spec-list     List all specifications"
    aos_print $NC ""
    aos_print $NC "For detailed help on a command, use: aos-help <command>"
  else
    case "$command" in
      "init")
        aos_print $CYAN "aos-init - Initialize Agent OS"
        aos_print $NC "Creates the .agent-os directory structure in your project"
        ;;
      "spec")
        aos_print $CYAN "aos-spec <name> - Create a new specification"
        aos_print $NC "Creates a new feature specification with the given name"
        ;;
      "execute")
        aos_print $CYAN "aos-execute [task-number] - Execute tasks"
        aos_print $NC "Executes the next task or a specific task number"
        ;;
      *)
        aos_print $YELLOW "No help available for: $command"
        ;;
    esac
  fi
}

# Show current status
aos-status() {
  aos_check_project || return 1
  
  aos_print $CYAN "Agent OS Status"
  aos_print $NC ""
  
  # Current spec
  local current_spec=$(aos_get_current_spec)
  if [ -n "$current_spec" ]; then
    aos_print $NC "Current Spec: $current_spec"
    
    # Task status
    local tasks_file="$AOS_DIR/specs/$current_spec/tasks.md"
    if [ -f "$tasks_file" ]; then
      local completed=$(grep -c "^\- \[x\]" "$tasks_file" 2>/dev/null || echo 0)
      local total=$(grep -c "^\- \[.\]" "$tasks_file" 2>/dev/null || echo 0)
      aos_print $NC "Tasks: $completed/$total completed"
    fi
  else
    aos_print $NC "Current Spec: None"
  fi
  
  # Git status
  aos_print $NC ""
  aos_print $NC "Git Branch: $(git branch --show-current 2>/dev/null || echo 'not in git repo')"
  
  # Recent activity
  local status_file="$AOS_DIR/status/current-status.md"
  if [ -f "$status_file" ]; then
    aos_print $NC ""
    aos_print $NC "Recent Activity:"
    tail -n 5 "$status_file" | while read -r line; do
      aos_print $NC "  $line"
    done
  fi
}

# Clean temporary files
aos-clean() {
  aos_check_project || return 1
  
  aos_print $CYAN "Cleaning Agent OS temporary files..."
  
  # Clean learning cache
  if [ -d "$AOS_DIR/learning/.cache" ]; then
    rm -rf "$AOS_DIR/learning/.cache"
    aos_print $NC "  Cleared learning cache"
  fi
  
  # Clean status tracking
  if [ -f "$AOS_DIR/status/.tmp" ]; then
    rm -f "$AOS_DIR/status/.tmp"
    aos_print $NC "  Cleared temporary status"
  fi
  
  aos_print $GREEN "Cleanup complete!"
}

# Update Agent OS
aos-update() {
  aos_check_project || return 1
  
  aos_print $CYAN "Updating Agent OS..."
  aos_print $YELLOW "This would fetch latest Agent OS updates"
  aos_print $NC "Manual update: Check https://github.com/anthropics/agent-os for updates"
}

# GitHub Workflow Best Practices Command
aos-github-workflow() {
  local action="${1:-help}"
  
  if ! aos_check_project; then
    return 1
  fi
  
  case "$action" in
    "help")
      aos_print $CYAN "GitHub Workflow Best Practices"
      aos_print $CYAN "=============================="
      echo
      aos_print $YELLOW "Commands:"
      aos_print $NC "  aos-github-workflow help          - Show this help"
      aos_print $NC "  aos-github-workflow check         - Check current workflow status"
      aos_print $NC "  aos-github-workflow fix-issue <#> - Fix improperly closed issue"
      aos_print $NC "  aos-github-workflow validate      - Validate PR-issue relationships"
      echo
      aos_print $YELLOW "Best Practices:"
      aos_print $NC "  • Never close issues directly when work is done"
      aos_print $NC "  • Reference issues in PRs with 'Closes #XXX'"
      aos_print $NC "  • Let GitHub close issues automatically when PR merges"
      aos_print $NC "  • This maintains proper project board flow"
      ;;
      
    "check")
      aos_print $YELLOW "Checking GitHub workflow status..."
      gh issue list --state open --limit 5
      echo
      aos_print $YELLOW "Recent PRs:"
      gh pr list --state open --limit 5
      ;;
      
    "validate")
      aos_print $YELLOW "Validating PR-issue relationships..."
      local prs=$(gh pr list --state open --json number,title,body)
      echo "$prs" | jq -r '.[] | "\(.number)|\(.title)|\(.body)"' | while IFS='|' read -r number title body; do
        if echo "$body" | grep -q "Closes #"; then
          aos_print $GREEN "✅ PR #$number properly references issue"
        else
          aos_print $YELLOW "⚠️  PR #$number missing issue reference"
        fi
      done
      ;;
      
    "fix-issue")
      local issue_number="$2"
      if [ -n "$issue_number" ]; then
        aos_print $YELLOW "Fixing issue #$issue_number workflow..."
        gh issue reopen "$issue_number" --comment "Reopening to properly close via PR workflow"
        aos_print $GREEN "Issue #$issue_number reopened. Now reference it in your PR with 'Closes #$issue_number'"
      else
        aos_print $RED "Usage: aos-github-workflow fix-issue <number>"
      fi
      ;;
      
    *)
      aos_print $RED "Unknown action: $action"
      aos_print $YELLOW "Run 'aos-github-workflow help' for available commands"
      ;;
  esac
}

# Define aliases
alias aos='aos-help'
alias aosi='aos-init'
alias aoss='aos-spec'
alias aost='aos-tasks'
alias aose='aos-execute'
alias aosr='aos-review'
alias aosg='aos-git'
alias aosst='aos-status'
alias aosgh='aos-github-workflow'

# Print success message
aos_print $GREEN "Agent OS commands loaded successfully!"
aos_print $NC "Type 'aos-help' to see available commands"