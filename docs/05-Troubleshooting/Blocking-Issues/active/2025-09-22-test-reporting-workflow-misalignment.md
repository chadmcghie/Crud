---
id: BI-2025-09-22-001
status: active
category: build
severity: medium
created: 2025-09-22 12:00
resolved:
spec: 2025-09-20-multi-config-e2e-testing
task: test-workflow-organization-and-reporting
---

# Test Reporting Workflow Misalignment

## Problem Statement
Backend integration tests are being reported under "Feature Branch Tests" instead of "PR Validation" workflow, and unit test results are not visible in PR validation summaries, creating confusion about test execution and results ownership.

## Symptoms
- Backend integration tests show under "Feature Branch Tests" summary despite running in PR validation workflow
- Unit test summaries are missing from PR validation workflow
- Test result ownership is unclear between feature branch and PR validation workflows
- Developers cannot easily see which tests ran where and what the results were

## Impact
- Reduced CI/CD transparency for PR reviewers
- Confusion about test execution flow and responsibility
- Difficulty troubleshooting test failures due to unclear workflow association
- Poor developer experience when reviewing PR test results

## Root Cause Analysis (Five Whys)
1. Why are integration tests showing under feature branch tests?
   Answer: Integration tests use artifacts from feature branch build, causing test reporter to associate results with feature workflow

2. Why does the test reporter associate results with feature workflow?
   Answer: The test artifacts contain metadata linking them to the original feature branch build job

3. Why are test artifacts linked to feature branch metadata?
   Answer: The build process in feature branch workflow creates artifacts with workflow context embedded

4. Why don't we have separate test reporting for PR validation?
   Answer: PR validation workflow was designed to reuse feature artifacts for efficiency, but reporting wasn't properly separated

5. Why wasn't reporting separation considered in workflow design?
   Answer: Original design prioritized artifact reuse for speed over clear test result ownership (ROOT CAUSE)

## Attempted Solutions

### Attempt 1: [2025-09-22 12:00]
**Approach**: Identified the workflow configuration issue through analysis
**Result**: Found root cause but haven't implemented fix yet
**Files Modified**: None yet
**Key Learning**: The issue is in test reporter configuration and workflow artifact handling, not the tests themselves

### Attempt 2: [Not yet attempted]
**Approach**: Will need to fix test reporter configuration
**Result**: Pending
**Files Modified**: Will likely need .github/workflows/pr-validation.yml changes
**Key Learning**: Need to separate test result ownership from artifact source

### Attempt 3: [Not yet attempted]
**Approach**: May need to add unit test summary to PR validation
**Result**: Pending
**Files Modified**: Pending
**Key Learning**: Pending

## Strategic Changes (DO NOT ROLLBACK)
- [ ] File: .github/workflows/pr-validation.yml - Lines: TBD - Change: [Any test reporter improvements] - Reason: [Improves CI/CD transparency and developer experience]
- [ ] File: .github/workflows/feature-branch-tests.yml - Lines: TBD - Change: [Any workflow metadata improvements] - Reason: [Clarifies test execution responsibility]

## Current Workaround
Developers must check both "Feature Branch Tests" and "PR Validation" sections to get complete test picture, and manually correlate which tests ran where.

## Next Steps
- [ ] Fix test reporter configuration to properly associate integration tests with PR validation workflow
- [ ] Add unit test result summary to PR validation workflow (either re-run or display from artifacts)
- [ ] Ensure clear separation between feature-level testing (immediate feedback) and PR-level testing (comprehensive validation)
- [ ] Update workflow documentation to clarify test execution flow and reporting ownership

## Related Issues
- Link to related blocking issue: None yet
- Link to GitHub PR: #215 (multi-config E2E testing implementation)
- Link to spec task: docs/03-Development/specs/2025-09-20-multi-config-e2e-testing/