# Agent OS Status Tracking

This directory contains progress tracking and status information for Agent OS specifications.

## Structure

- `current-status.md` - Current overall status of all active specifications
- `history/` - Historical status snapshots (optional, for future implementation)

## Usage

The status tracking system automatically updates when tasks are executed or reviewed:

1. **Automatic Updates**: Status is updated when tasks are marked complete
2. **Manual Updates**: Run `node .agent-os/status-aggregator.js` to refresh
3. **View Status**: Check `current-status.md` for latest progress

## Status Format

The `current-status.md` file contains:
- Active specifications with task completion counts
- Recent review results with severity counts  
- Blocked items with reasons and timestamps
- Overall progress percentage

## Integration

Status tracking integrates with:
- `project-manager` agent - Updates status after task completion
- `review-agent` - Records review results
- `document-blocking-issue` agent - Tracks blocked items