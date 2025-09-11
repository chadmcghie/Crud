# GitHub Workflow Best Practices

## Overview

This document defines the proper GitHub workflow for Agent OS to ensure issues flow correctly through project boards and maintain proper traceability between issues, pull requests, and project management.

## Core Principle

**Never close GitHub issues directly when work is completed. Instead, reference them in pull requests and let GitHub handle the workflow automatically.**

## Proper Workflow

### 1. Issue Lifecycle
```
Issue Created → In Progress → In Review (via PR) → Done (when PR merges)
```

### 2. Implementation Steps

#### When Starting Work
1. **Create/Reference Issue**: Ensure GitHub issue exists for the work
2. **Link to Spec**: Reference issue number in spec files
3. **Keep Issue Open**: Don't close when work is "done"

#### When Creating Pull Request
1. **Reference Issue**: Include "Closes #XXX" or "Fixes #XXX" in PR description
2. **Link to Spec**: Reference the specification in PR description
3. **Let GitHub Handle**: Don't manually close the issue

#### When PR Merges
1. **Automatic Closure**: GitHub automatically closes linked issues
2. **Project Board Update**: Issue moves to "Done" on project boards
3. **Proper Traceability**: Full audit trail maintained

## Agent OS Integration

### Updated Project Manager Agent Behavior

#### ❌ OLD (Broken) Workflow
```bash
# Don't do this anymore
gh issue close 127 --comment "Completed as part of spec: ..."
```

#### ✅ NEW (Proper) Workflow
```bash
# Reference issue in PR instead
gh pr create --body "Closes #127

Implementation details...
Spec: .agents/.agent-os/specs/2025-01-15-feature-name/"
```

### Git Workflow Agent Updates

The `git-workflow` agent should:

1. **Check for Issue References**: Look for issue numbers in spec files
2. **Include in PR Description**: Automatically add "Closes #XXX" to PR body
3. **Link to Spec**: Include spec reference in PR description
4. **Never Close Issues**: Leave issue management to GitHub

### Context Fetcher Agent Updates

The `context-fetcher` agent should:

1. **Track Issue-PR Relationships**: Show which PRs are linked to which issues
2. **Project Board Status**: Display current project board status
3. **Workflow Validation**: Check if issues are properly linked to PRs

## Implementation Guidelines

### For Spec Files
```markdown
# Spec Requirements Document

> Spec: Feature Name
> Created: 2025-01-15
> GitHub Issue: #177 - Issue Title
> Status: IN PROGRESS
```

### For PR Descriptions
```markdown
## Implementation Summary
[Details about what was implemented]

## Related
- **GitHub Issue**: #177 - Issue Title
- **Specification**: docs/03-Development/specs/2025-01-15-feature-name/

**Closes #177**
```

### For Task Files
```markdown
#### Task 1.1: Implement Feature
- [ ] Add feature implementation
- [ ] Test feature functionality
- [ ] Update documentation

**GitHub Issue**: #177
**Estimated Time**: 2 hours
```

## Benefits

### Project Management
- **Proper Board Flow**: Issues move through "In Progress" → "In Review" → "Done"
- **Clear Status**: Always know what's being reviewed vs completed
- **Better Planning**: Can see work in progress vs work under review

### Traceability
- **Complete Audit Trail**: Issue → PR → Merge → Close
- **Linked Discussions**: All conversations linked together
- **Spec Integration**: Clear connection between specs and issues

### Automation
- **GitHub Handles Workflow**: No manual issue management needed
- **Automatic Updates**: Project boards update automatically
- **Consistent Process**: Same workflow for all features

## Migration Strategy

### For Existing Workflows
1. **Update Project Manager Agent**: Remove direct issue closing
2. **Update Git Workflow Agent**: Add issue referencing to PR creation
3. **Update Documentation**: Reflect new workflow in all guides
4. **Train Team**: Ensure everyone follows new process

### For Current Issues
1. **Reopen Closed Issues**: If work is still in progress
2. **Link to PRs**: Add "Closes #XXX" to existing PRs
3. **Update Project Boards**: Ensure proper status tracking

## Tools and Commands

### Issue Management
```bash
# Reopen an issue (if closed prematurely)
gh issue reopen 177 --comment "Reopening to properly close via PR workflow"

# Check issue status
gh issue view 177 --json state,title

# List issues by status
gh issue list --state open
gh issue list --state closed
```

### PR Management
```bash
# Create PR with issue reference
gh pr create --body "Closes #177

Implementation details..."

# Update existing PR to reference issue
gh pr edit 188 --body "Updated description

Closes #177"
```

### Project Board Integration
```bash
# Check project board status
gh api graphql -f query='query { repository(owner: "owner", name: "repo") { projectsV2(first: 10) { nodes { title items(first: 100) { nodes { content { ... on Issue { number title } } } } } } } } }'
```

## Quality Assurance

### Validation Checklist
- [ ] Issue exists and is open when work starts
- [ ] Issue is referenced in spec files
- [ ] PR includes "Closes #XXX" in description
- [ ] Issue closes automatically when PR merges
- [ ] Project board shows proper status progression

### Common Mistakes to Avoid
- ❌ Closing issues manually when work is "done"
- ❌ Creating PRs without issue references
- ❌ Not linking specs to issues
- ❌ Mixing manual and automatic issue management

## Success Metrics

- **Project Board Accuracy**: Issues flow correctly through statuses
- **Traceability**: Complete audit trail from issue to completion
- **Team Adoption**: Consistent workflow across all features
- **Automation**: Reduced manual issue management overhead
