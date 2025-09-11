# Agent OS Command Dashboard

A centralized reference for all Agent OS commands and workflows. This dashboard provides quick access to common operations for product analysis, specification creation, task management, and execution.

## Quick Commands

### Essential Commands
```bash
# Initialize Agent OS in a new project
aos-init

# Get help and command list
aos-help

# Execute tasks for current spec
aos-execute

# Review and commit changes
aos-review
```

### Workflow Commands
```bash
# Create a new spec
aos-spec "feature-name"

# Create tasks from spec
aos-tasks

# Execute tasks
aos-execute

# Git workflow (commit, push, PR)
aos-git
```

## Installation

### Windows (PowerShell)

1. Open PowerShell and navigate to your project:
```powershell
cd C:\path\to\your\project
```

2. Source the Agent OS commands:
```powershell
. .agent-os\tools\aos-commands.ps1
```

3. (Optional) Add to your PowerShell profile for permanent access:
```powershell
# Open your profile
notepad $PROFILE

# Add this line to the profile
. C:\path\to\your\project\.agent-os\tools\aos-commands.ps1
```

### Unix/Linux/macOS (Bash/Zsh)

1. Open terminal and navigate to your project:
```bash
cd /path/to/your/project
```

2. Source the Agent OS commands:
```bash
source .agents/.agent-os/tools/aos-commands.sh
```

3. (Optional) Add to your shell profile for permanent access:
```bash
# For bash
echo "source /path/to/your/project/.agents/.agent-os/tools/aos-commands.sh" >> ~/.bashrc

# For zsh
echo "source /path/to/your/project/.agents/.agent-os/tools/aos-commands.sh" >> ~/.zshrc
```

## Command Reference

### Product Analysis Commands

#### `aos-analyze`
Analyze the current codebase and product structure
```bash
aos-analyze
```
- Examines project architecture
- Identifies patterns and conventions
- Generates analysis report

#### `aos-init`
Initialize Agent OS in a new project
```bash
aos-init
```
- Installs Agent OS structure
- Creates necessary directories
- Sets up configuration files

### Specification Commands

#### `aos-spec [name]`
Create a new specification for a feature
```bash
aos-spec "user-authentication"
aos-spec "payment-integration"
```
- Creates spec folder with date prefix
- Generates spec.md template
- Sets up sub-specs directory

#### `aos-spec-list`
List all existing specifications
```bash
aos-spec-list
```
- Shows all specs in chronological order
- Displays completion status
- Shows associated GitHub issues

### Task Management Commands

#### `aos-tasks`
Create tasks from the current specification
```bash
aos-tasks
```
- Reads current spec.md
- Generates tasks.md with breakdown
- Creates GitHub issues if configured

#### `aos-tasks-status`
Check status of tasks for current spec
```bash
aos-tasks-status
```
- Shows completed vs pending tasks
- Displays blocking issues
- Provides completion percentage

### Execution Commands

#### `aos-execute [task-number]`
Execute tasks for the current specification
```bash
aos-execute        # Execute next uncompleted task
aos-execute 1      # Execute specific task
aos-execute 1-3    # Execute task range
```
- Runs test-driven development workflow
- Updates task status automatically
- Handles blocking issues

#### `aos-test`
Run tests for the current feature
```bash
aos-test
```
- Executes task-specific tests
- Shows detailed failure analysis
- Suggests fixes for failures

### Git Workflow Commands

#### `aos-git`
Complete git workflow (commit, push, PR)
```bash
aos-git
```

#### `aos-github-workflow`
GitHub workflow best practices management
```bash
aos-github-workflow help          # Show help and best practices
aos-github-workflow check         # Check current workflow status
aos-github-workflow validate      # Validate PR-issue relationships
aos-github-workflow fix-issue 177 # Fix improperly closed issue
```
- Ensures proper issue-PR workflow
- Validates GitHub best practices
- Fixes broken project board flows
- Creates semantic commits
- Pushes to feature branch
- Creates pull request with template

#### `aos-commit`
Create a semantic commit for current changes
```bash
aos-commit
```
- Analyzes changes
- Suggests commit message
- Follows conventional commits format

#### `aos-pr`
Create a pull request for current feature
```bash
aos-pr
```
- Generates PR description from spec
- Links related issues
- Adds test results

### Review Commands

#### `aos-review`
Review current work before committing
```bash
aos-review
```
- Checks code style compliance
- Runs linting and formatting
- Verifies test coverage

#### `aos-review-spec`
Review specification for completeness
```bash
aos-review-spec
```
- Validates spec structure
- Checks for missing sections
- Ensures clarity and completeness

### Utility Commands

#### `aos-help`
Display help and available commands
```bash
aos-help
aos-help [command]  # Get help for specific command
```

#### `aos-status`
Show current Agent OS status
```bash
aos-status
```
- Current spec and task
- Git branch information
- Recent activity

#### `aos-clean`
Clean up temporary files and caches
```bash
aos-clean
```
- Removes generated files
- Clears learning cache
- Resets status tracking

#### `aos-update`
Update Agent OS to latest version
```bash
aos-update
```
- Fetches latest instructions
- Updates command definitions
- Migrates configuration if needed

## Command Aliases

Short aliases for frequently used commands:

| Alias | Full Command | Description |
|-------|-------------|-------------|
| `aos` | `aos-help` | Show help |
| `aosi` | `aos-init` | Initialize Agent OS |
| `aoss` | `aos-spec` | Create specification |
| `aost` | `aos-tasks` | Create tasks |
| `aose` | `aos-execute` | Execute tasks |
| `aosr` | `aos-review` | Review changes |
| `aosg` | `aos-git` | Git workflow |
| `aosgh` | `aos-github-workflow` | GitHub workflow best practices |
| `aosst` | `aos-status` | Show status |

## Environment Variables

Configure Agent OS behavior with environment variables:

```bash
# Set GitHub token for issue creation
export AOS_GITHUB_TOKEN="your-token"

# Set default PR base branch
export AOS_BASE_BRANCH="main"

# Enable verbose output
export AOS_VERBOSE="true"

# Set custom Agent OS directory
export AOS_DIR=".custom-agent-os"
```

## Troubleshooting

### Command Not Found
If commands are not recognized:
1. Ensure you've sourced the appropriate script
2. Check that the .agents/.agent-os/tools directory exists
3. Verify script permissions (Unix/Linux)

### Permission Denied (Unix/Linux)
```bash
chmod +x .agents/.agent-os/tools/aos-commands.sh
```

### Profile Not Loading (Windows)
```powershell
# Check execution policy
Get-ExecutionPolicy

# Set to RemoteSigned if needed
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Advanced Usage

### Chaining Commands
```bash
# Complete workflow in one line
aos-spec "new-feature" && aos-tasks && aos-execute && aos-git
```

### Custom Workflows
Create custom command combinations:
```bash
# Define in your shell profile
alias aos-feature='aos-spec && aos-tasks && aos-execute'
```

### Integration with CI/CD
```yaml
# GitHub Actions example
- name: Execute Agent OS Tasks
  run: |
    source .agents/.agent-os/tools/aos-commands.sh
    aos-execute
    aos-test
```

## Best Practices

1. **Always review before committing**: Use `aos-review` before `aos-git`
2. **Keep specs focused**: One feature per specification
3. **Update task status**: Mark tasks complete as you work
4. **Document blockers**: Use blocking issue tracking for problems
5. **Use semantic commits**: Let Agent OS generate commit messages

## Getting Help

- **Documentation**: `.agents/.agent-os/docs/`
- **GitHub Issues**: Report bugs and request features
- **Learning Cache**: `.agents/.agent-os/learning/patterns.md`
- **Command Help**: `aos-help [command]`

---

*Agent OS Command Dashboard v1.0 - Productivity through automation*