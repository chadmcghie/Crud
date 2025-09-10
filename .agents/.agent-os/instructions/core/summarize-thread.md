---
description: Rules to serialize the dialogue context and produce a condensed summary
globs:
alwaysApply: false
version: 1.0
encoding: UTF-8
---

# Task Execution Rules

## Overview

Read the current dialogue context, summarize the exchange in a concise formatted Markdown document, and save it according to the specified path and naming convention.

<pre_flight_check>
  EXECUTE: @.agents/.agent-os/instructions/meta/pre-flight.md
</pre_flight_check>

<process_flow>

<step number="1" name="context_capture">

### Step 1: Dialogue Context Capture

<instructions>
  ACTION: Retrieve the entire conversation history (dialogue context) available in this session.
  ANALYZE: Identify main themes, key questions, and answers.
  EXTRACT: Important technical terms, clarifications, or decisions made.
  NOTE: Ignore filler or repetitive exchanges unless critical to understanding intent.
</instructions>

</step>

<step number="2" name="condensed_summary">

### Step 2: Summarize the Exchange

<instructions>
  ACTION: Create a structured Markdown summary with:
    - **Title**: Brief topic identifier
    - **TL;DR**: One-sentence summary of the conversation
    - **Key Points**: Bulleted list of main exchanges
    - **Decisions/Resolutions**: Explicit outcomes, agreements, or next steps
  FORMAT: Use Markdown headings, lists, and emphasis for clarity.
  LENGTH: Keep concise, but ensure all important context is retained.
</instructions>

</step>

<step number="3" subagent="date-checker" name="date_determination">

### Step 3: Date Determination

Use the date-checker subagent to determine the current date in YYYYMMDD format for file naming. The subagent will output today's date which will be used in the filename.

<subagent_output>
  The date-checker subagent will provide the current date in YYYYMMDD format at the end of its response. Store this date for use in file naming in step 4.
</subagent_output>

</step>

<step number="4" name="file_saving">

### Step 4: Save to File

<instructions>
  ACTION: Save the Markdown summary in the following path:
    `C:\Users\chadm\source\repos\Crud\docs\Misc\AI Discussions`
  NAMING CONVENTION: `claude-task-{BRIEF-TASK-DESCRIPTION}-{DATE}.md`
    - {BRIEF-TASK-DESCRIPTION}: kebab-case description of the conversation topic
    - {DATE}: The YYYYMMDD date provided by the date-checker subagent in step 3
  EXAMPLE: `claude-task-conversation-summary-20241231.md`
  IMPORTANT: Use the exact date provided by the date-checker subagent, do not guess or calculate
  VERIFY: Ensure UTF-8 encoding.
</instructions>

</step>

</process_flow>

<post_flight_check>
  EXECUTE: @.agents/.agent-os/instructions/meta/post-flight.md
</post_flight_check>
