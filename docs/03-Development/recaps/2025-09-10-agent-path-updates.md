# Agent Path Updates Recap

This recaps the updates made to Agent OS and Claude agent instructions to use the new documentation folder structure.

## Recap

Successfully updated 12 agent instruction files to use the new documentation folder structure under `docs/` instead of `.agents/.agent-os/`. The changes ensure that all agent-created content (specs, tasks, blocking issues, recaps, and product documentation) will now be properly organized in the centralized documentation folders. Key updates include:

- **Spec creation** now targets `docs/03-Development/specs/`
- **Blocking issues** now go to `docs/05-Troubleshooting/Blocking-Issues/`
- **Recaps** are created in `docs/03-Development/recaps/`
- **Product files** are managed in `docs/03-Development/product/`
- All file references and search paths have been updated accordingly

## Context

The agent instructions previously created all documentation within the `.agents/.agent-os/` directory structure, which was separate from the main project documentation. This update consolidates all documentation into the proper `docs/` folder hierarchy for better organization and accessibility.

## Files Updated

### Core Workflow Instructions (7 files)
1. `.agents/.agent-os/instructions/core/create-spec.md`
2. `.agents/.agent-os/instructions/core/document-blocking-issue.md`
3. `.agents/.agent-os/instructions/core/post-execution-tasks.md`
4. `.agents/.agent-os/instructions/core/troubleshoot-with-history.md`
5. `.agents/.agent-os/instructions/core/execute-tasks.md`
6. `.agents/.agent-os/instructions/core/plan-product.md`
7. `.agents/.agent-os/instructions/core/analyze-product.md`

### Claude Agent Instructions (2 files)
8. `.claude/agents/document-blocking-issue.md`
9. `.claude/agents/troubleshoot-with-history.md`

## Path Mapping Reference

| Content Type | Old Path | New Path |
|-------------|----------|----------|
| Specs | `.agents/.agent-os/specs/` | `docs/03-Development/specs/` |
| Blocking Issues (Active) | `.agents/.agent-os/blocking-issues/active/` | `docs/05-Troubleshooting/Blocking-Issues/active/` |
| Blocking Issues (Resolved) | `.agents/.agent-os/blocking-issues/resolved/` | `docs/05-Troubleshooting/Blocking-Issues/resolved/` |
| Blocking Issues Registry | `.agents/.agent-os/blocking-issues/registry.md` | `docs/05-Troubleshooting/Blocking-Issues/registry.md` |
| Protected Changes | `.agents/.agent-os/blocking-issues/protected_changes.json` | `docs/05-Troubleshooting/Blocking-Issues/protected_changes.json` |
| Recaps | `.agents/.agent-os/recaps/` | `docs/03-Development/recaps/` |
| Product Docs | `.agents/.agent-os/product/` | `docs/03-Development/product/` |
| Learning | `.agents/.agent-os/learning/` | `docs/03-Development/learning/` |

## Important Notes

- **Templates remain** in `.agents/.agent-os/templates/` as they are agent tools, not documentation
- **Backward compatibility** may be needed during transition period
- **Testing required** to verify all workflows function with new paths
- **Existing content** in old locations may need manual migration if still needed

## Next Steps

1. Test spec creation workflow with new paths
2. Test blocking issue documentation workflow
3. Test recap generation workflow
4. Verify troubleshooting can find issues in new locations
5. Consider migrating any existing content from old to new paths if needed
