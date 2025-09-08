---
name: document-blocking-issue
description: Document blocking issues following ITIL Problem Management standards
globs:
alwaysApply: false
version: 1.0
encoding: UTF-8
---

# Blocking Issue Documentation Rules

## Overview
Document blocking issues systematically to prevent regression and maintain knowledge base following ITIL Problem Management standards.

<pre_flight_check>
  EXECUTE: @.agent-os/instructions/meta/pre-flight.md
</pre_flight_check>

<process_flow>

<step number="1" name="issue_identification">

### Step 1: Issue Identification

Determine if issue qualifies as blocking after 3 failed attempts or 30 minutes elapsed.

<blocking_criteria>
  <attempts>maximum 3 different approaches</attempts>
  <time_limit>30 minutes on single issue</time_limit>
  <impact>prevents task completion</impact>
</blocking_criteria>

<qualification_check>
  IF any of the following are true:
    - Three different solution approaches have failed
    - Issue prevents task/sub-task completion
    - No clear path forward after investigation
    - Circular dependency or conflict detected
  THEN:
    - Proceed to document as blocking issue
  ELSE:
    - Continue troubleshooting
</qualification_check>

<instructions>
  ACTION: Evaluate if issue meets blocking criteria
  DECISION: Document if blocking, continue if not
  TIMING: Check after each failed attempt
</instructions>

</step>

<step number="2" name="create_documentation">

### Step 2: Create Issue Documentation

Create new blocking issue file in .agent-os/blocking-issues/active/ with standardized format.

<file_naming>
  FORMAT: YYYY-MM-DD-{issue-slug}.md
  EXAMPLE: 2025-08-30-test-timeout-loop.md
  SLUG: descriptive-kebab-case
</file_naming>

<issue_template>
```markdown
---
id: BI-YYYY-MM-DD-001
status: active
category: test|build|deployment|functionality|configuration
severity: critical|high|medium|low
created: YYYY-MM-DD HH:MM
resolved: 
spec: spec-folder-name
task: task-number-and-description
---

# [Brief Issue Title]

## Problem Statement
[Clear, concise description of what is blocked and why]

## Symptoms
- Error message or behavior observed
- When it occurs (always/intermittent)
- Environment/context where it happens

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
**Approach**: What was tried
**Result**: Why it failed
**Files Modified**: 
- path/to/file1
- path/to/file2
**Key Learning**: What we learned from this attempt

### Attempt 2: [YYYY-MM-DD HH:MM]
**Approach**: What was tried
**Result**: Why it failed
**Files Modified**:
- path/to/file3
**Key Learning**: What we learned from this attempt

### Attempt 3: [YYYY-MM-DD HH:MM]
**Approach**: What was tried
**Result**: Why it failed
**Files Modified**:
- path/to/file4
**Key Learning**: What we learned from this attempt

## Strategic Changes (DO NOT ROLLBACK)
List of improvements made during troubleshooting that must be preserved:
- [ ] File: path/to/file - Lines: X-Y - Change: [description] - Reason: [Why this change is important]
- [ ] File: path/to/file - Lines: X-Y - Change: [description] - Reason: [Why this change is important]

## Current Workaround
[Temporary solution if available, or "None available"]

## Next Steps
- [ ] What to try next
- [ ] Investigation needed
- [ ] External help required

## Related Issues
- Link to related blocking issue: 
- Link to GitHub issue/PR:
- Link to spec task:
```
</issue_template>

<instructions>
  ACTION: Create new file in .agent-os/blocking-issues/active/
  USE: Issue template above
  POPULATE: All sections with known information
  ASSIGN: Unique ID with date prefix
</instructions>

</step>

<step number="3" name="root_cause_analysis">

### Step 3: Perform Root Cause Analysis

Apply Five Whys methodology to identify root cause, focusing on systems not people.

<five_whys_process>
  <guideline>
    Start with the immediate problem
    Ask "Why did this happen?"
    For each answer, ask "Why?" again
    Continue until root cause found (3-5 levels)
    Focus on process/system failures
  </guideline>
</five_whys_process>

<example>
  1. Why: Tests are timing out
     Answer: Server takes too long to start
  2. Why: Server takes too long to start  
     Answer: Database migrations run every time
  3. Why: Database migrations run every time
     Answer: No check for existing schema
  4. Why: No check for existing schema
     Answer: Test setup assumes clean state
  5. Why: Test setup assumes clean state
     Answer: Original design didn't consider reuse (ROOT CAUSE)
</example>

<instructions>
  ACTION: Apply Five Whys iteratively
  DEPTH: Minimum 3 levels, maximum 5
  FOCUS: System/process causes, not people
  DOCUMENT: Each level in the issue file
</instructions>

</step>

<step number="4" name="protect_improvements">

### Step 4: Identify and Protect Strategic Changes

Mark improvements that must not be rolled back and update protection registry.

<identification_criteria>
  <protect_when>
    - Change fixes a bug (even partially)
    - Change improves performance
    - Change adds necessary logging/debugging
    - Change corrects architectural issue
    - Change implements better practice
  </protect_when>
</identification_criteria>

<protection_process>
  1. List each beneficial change in issue document
  2. Include file path, line numbers, and reason
  3. Update .agent-os/blocking-issues/protected_changes.json
  4. Mark changes with protection comment in code
</protection_process>

<protected_changes_format>
```json
{
  "protected_changes": [
    {
      "issue_id": "BI-YYYY-MM-DD-001",
      "file": "path/to/file",
      "lines": [startLine, endLine],
      "hash": "git-commit-hash",
      "change_description": "What was changed",
      "reason": "Why this must be preserved",
      "created": "YYYY-MM-DD HH:MM",
      "do_not_modify": true
    }
  ],
  "last_updated": "YYYY-MM-DDTHH:MM:SSZ"
}
```
</protected_changes_format>

<instructions>
  ACTION: Identify all strategic improvements
  DOCUMENT: In both issue file and protected_changes.json
  FORMAT: Include full context for future reference
  PROTECT: Add inline comments warning against rollback
</instructions>

</step>

<step number="5" name="update_registry">

### Step 5: Update Master Registry

Add entry to blocking issues registry for quick lookup.

<registry_update>
  <location>.agent-os/blocking-issues/registry.md</location>
  <add_to>Active Issues table</add_to>
  <fields>
    - ID (from issue file)
    - Created date
    - Spec name
    - Category
    - Brief description
    - Status (active)
  </fields>
</registry_update>

<instructions>
  ACTION: Update registry.md with new issue
  FORMAT: Add row to Active Issues table
  SORT: Most recent first
</instructions>

</step>

<step number="6" name="notify_and_escalate">

### Step 6: Notification and Escalation

Alert relevant parties if issue is critical or requires external help.

<escalation_criteria>
  <immediate>
    - Severity: critical
    - Blocks multiple tasks
    - Security implications
  </immediate>
  <deferred>
    - Severity: medium/low
    - Single task affected
    - Workaround available
  </deferred>
</escalation_criteria>

<notification_format>
  ## ⚠️ Blocking Issue Created
  
  **ID**: BI-YYYY-MM-DD-001
  **Title**: [Issue title]
  **Severity**: [critical|high|medium|low]
  **Impact**: [What's blocked]
  **Attempts**: 3 solutions tried
  
  See: .agent-os/blocking-issues/active/YYYY-MM-DD-issue-slug.md
</notification_format>

<instructions>
  ACTION: Output notification if critical
  INCLUDE: Issue ID and location
  SUGGEST: Next steps or escalation needed
</instructions>

</step>

</process_flow>

<post_flight_check>
  EXECUTE: @.agent-os/instructions/meta/post-flight.md
</post_flight_check>