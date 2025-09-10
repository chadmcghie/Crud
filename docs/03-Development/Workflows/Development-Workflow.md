# Development Workflow Guide

## Overview

This guide outlines the development workflow for contributors, including branching strategies, CI/CD integration, and best practices for working with our automated pipeline.

## Prerequisites

Before starting development:

1. **Environment Setup**:
   - .NET 8 SDK installed
   - Node.js 20+ installed
   - Git configured with your credentials
   - Visual Studio 2022 or VS Code
   - GitHub account with repository access

2. **Repository Setup**:
   ```bash
   git clone [repository-url]
   cd Crud
   git checkout dev
   git pull origin dev
   ```

## Branching Strategy

### Branch Types

| Branch Pattern | Purpose | Base Branch | Merges To |
|----------------|---------|-------------|-----------|
| `feature/*` | New features | `dev` | `dev` |
| `bugfix/*` | Bug fixes | `dev` | `dev` |
| `hotfix/*` | Emergency fixes | `main` | `main` & `dev` |
| `dev` | Integration branch | - | `main` |
| `main` | Production branch | - | - |

### Creating a Feature Branch

```bash
# Ensure you're on latest dev
git checkout dev
git pull origin dev

# Create your feature branch
git checkout -b feature/your-feature-name

# Naming conventions:
# feature/add-user-authentication
# feature/update-payment-processing
# bugfix/fix-login-error
# hotfix/critical-security-patch
```

## Agent-Assisted Development

This project leverages AI agents to streamline development workflow. We recommend using agents for structured development processes.

### Agent Workflow Integration

**Before Starting Development**:
1. **Plan with Agent OS**: Use `/create-spec` in Claude to create feature specifications
2. **Break Down Work**: Use `create-tasks` to generate actionable development tasks
3. **Review Technical Requirements**: Check generated technical specifications

**During Development**:
1. **Execute Tasks Systematically**: Use `execute-task` for structured TDD workflow
2. **Use Context-Aware Tools**: Leverage Cursor for code completion and refactoring
3. **Pair Program**: Use GitHub Copilot for inline suggestions and test generation

**For Complex Issues**:
1. **Document Problems**: Use `document-blocking-issue` for persistent issues
2. **Systematic Troubleshooting**: Use `troubleshoot-with-history` agent
3. **Follow Protected Changes**: Maintain improvements identified during debugging

**Upon Completion**:
1. **Update Progress**: Use `post-execution-tasks` to update roadmaps and create recaps
2. **Verify Completion**: Use `project-manager` agent for task verification
3. **Document Lessons**: Update knowledge base with lessons learned

### Agent Selection Quick Reference

| Task Type | Recommended Agent | Purpose |
|-----------|------------------|---------|
| Feature Planning | Agent OS `create-spec` | Structured specification creation |
| Task Breakdown | Agent OS `create-tasks` | Actionable development items |
| Implementation | Agent OS `execute-task` | TDD workflow execution |
| Code Completion | Cursor AI | Context-aware suggestions |
| Pair Programming | GitHub Copilot | Inline code assistance |
| Testing | Claude `test-runner` | Test analysis and debugging |
| Git Operations | Claude `git-workflow` | PR creation and management |
| Troubleshooting | Claude `troubleshoot-with-history` | Systematic problem solving |

See [Agent Utilization Guide](../Agent-Utilization-Guide.md) for complete documentation.

## Development Cycle

### 1. Local Development

**Write Code**:
- Follow existing code patterns
- Maintain consistent styling
- Add appropriate comments
- Update tests as needed

**Local Testing**:
```bash
# Run backend tests
dotnet test test/Tests.Unit.Backend/

# Run Angular tests
cd src/Angular
npm test

# Run specific test file
dotnet test --filter "FullyQualifiedName~PersonServiceTests"
```

### 2. Commit Guidelines

**Commit Message Format**:
```
type(scope): description

[optional body]

[optional footer]
```

**Examples**:
```bash
git commit -m "feat(api): add user authentication endpoint"
git commit -m "fix(angular): resolve navigation menu bug"
git commit -m "docs(readme): update CI/CD documentation"
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Test additions/changes
- `chore`: Build process or auxiliary tool changes

### 3. Push to Remote

```bash
# First push
git push -u origin feature/your-feature-name

# Subsequent pushes
git push
```

**What Happens**:
- Triggers `feature-branch-tests.yml`
- Runs quick unit tests
- Provides rapid feedback (2-3 minutes)

### 4. Create Pull Request

**Via GitHub UI**:
1. Navigate to the repository
2. Click "Pull requests" → "New pull request"
3. Base: `dev` ← Compare: `feature/your-feature-name`
4. Fill out the PR template

**PR Template**:
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No console errors
```

**What Happens**:
- Triggers `pr-validation.yml`
- Runs full test suite (5-10 minutes)
- All checks must pass before merge

### 5. Code Review Process

**For Reviewers**:
- Check code quality and patterns
- Verify test coverage
- Ensure documentation updates
- Look for potential issues

**For Authors**:
- Respond to feedback promptly
- Make requested changes
- Re-request review after updates

### 6. Merging to Dev

**Requirements**:
- At least 1 approval
- All CI checks passing
- No merge conflicts
- Conversations resolved

**After Merge**:
- Branch auto-deleted (if configured)
- Triggers `deploy-staging.yml`
- Auto-deploys to staging environment

## Working with Staging

### Staging Deployment

After merging to `dev`:
1. Staging deployment starts automatically
2. Takes 5-10 minutes
3. Available at: `https://staging.your-app.com`

### Testing in Staging

**Verification Steps**:
1. Test new features
2. Verify integrations
3. Check performance
4. Review with stakeholders

### Reporting Issues

If issues found in staging:
1. Create bugfix branch from `dev`
2. Fix the issue
3. Follow standard PR process

## Production Deployment

### Creating Production PR

When staging is approved:

```bash
# Ensure dev is up to date
git checkout dev
git pull origin dev

# Create PR via GitHub UI
# Base: main ← Compare: dev
```

**Requirements**:
- Management approval (2 reviewers)
- Staging sign-off completed
- No critical issues

### Production Deployment Process

1. **PR to Main**:
   - Triggers smoke tests
   - Requires approvals

2. **Merge to Main**:
   - Triggers `deploy-production.yml`
   - Requires environment approval
   - Deploys to production

3. **Post-Deployment**:
   - Monitor metrics
   - Check error rates
   - Verify critical paths

## Emergency Hotfixes

For critical production issues:

```bash
# Create hotfix from main
git checkout main
git pull origin main
git checkout -b hotfix/critical-issue

# Make minimal fix
# Test thoroughly

# Push and create PR to main
git push -u origin hotfix/critical-issue
```

**Process**:
1. PR directly to `main`
2. Expedited review process
3. Deploy to production
4. Cherry-pick to `dev`

## CI/CD Pipeline Status

### Understanding Workflow Status

| Status | Meaning | Action |
|--------|---------|--------|
| ✅ Green | All checks passed | Ready to merge |
| 🟡 Yellow | Checks running | Wait for completion |
| ❌ Red | Checks failed | Fix issues |
| ⏭️ Skipped | Copilot commit | Normal, no action |

### Common CI Issues and Solutions

**Test Failures**:
```bash
# View test results in GitHub Actions
# Fix locally and push updates
dotnet test --logger "console;verbosity=detailed"
```

**Build Failures**:
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

**Lint Failures**:
```bash
# Angular linting
cd src/Angular
npm run lint -- --fix
```

## Best Practices

### Code Quality

1. **Before Pushing**:
   - Run tests locally
   - Check for lint issues
   - Review your own changes

2. **PR Size**:
   - Keep PRs focused and small
   - One feature/fix per PR
   - Easier to review and test

3. **Documentation**:
   - Update relevant docs
   - Add inline comments for complex logic
   - Update README if needed

### Communication

1. **PR Descriptions**:
   - Be clear and detailed
   - Link related issues
   - Include testing steps

2. **Review Comments**:
   - Be constructive
   - Suggest improvements
   - Ask questions if unclear

3. **Status Updates**:
   - Update PR status
   - Communicate blockers
   - Request help when needed

## Tools and Resources

### Useful Commands

```bash
# Check branch status
git status

# View recent commits
git log --oneline -10

# Sync with upstream
git fetch origin
git rebase origin/dev

# Clean up local branches
git branch -d feature/old-feature
git remote prune origin
```

### GitHub CLI

```bash
# Install GitHub CLI
# Create PR from command line
gh pr create --base dev --title "Feature: Add new capability"

# Check PR status
gh pr status

# View workflow runs
gh run list
```

### VS Code Extensions

- GitLens
- GitHub Pull Requests
- GitHub Actions
- .NET Core Test Explorer

### AI Agent Tools

**Claude Code Commands**:
```bash
# Feature planning and specification
/create-spec [feature description]

# Task breakdown and management
/create-tasks [spec reference]

# Execute development tasks
/execute-tasks [task reference]

# Troubleshoot complex issues
/troubleshoot-issues [issue description]

# Analyze project structure
/analyze-product
```

**Agent OS Workflows** (in `.agents/.agent-os/instructions/core/`):
- `plan-product.md` - Product planning and roadmap creation
- `create-spec.md` - Feature specification generation
- `create-tasks.md` - Task breakdown and planning
- `execute-task.md` - Structured development execution
- `post-execution-tasks.md` - Completion and documentation

**Specialized Agents** (in `.claude/agents/`):
- `project-manager` - Task completion verification
- `test-runner` - Test analysis and debugging
- `git-workflow` - Git operations automation
- `troubleshoot-with-history` - Systematic problem solving

See [Agent Utilization Guide](../Agent-Utilization-Guide.md) for detailed usage.

## Troubleshooting

### Merge Conflicts

```bash
# Update your branch
git checkout feature/your-branch
git fetch origin
git rebase origin/dev

# Resolve conflicts
# Edit conflicted files
git add .
git rebase --continue
```

### Failed Deployments

1. Check GitHub Actions logs
2. Review deployment artifacts
3. Verify environment variables
4. Contact DevOps if needed

### Test Flakiness

1. Re-run failed jobs (one retry)
2. Run tests locally to reproduce
3. Check for timing issues
4. Review test isolation

### Persistent Development Issues

For complex or recurring problems:

1. **Document the Issue**: Use `document-blocking-issue` agent to create structured documentation
2. **Systematic Troubleshooting**: Use `troubleshoot-with-history` agent for systematic problem-solving
3. **Preserve Strategic Changes**: Follow the protected changes mechanism to maintain improvements
4. **Knowledge Preservation**: Update the blocking-issues registry for future reference

**Blocking Issue Workflow**:
```bash
# Document a persistent problem
/document-blocker [issue description]

# Systematic troubleshooting with history awareness
/troubleshoot-issues [blocking issue reference]
```

See [Troubleshooting Agents Design](../../05-Troubleshooting/Troubleshooting-Guides/troubleshooting-agents-design.md) for detailed information.

## Getting Help

- **Agent Utilization**: Check [Agent Utilization Guide](../Agent-Utilization-Guide.md)
- **Systematic Problem Solving**: Use `troubleshoot-with-history` agent
- **Documentation**: Check `/docs` folder
- **Team Chat**: Use Slack/Teams channel
- **Issues**: Create GitHub issue
- **Wiki**: Check project wiki

## Summary

This workflow ensures:
- ✅ Code quality through automated testing
- ✅ Rapid feedback on feature branches
- ✅ Thorough validation before integration
- ✅ Safe progression to production
- ✅ Clear rollback procedures

Remember: The CI/CD pipeline is here to help, not hinder. It catches issues early and ensures consistent quality across the codebase.