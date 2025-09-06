# Troubleshooting Agents Design

## Overview

This document defines two complementary agents for your Agent OS that follow industry standards (ITIL, ISO 31010) for incident management and root cause analysis. These agents prevent regression loops and maintain strategic improvements.

## Agent 1: Blocking Issue Documentation Agent

### Purpose
Documents blocking issues following ITIL Problem Management standards, creating a persistent knowledge base that prevents issue recurrence and rollback of fixes.

### Industry Standards Applied
- **ITIL Problem Management**: Structured documentation of recurring problems
- **ISO/IEC 31010**: Risk assessment and documentation standards
- **Blameless Post-Mortem**: Focus on systems/processes, not individuals

### Core Responsibilities
1. **Issue Capture**: Document blocking issues with structured format
2. **Root Cause Analysis**: Apply Five Whys methodology
3. **Solution Tracking**: Record attempted fixes and their outcomes
4. **Prevention Registry**: Maintain list of "do not rollback" changes

### File Structure
```
.agent-os/
├── blocking-issues/
│   ├── active/           # Current blocking issues
│   │   └── YYYY-MM-DD-{issue-slug}.md
│   ├── resolved/         # Resolved issues for reference
│   │   └── YYYY-MM-DD-{issue-slug}.md
│   └── registry.md       # Master registry of all issues
```

### Blocking Issue Template
```markdown
---
id: BI-YYYY-MM-DD-001
status: active|resolved
category: test|build|deployment|functionality
severity: critical|high|medium|low
created: YYYY-MM-DD HH:MM
resolved: YYYY-MM-DD HH:MM
spec: spec-folder-name
---

# [Brief Issue Title]

## Problem Statement
Clear, concise description of what is blocked and why.

## Impact
- What features/tasks are blocked
- Business/development impact
- Number of affected components

## Root Cause Analysis (Five Whys)
1. Why did this issue occur? [Answer]
2. Why? [Answer]
3. Why? [Answer]
4. Why? [Answer]
5. Why? [Root cause]

## Attempted Solutions
### Attempt 1: [YYYY-MM-DD HH:MM]
- **Approach**: What was tried
- **Result**: Why it failed
- **Files Modified**: List of files
- **Commit Hash**: If applicable

### Attempt 2: [YYYY-MM-DD HH:MM]
- **Approach**: What was tried
- **Result**: Why it failed
- **Files Modified**: List of files
- **Commit Hash**: If applicable

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [ ] File: path/to/file - Lines: X-Y - Reason: [Why this change is important]
- [ ] File: path/to/file - Lines: X-Y - Reason: [Why this change is important]

## Current Workaround
Temporary solution if available.

## Permanent Solution
Once resolved, document the final fix here.

## Lessons Learned
What can prevent similar issues in the future.

## Related Issues
- Links to related blocking issues
- Links to GitHub issues/PRs
```

## Agent 2: Troubleshooting Agent

### Purpose
Systematic troubleshooting that consumes blocking issue documentation to prevent regression loops and maintain strategic improvements.

### Industry Standards Applied
- **ITIL Incident Management**: Structured incident resolution
- **Root Cause Analysis (RCA)**: Systematic problem solving
- **Change Management**: Prevent regression of fixes

### Core Responsibilities
1. **Issue Assessment**: Check if problem matches existing blocking issues
2. **Solution History**: Review previous attempts to avoid repetition
3. **Protected Changes**: Identify and preserve strategic improvements
4. **Progressive Resolution**: Build on previous attempts, don't restart

### Workflow Integration
```yaml
troubleshooting_flow:
  1_initial_assessment:
    - Check .agent-os/blocking-issues/registry.md
    - Search for similar symptoms in resolved issues
    - Identify if this is a continuation of existing issue
  
  2_context_preservation:
    - Load all "DO NOT ROLLBACK" changes from active issues
    - Create protected_changes.json with file ranges to preserve
    - Warn before modifying any protected code sections
  
  3_progressive_troubleshooting:
    - Review all previous attempts from blocking issue
    - Start from last attempt number + 1
    - Document each new attempt in blocking issue file
  
  4_solution_validation:
    - Verify fix doesn't revert protected changes
    - Run regression tests for previously resolved issues
    - Update blocking issue with resolution if successful
  
  5_knowledge_capture:
    - Move issue to resolved/ folder if fixed
    - Update registry with resolution summary
    - Create recap in .agent-os/recaps/ if significant
```

### Protected Changes Mechanism
```json
{
  "protected_changes": [
    {
      "issue_id": "BI-2025-08-29-001",
      "file": "test/Tests.E2E.NG/playwright.config.ts",
      "lines": [45, 67],
      "hash": "a3f5b2c",
      "reason": "Serial execution fix for SQLite constraints",
      "do_not_modify": true
    }
  ],
  "last_updated": "2025-08-29T14:30:00Z"
}
```

## Agent Instruction Files

### 1. `.agent-os/instructions/core/document-blocking-issue.md`
```markdown
---
description: Document blocking issues following ITIL Problem Management standards
globs:
alwaysApply: false
version: 1.0
encoding: UTF-8
---

# Blocking Issue Documentation Rules

## Overview
Document blocking issues systematically to prevent regression and maintain knowledge base.

<process_flow>

<step number="1" name="issue_identification">
### Step 1: Issue Identification
Determine if issue qualifies as blocking after 3 failed attempts or 30 minutes elapsed.

<criteria>
  <blocking_when>
    - Three different approaches have failed
    - Issue prevents task completion
    - No clear path forward exists
  </blocking_when>
</criteria>
</step>

<step number="2" name="create_documentation">
### Step 2: Create Issue Documentation
Create new blocking issue file in .agent-os/blocking-issues/active/.

<instructions>
  ACTION: Create YYYY-MM-DD-{issue-slug}.md
  TEMPLATE: Use blocking issue template
  POPULATE: All known information
</instructions>
</step>

<step number="3" name="root_cause_analysis">
### Step 3: Perform Root Cause Analysis
Apply Five Whys methodology to identify root cause.

<instructions>
  ACTION: Ask "Why?" iteratively
  DEPTH: Minimum 3 levels, maximum 5
  FOCUS: System/process causes, not people
</instructions>
</step>

<step number="4" name="protect_improvements">
### Step 4: Identify Strategic Changes
Mark improvements that must not be rolled back.

<instructions>
  ACTION: List all beneficial changes made
  FORMAT: File, lines, and reason for protection
  UPDATE: protected_changes.json
</instructions>
</step>

</process_flow>
```

### 2. `.agent-os/instructions/core/troubleshoot-with-history.md`
```markdown
---
description: Systematic troubleshooting using blocking issue history
globs:
alwaysApply: false
version: 1.0
encoding: UTF-8
---

# Historical Troubleshooting Rules

## Overview
Troubleshoot issues progressively using documented history to prevent regression.

<process_flow>

<step number="1" name="history_check">
### Step 1: Check Issue History
Search blocking issues for similar problems.

<instructions>
  ACTION: Search .agent-os/blocking-issues/
  CHECK: Both active and resolved folders
  MATCH: Symptoms, error messages, affected files
</instructions>
</step>

<step number="2" name="load_protected_changes">
### Step 2: Load Protected Changes
Identify all changes that must be preserved.

<instructions>
  ACTION: Read protected_changes.json
  LOAD: All DO NOT ROLLBACK sections
  WARN: Before modifying protected code
</instructions>
</step>

<step number="3" name="progressive_resolution">
### Step 3: Build on Previous Attempts
Continue from last documented attempt.

<instructions>
  ACTION: Review all previous attempts
  START: From attempt number N+1
  AVOID: Repeating failed approaches
  BUILD: On partial successes
</instructions>
</step>

<step number="4" name="validate_solution">
### Step 4: Validate Without Regression
Ensure fix doesn't break previous fixes.

<instructions>
  ACTION: Verify protected changes intact
  RUN: Regression tests for related issues
  CHECK: No reintroduction of old bugs
</instructions>
</step>

</process_flow>
```

## Implementation Benefits

### 1. Prevents Regression Loops
- Protected changes mechanism ensures strategic improvements aren't rolled back
- Historical documentation prevents repeating failed solutions
- Progressive troubleshooting builds on previous work

### 2. Knowledge Preservation
- Structured documentation creates searchable knowledge base
- Root cause analysis provides deep understanding
- Lessons learned prevent future occurrences

### 3. Efficiency Improvements
- Reduces time spent on recurring issues
- Eliminates redundant troubleshooting attempts
- Accelerates resolution through history awareness

### 4. Team Collaboration
- Blameless documentation encourages transparency
- Shared knowledge base helps all team members
- Clear handoff points for complex issues

## Integration with Existing Agent OS

### Compatibility
- Follows existing Agent OS file structure patterns
- Uses same YAML frontmatter format
- Integrates with existing test-runner and project-manager agents
- Complements execute-task.md blocking issue handling

### Workflow Enhancement
- Triggers after 3 failed attempts (aligns with execute-task.md)
- Creates documentation in parallel structure to specs/
- Uses similar recap format for resolved issues
- Maintains consistency with existing .agent-os standards

## Next Steps

1. Get permission to add these files to .agent-os folder
2. Create the instruction files in .agent-os/instructions/core/
3. Create blocking-issues folder structure
4. Initialize protected_changes.json
5. Test with a sample blocking issue
6. Document agent usage in .agent-os/product/

## Usage Example

When encountering a blocking issue:
```bash
# Agent automatically triggers after 3 failed attempts
# Creates: .agent-os/blocking-issues/active/2025-08-30-test-timeout.md
# Updates: protected_changes.json with changes to preserve
# Future troubleshooting will load and respect these protections
```

This design ensures your troubleshooting process becomes progressively smarter, preventing the multi-day loops you've been experiencing while maintaining all strategic improvements made along the way.