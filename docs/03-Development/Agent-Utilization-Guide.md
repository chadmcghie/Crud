# Agent Utilization Guide

## Overview

This guide outlines the preferred utilization of AI agents in the Crud project development workflow. We use a combination of Agent OS, Claude Code, Cursor, and GitHub Copilot to accelerate development while maintaining code quality and architectural consistency.

## Agent Ecosystem

### 1. Agent OS (Primary Orchestrator)
**Location**: `.agents/.agent-os/` folder  
**Purpose**: Structured workflow management for planning, specification, and task execution

**Core Workflows**:
- **Product Planning**: `plan-product.md` - Generate mission, roadmap, and tech stack documentation
- **Specification Creation**: `create-spec.md` - Create detailed feature specifications aligned with product roadmap
- **Task Management**: `create-tasks.md` - Break down specifications into actionable tasks
- **Task Execution**: `execute-task.md` - Execute tasks following TDD workflow
- **Progress Tracking**: `post-execution-tasks.md` - Update roadmaps and create recaps

**Agent OS File Structure**:
```
.agents/.agent-os/
├── instructions/core/          # Core workflow instructions
├── product/                   # Mission, roadmap, tech stack
├── specs/                    # Feature specifications by date
├── recaps/                   # Completion summaries
├── blocking-issues/          # Issue documentation
└── standards/                # Code and process standards
```

### 2. Claude Code
**Location**: `.claude/` folder  
**Purpose**: Project-specific Claude AI configuration and specialized agents

**Specialized Agents**:
- `project-manager` - Task completion verification and roadmap updates
- `test-runner` - Test execution and failure analysis
- `git-workflow` - Git operations and PR management
- `context-fetcher` - Documentation and context retrieval
- `troubleshoot-with-history` - Debug with historical context

**Commands**:
- `/analyze-product` - Analyze codebase and install Agent OS
- `/create-spec` - Create feature specifications
- `/execute-tasks` - Execute planned tasks
- `/troubleshoot-issues` - Debug and resolve problems

### 3. Cursor AI
**Purpose**: Context-aware code completion and refactoring  
**Configuration**: `.cursorrules` file with project-specific rules

**Primary Use Cases**:
- Code completion with project context
- Automated refactoring
- Code generation following project patterns
- Real-time suggestions during development

### 4. GitHub Copilot
**Purpose**: AI pair programming and code suggestions  
**Integration**: Native IDE integration

**Primary Use Cases**:
- Inline code suggestions
- Test generation
- Documentation generation
- Pattern recognition and completion

## Recommended Agent Workflow

### Phase 1: Planning and Specification
1. **Use Agent OS `plan-product`** for new features or major changes
   - Generates mission-aligned specifications
   - Creates technical requirements
   - Establishes clear scope and dependencies

2. **Use Agent OS `create-spec`** for detailed feature planning
   - Creates comprehensive specifications
   - Generates technical and API specifications
   - Establishes acceptance criteria

3. **Use Agent OS `create-tasks`** to break down specifications
   - Creates actionable development tasks
   - Establishes dependencies and priorities
   - Generates test requirements

### Phase 2: Development and Implementation
1. **Start with Agent OS `execute-task`** for structured development
   - Follows TDD workflow
   - Maintains architectural consistency
   - Tracks progress systematically

2. **Use Cursor for active coding**
   - Context-aware completions
   - Pattern-based suggestions
   - Automated refactoring

3. **Use Copilot for pair programming**
   - Inline suggestions
   - Test generation
   - Documentation creation

4. **Use Claude specialized agents as needed**
   - `test-runner` for test analysis
   - `git-workflow` for PR management
   - `troubleshoot-with-history` for debugging

### Phase 3: Completion and Documentation
1. **Use Agent OS `post-execution-tasks`** for completion
   - Updates roadmap status
   - Creates completion recaps
   - Documents lessons learned

2. **Use `project-manager` agent** for verification
   - Validates task completion
   - Updates tracking documentation
   - Ensures quality standards

## Agent Selection Guidelines

### When to Use Agent OS
- **Feature planning and specification**
- **Breaking down complex work**
- **Structured task execution**
- **Progress tracking and documentation**
- **Following established workflows**

### When to Use Cursor
- **Active code development**
- **Refactoring existing code**
- **Pattern-based code generation**
- **Context-aware completions**

### When to Use Copilot
- **Pair programming sessions**
- **Learning new patterns**
- **Quick code suggestions**
- **Test and documentation generation**

### When to Use Claude Code
- **Project management tasks**
- **Complex troubleshooting**
- **Git workflow automation**
- **Custom project-specific operations**

## Integration with Development Workflow

### Branch Creation
1. Use Agent OS to plan the feature specification
2. Use `create-tasks` to break down the work
3. Create feature branch following naming conventions
4. Use `execute-task` to implement systematically

### Pull Request Process
1. Use `git-workflow` agent for PR creation
2. Use `test-runner` for automated testing
3. Use `project-manager` for completion verification
4. Use `post-execution-tasks` for documentation

### Issue Resolution
1. Use `document-blocking-issue` for complex problems
2. Use `troubleshoot-with-history` for debugging
3. Follow protected changes mechanism
4. Document solutions in blocking-issues registry

## Best Practices

### Agent Coordination
- **Start with Agent OS** for structured planning
- **Use specialized agents** for specific tasks
- **Maintain consistency** across agent outputs
- **Document decisions** in appropriate locations

### Quality Assurance
- **Follow TDD workflow** when using execute-task
- **Validate with test-runner** before completion
- **Use project-manager** for verification
- **Maintain architectural standards**

### Documentation
- **Keep specifications updated** in docs/03-Development/specs/
- **Document blocking issues** properly in docs/05-Troubleshooting/Blocking-Issues/
- **Create recaps** for completed work
- **Update roadmaps** consistently

### Troubleshooting
- **Document issues** using blocking-issues workflow
- **Use troubleshoot-with-history** for complex problems
- **Maintain protected changes** registry
- **Follow ITIL-based problem management**

## Configuration Files

### Agent OS Configuration
- `instructions/core/` - Core workflow definitions
- `product/mission.md` - Project mission and goals
- `product/roadmap.md` - Feature roadmap
- `standards/` - Code and process standards

### Claude Configuration
- `settings.local.json` - Pre-approved actions
- `agents/` - Specialized agent definitions
- `commands/` - Custom slash commands

### Project Configuration
- `CLAUDE.md` - Claude-specific project guidance
- `.cursorrules` - Cursor AI configuration
- Development workflow integration

## Conclusion

The agent ecosystem in this project provides a comprehensive workflow from planning through implementation to completion. By following this guide, developers can leverage AI assistance while maintaining code quality, architectural consistency, and proper documentation practices.

The key to success is using the right agent for the right task and maintaining consistency across the workflow. Agent OS provides the structured foundation, while specialized agents handle specific aspects of development and project management.