# Commit, Push, and Create PR - Git Workflow Automation

## Purpose
Automate the complete git workflow for committing changes, pushing to remote, and creating a pull request in a single coordinated action.

## When to Use
- After completing feature development or bug fixes
- When ready to submit changes for code review
- For automated deployment workflows
- When following standardized git workflow procedures

## Input Requirements
- Working directory with uncommitted changes
- Valid git repository with remote origin configured
- GitHub CLI (gh) configured and authenticated
- Appropriate permissions to create branches and PRs

## Prerequisites Check
Before executing this workflow, verify:
- [ ] All tests are passing
- [ ] Code formatting/linting is applied
- [ ] No merge conflicts exist
- [ ] Working on appropriate base branch

## Context Gathering

### Current Git State
```bash
# Gather current git status and changes
git status
git diff HEAD  # Show all staged and unstaged changes
git branch --show-current  # Current branch name
```

### Change Analysis
- **Files modified:** [List from git status]
- **Change scope:** [Feature/bugfix/refactor/docs]
- **Impact level:** [Low/medium/high]
- **Breaking changes:** [Yes/No]

## Automated Workflow Steps

### Step 1: Branch Management
```bash
# If on main/dev branch, create feature branch
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" = "main" ] || [ "$CURRENT_BRANCH" = "dev" ]; then
    FEATURE_BRANCH="feature/$(date +%Y%m%d)-$(echo $COMMIT_MESSAGE | tr ' ' '-' | head -c 20)"
    git checkout -b "$FEATURE_BRANCH"
fi
```

### Step 2: Stage and Commit Changes
```bash
# Stage all relevant changes
git add .

# Create semantic commit message
git commit -m "$COMMIT_MESSAGE

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

### Step 3: Push to Remote
```bash
# Push branch to origin with upstream tracking
git push -u origin $(git branch --show-current)
```

### Step 4: Create Pull Request
```bash
# Create PR with standardized template
gh pr create --title "$PR_TITLE" --body "$PR_BODY"
```

## Commit Message Generation

### Semantic Commit Format
```
type(scope): subject

body

footer
```

### Commit Types
- **feat:** New feature
- **fix:** Bug fix
- **docs:** Documentation only
- **style:** Code style (formatting, semicolons, etc.)
- **refactor:** Code refactoring
- **perf:** Performance improvements
- **test:** Adding or updating tests
- **chore:** Build process or auxiliary tool changes
- **ci:** CI/CD configuration changes

### Examples
```
feat(auth): add password reset functionality

Implement password reset flow with email verification
- Add password reset request endpoint
- Create email template for reset link
- Add reset token validation
- Update user service with reset methods

Closes #123
```

## Pull Request Template

### PR Title Format
`[Type] Brief description of changes`

### PR Body Template
```markdown
## Summary
Brief description of what this PR accomplishes

## Changes Made
- [ ] List of specific changes
- [ ] Another change
- [ ] Third change

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] E2E tests pass
- [ ] Manual testing completed

## Screenshots/Videos
[If UI changes, include screenshots]

## Breaking Changes
[List any breaking changes, or "None"]

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No merge conflicts
- [ ] Tests added for new functionality

🤖 Generated with [Claude Code](https://claude.ai/code)
```

## Error Handling

### Common Issues and Solutions

**Merge Conflicts:**
```bash
# If push fails due to conflicts
git pull origin $(git branch --show-current)
# Resolve conflicts manually
git add .
git commit -m "Resolve merge conflicts"
git push
```

**Authentication Issues:**
```bash
# Re-authenticate GitHub CLI
gh auth login --web
```

**Branch Protection Rules:**
- Ensure PR creation instead of direct push to protected branches
- Include required reviewers if configured
- Verify status checks are passing

## Execution Template

### Single-Message Workflow
```bash
# Execute all steps in parallel for maximum efficiency

# 1. Branch check and creation (if needed)
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" = "main" ] || [ "$CURRENT_BRANCH" = "dev" ]; then
    git checkout -b "feature/automated-$(date +%Y%m%d-%H%M%S)"
fi

# 2. Stage and commit
git add .
git commit -m "$(cat <<'EOF'
{commit_type}({scope}): {subject}

{body}

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"

# 3. Push with upstream
git push -u origin $(git branch --show-current)

# 4. Create PR
gh pr create --title "{pr_title}" --body "$(cat <<'EOF'
## Summary
{summary}

## Changes Made
{changes_list}

## Testing
- [ ] All tests passing
- [ ] Code review ready

🤖 Generated with [Claude Code](https://claude.ai/code)
EOF
)"
```

## Integration with Development Workflow

### Pre-commit Hooks Integration
- Ensure pre-commit hooks run before commit
- Handle hook failures gracefully
- Re-commit if hooks modify files

### CI/CD Integration
- Wait for initial CI checks before creating PR
- Include CI status in PR description
- Configure auto-merge if all checks pass

### Team Collaboration
- Tag appropriate reviewers based on changed files
- Include relevant team members in PR notifications
- Follow team branching and PR naming conventions

## Success Criteria
- [ ] Clean commit created with proper message format
- [ ] Branch pushed successfully to remote
- [ ] Pull request created with complete description
- [ ] All automation completed in single action
- [ ] No manual intervention required for standard workflow

## Rollback Procedures
If the automated workflow fails:
1. Check git status to see what completed
2. If commit created but push failed: `git reset HEAD~1` to undo commit
3. If branch created but not needed: `git checkout main && git branch -D feature-branch`
4. If PR created incorrectly: `gh pr close PR_NUMBER` and recreate manually