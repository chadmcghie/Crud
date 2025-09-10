---
name: troubleshoot-with-history
description: Systematic troubleshooting using blocking issue history to prevent regression
globs:
alwaysApply: false
version: 1.0
encoding: UTF-8
---

# Historical Troubleshooting Rules

## Overview
Troubleshoot issues progressively using documented history to prevent regression loops and preserve strategic improvements.

<pre_flight_check>
  EXECUTE: @.agents/.agent-os/instructions/meta/pre-flight.md
</pre_flight_check>

<process_flow>

<step number="1" name="history_check">

### Step 1: Check Issue History

Search blocking issues for similar problems before starting troubleshooting.

<search_locations>
  <primary>docs/05-Troubleshooting/Blocking-Issues/active/</primary>
  <secondary>docs/05-Troubleshooting/Blocking-Issues/resolved/</secondary>
  <registry>docs/05-Troubleshooting/Blocking-Issues/registry.md</registry>
  <learning>docs/03-Development/learning/patterns.md</learning>
</search_locations>

<search_criteria>
  <match_on>
    - Error messages (exact or partial)
    - File paths affected
    - Symptoms described
    - Component/feature area
    - Task or spec reference
  </match_on>
</search_criteria>

<history_loading>
  IF matching issue found:
    - Load complete issue documentation
    - Note all previous attempts
    - Identify last attempt number
    - Extract protected changes
    - Review lessons learned
  
  IF matching pattern in learning cache:
    - Apply known solution from patterns.md
    - Reference pattern ID in solution
    - Skip failed approaches documented
  
  ELSE:
    - Proceed with fresh troubleshooting
    - Be prepared to document if blocking
</history_loading>

<instructions>
  ACTION: Search all blocking issues
  GREP: Error messages and symptoms
  CHECK: Both active and resolved folders
  LOAD: Full context of matching issues
</instructions>

</step>

<step number="2" name="load_protected_changes">

### Step 2: Load and Protect Strategic Changes

Identify all changes that must be preserved during troubleshooting.

<protection_sources>
  1. docs/05-Troubleshooting/Blocking-Issues/protected_changes.json
  2. DO NOT ROLLBACK sections in active issues
  3. Inline code comments marking protected sections
</protection_sources>

<load_process>
```bash
# Read protected changes registry
cat docs/05-Troubleshooting/Blocking-Issues/protected_changes.json

# Extract all protected file ranges
# Create in-memory map of protected code sections
# Flag these sections as read-only during troubleshooting
```
</load_process>

<protection_enforcement>
  <before_edit>
    - Check if file has protected sections
    - Verify edit doesn't overlap protected lines
    - Warn if attempting to modify protected code
  </before_edit>
  <override_process>
    - Require explicit justification
    - Document why protection is being overridden
    - Update protected_changes.json with new state
  </override_process>
</protection_enforcement>

<warning_template>
  ⚠️ PROTECTED CODE WARNING
  
  File: [path/to/file]
  Lines: [X-Y]
  Protected by: Issue BI-YYYY-MM-DD-001
  Reason: [Why this is protected]
  
  This code section is protected from modification.
  Previous attempts to change this caused: [problem]
  
  To override: Document justification in blocking issue.
</warning_template>

<instructions>
  ACTION: Load protected_changes.json
  MAP: All protected code sections
  ENFORCE: Protection before any edits
  WARN: User about protected sections
</instructions>

</step>

<step number="3" name="progressive_resolution">

### Step 3: Build on Previous Attempts

Continue from last documented attempt, don't restart from beginning.

<continuation_strategy>
  <if_existing_issue>
    - Start from Attempt N+1
    - Review what was learned from each previous attempt
    - Build on partial successes
    - Avoid repeating exact same approaches
  </if_existing_issue>
  <if_new_issue>
    - Start with Attempt 1
    - Document comprehensively for future reference
  </if_new_issue>
</continuation_strategy>

<attempt_documentation>
### Attempt N: [YYYY-MM-DD HH:MM]
**Hypothesis**: What we think will work and why
**Approach**: Specific steps to test hypothesis
**Implementation**:
```[language]
# Code changes made
```
**Result**: What happened (success/failure/partial)
**Files Modified**: 
- path/to/file (lines X-Y): description of change
**Key Learning**: What this tells us about the problem
**Next Direction**: Based on this result, what to try next
</attempt_documentation>

<learning_accumulation>
  <track>
    - What definitely doesn't work
    - What partially works
    - What constraints we've discovered
    - What dependencies are involved
  </track>
  <avoid>
    - Repeating failed approaches
    - Ignoring previous learnings
    - Starting over from scratch
    - Undoing partial progress
  </avoid>
</learning_accumulation>

<instructions>
  ACTION: Review all previous attempts
  START: From last attempt number + 1
  BUILD: On accumulated knowledge
  AVOID: Repeating failed solutions
  DOCUMENT: Each new attempt thoroughly
</instructions>

</step>

<step number="4" name="validate_solution">

### Step 4: Validate Without Regression

Ensure fix doesn't break previous fixes or revert protected changes.

<validation_checklist>
  - [ ] Protected changes remain intact
  - [ ] No reversion of previous fixes
  - [ ] Related issues still resolved
  - [ ] No new issues introduced
  - [ ] Tests pass that were fixed before
</validation_checklist>

<regression_testing>
  <scope>
    - Run tests for current issue
    - Run tests for all related resolved issues
    - Verify protected functionality still works
    - Check for side effects in connected components
  </scope>
  <process>
    1. Create test inventory from resolved issues
    2. Run regression test suite
    3. Verify each protected change still functions
    4. Document any new issues discovered
  </process>
</regression_testing>

<validation_report>
## Validation Results
  
**Primary Issue**: [Fixed/Not Fixed]
**Protected Changes**: [Intact/Modified (justified)]
**Regression Tests**: [X/Y Passing]
**Side Effects**: [None/Listed below]
  
### Test Results
- [ ] Current issue tests: PASS/FAIL
- [ ] Related issue #1 tests: PASS/FAIL
- [ ] Protected functionality: WORKING/BROKEN
</validation_report>

<instructions>
  ACTION: Run comprehensive validation
  VERIFY: All protected changes intact
  TEST: Previous issues remain fixed
  DOCUMENT: Complete validation results
</instructions>

</step>

<step number="5" name="solution_documentation">

### Step 5: Document Solution or Escalation

Update blocking issue with resolution or escalate if still blocked.

<if_resolved>
  <update_issue>
    - Mark status as resolved
    - Document final solution
    - Add resolution timestamp
    - List all changes made
    - Document lessons learned
  </update_issue>
  <move_to_resolved>
    - Move file to docs/05-Troubleshooting/Blocking-Issues/resolved/
    - Update registry.md
    - Keep protected changes active
  </move_to_resolved>
</if_resolved>

<if_still_blocked>
  <update_issue>
    - Document latest attempt
    - Update strategic changes list
    - Revise root cause analysis if needed
    - Suggest next approaches
    - Flag for escalation if needed
  </update_issue>
  <escalate>
    - Mark as needs-escalation
    - Summarize all attempts
    - Highlight what's been learned
    - Suggest external resources needed
  </escalate>
</if_still_blocked>

<resolution_template>
## Permanent Solution

**Resolved**: YYYY-MM-DD HH:MM
**Solution Summary**: [Brief description of what fixed it]

**Implementation**:
```[language]
# Final working code
```

**Why This Works**:
[Explanation of why this solution addresses root cause]

**Changes Made**:
- File: path/to/file - Change: [description]
- File: path/to/file - Change: [description]

**Lessons Learned**:
- [Key insight 1]
- [Key insight 2]
- [What to watch for in future]

**Prevention**:
[How to prevent similar issues]
</resolution_template>

<instructions>
  ACTION: Document outcome thoroughly
  UPDATE: Blocking issue and registry
  PRESERVE: Important learnings
  ESCALATE: If still blocked after attempts
</instructions>

</step>

<step number="6" name="knowledge_capture">

### Step 6: Update Knowledge Base

Capture learnings for future troubleshooting sessions.

<knowledge_updates>
  <patterns>
    - Add to "Common Patterns" in registry.md
    - Document symptom-cause relationships
    - Note effective diagnostic approaches
  </patterns>
  <solutions>
    - Create solution templates for similar issues
    - Document effective troubleshooting sequences
    - Add to team knowledge base
  </solutions>
  <improvements>
    - Suggest codebase improvements
    - Identify architectural weaknesses
    - Propose preventive measures
  </improvements>
</knowledge_updates>

<continuous_improvement>
  IF pattern detected across multiple issues:
    - Create ADR for architectural change
    - Propose refactoring task
    - Update best practices
  IF tooling gap identified:
    - Document tool requirements
    - Suggest automation opportunities
</continuous_improvement>

<instructions>
  ACTION: Extract reusable knowledge
  UPDATE: Registry with patterns
  SHARE: Learnings for team benefit
  PROPOSE: Systematic improvements
</instructions>

</step>

</process_flow>

<troubleshooting_principles>

## Core Principles

### 1. Never Start from Zero
- Always check history first
- Build on previous work
- Preserve partial progress

### 2. Protect Strategic Improvements  
- Never rollback improvements
- Document why changes matter
- Enforce protection mechanisms

### 3. Document Everything
- Every attempt teaches something
- Failed attempts prevent repetition
- Success patterns enable reuse

### 4. Progressive Resolution
- Each attempt advances understanding
- Accumulate constraints and learnings
- Narrow solution space systematically

### 5. Prevent Regression
- Test previous fixes remain intact
- Validate protected changes preserved
- Check for reintroduced issues

</troubleshooting_principles>

<post_flight_check>
  EXECUTE: @.agents/.agent-os/instructions/meta/post-flight.md
</post_flight_check>