---
description: Create an Agent OS tasks list from an approved feature spec
globs:
alwaysApply: false
version: 1.1
encoding: UTF-8
---

# Task Creation Rules

## Overview

With the user's approval, proceed to creating a tasks list based on the current feature spec.

<pre_flight_check>
  EXECUTE: @.agent-os/instructions/meta/pre-flight.md
</pre_flight_check>

<process_flow>

<step number="1" name="github_parent_issue_check">

### Step 1: GitHub Parent Issue Check

Retrieve the parent issue information from the spec to use for creating sub-issues.

<parent_issue_retrieval>
  1. Read the spec.md file from the current feature spec folder
  2. Extract the GitHub Issue number from the header
  3. Store the parent issue number for sub-issue linking
</parent_issue_retrieval>

</step>

<step number="2" subagent="file-creator" name="create_tasks">

### Step 2: Create tasks.md

Use the file-creator subagent to create file: tasks.md inside of the current feature's spec folder.

<file_template>
  <header>
    # Spec Tasks
    
    > Parent Issue: #[PARENT_ISSUE_NUMBER]
  </header>
</file_template>

<task_structure>
  <major_tasks>
    - count: 1-5
    - format: numbered checklist
    - grouping: by feature or component
  </major_tasks>
  <subtasks>
    - count: up to 8 per major task
    - format: decimal notation (1.1, 1.2)
    - first_subtask: typically write tests
    - last_subtask: verify all tests pass
  </subtasks>
</task_structure>

<task_template>
  ## Tasks

  - [ ] 1. [MAJOR_TASK_DESCRIPTION] (Issue: #[SUB_ISSUE_1])
    - [ ] 1.1 Write tests for [COMPONENT]
    - [ ] 1.2 [IMPLEMENTATION_STEP]
    - [ ] 1.3 [IMPLEMENTATION_STEP]
    - [ ] 1.4 Verify all tests pass

  - [ ] 2. [MAJOR_TASK_DESCRIPTION] (Issue: #[SUB_ISSUE_2])
    - [ ] 2.1 Write tests for [COMPONENT]
    - [ ] 2.2 [IMPLEMENTATION_STEP]
</task_template>

<ordering_principles>
  - Consider technical dependencies
  - Follow TDD approach
  - Group related functionality
  - Build incrementally
</ordering_principles>

</step>

<step number="3" name="check_existing_github_issues">

### Step 3: Check for Existing GitHub Issues

Before creating new issues, check if suitable issues already exist.

<existing_issue_check>
  1. Parse the parent issue body for already linked sub-issues
  2. Use `gh issue view [PARENT_ISSUE_NUMBER]` to see linked issues
  3. Check if linked issues match the planned tasks
  4. Search for related open issues: `gh issue list --search "[KEYWORDS]" --state open`
  5. Document which tasks have existing issues vs need new ones
</existing_issue_check>

<decision_tree>
  IF existing_issues_found_for_all_tasks:
    SKIP to step 5
    USE existing issue numbers in tasks.md
  ELSE:
    PROCEED to step 4
    CREATE only missing issues
</decision_tree>

</step>

<step number="4" name="create_github_sub_issues">

### Step 4: Create GitHub Sub-Issues (Only if Needed)

Create GitHub sub-issues ONLY for tasks that don't have existing issues.

<github_sub_issue_creation>
  <for_each_major_task_without_existing_issue>
    1. Verify no existing issue covers this task
    2. Create sub-issue title: "[SPEC_NAME] - [MAJOR_TASK_DESCRIPTION]"
    3. Create sub-issue body with:
       - Reference to parent issue: "Part of #[PARENT_ISSUE_NUMBER]"
       - Task description
       - List of subtasks as checklist
    4. Use command: `gh issue create --title "[TITLE]" --body "[BODY]" --label "enhancement"`
    5. Store the created issue number
    6. Update tasks.md with the issue number
  </for_each_major_task_without_existing_issue>
  
  <link_to_parent>
    IF new_issues_were_created:
      - Add comment to parent issue listing sub-issues
      - Use command: `gh issue comment [PARENT_ISSUE_NUMBER] --body "Sub-tasks: #[ISSUE_1], #[ISSUE_2], ..."`
  </link_to_parent>
</github_sub_issue_creation>

</step>

<step number="5" name="execution_readiness">

### Step 5: Execution Readiness Check

Evaluate readiness to begin implementation by presenting the first task summary and requesting user confirmation to proceed.

<readiness_summary>
  <present_to_user>
    - Spec name and description
    - First task summary from tasks.md
    - Estimated complexity/scope
    - Key deliverables for task 1
  </present_to_user>
</readiness_summary>

Do not skip this execution prompt!!!  The user must consent to begining work on the task.  If there's no consent then do not start working on the task.

<execution_prompt>
  PROMPT: "The spec planning is complete. The first task is:

  **Task 1:** [FIRST_TASK_TITLE]
  [BRIEF_DESCRIPTION_OF_TASK_1_AND_SUBTASKS]

  Would you like me to proceed with implementing Task 1? I will focus only on this first task and its subtasks unless you specify otherwise.

  Type 'yes' to proceed with Task 1, or let me know if you'd like to review or modify the plan first."
</execution_prompt>

<execution_flow>
  IF user_confirms_yes:
    REFERENCE: @.agent-os/instructions/core/execute-tasks.md
    FOCUS: Only Task 1 and its subtasks
    CONSTRAINT: Do not proceed to additional tasks without explicit user request
  ELSE:
    WAIT: For user clarification or modifications
</execution_flow>

</step>

</process_flow>

<post_flight_check>
  EXECUTE: @.agent-os/instructions/meta/post-flight.md
</post_flight_check>
