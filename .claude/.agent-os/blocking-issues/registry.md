# Blocking Issues Registry

This registry tracks all blocking issues that prevent task completion following ITIL Problem Management standards.

## Active Issues

| ID | Created | Spec | Category | Description | Status |
|---|---|---|---|---|---|
| BI-2025-09-10-001 | 2025-09-10 | e2e-testing | test | E2E Test Failures in Staging Deployment Pipeline | active |

## Resolved Issues

| ID | Created | Resolved | Spec | Category | Description | Resolution |
|---|---|---|---|---|---|---|
| | | | | | | |

## Issue Categories
- **test**: Testing framework, test execution, or test environment issues
- **build**: Build system, compilation, or packaging issues
- **deployment**: Deployment pipeline or environment issues
- **functionality**: Core application functionality failures
- **configuration**: Configuration or environment setup issues

## Severity Levels
- **critical**: Blocks multiple tasks, security implications, or production impact
- **high**: Blocks important functionality or deployment pipeline
- **medium**: Blocks specific features but workarounds exist
- **low**: Minor issues with available workarounds

## Quick Reference
- Active issues location: `.claude/.agent-os/blocking-issues/active/`
- Resolved issues location: `.claude/.agent-os/blocking-issues/resolved/`
- Protected changes: `.claude/.agent-os/blocking-issues/protected_changes.json`

## Usage
1. Document blocking issues after 3 failed attempts or 30 minutes elapsed
2. Use standardized template and perform Five Whys root cause analysis
3. Protect strategic improvements that must not be rolled back
4. Update registry when issues are created or resolved
5. Move resolved issues to resolved/ directory when fixed