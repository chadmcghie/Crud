# Agent OS Learning Cache

This directory contains accumulated knowledge from resolved issues and common patterns encountered during development.

## Structure

- `patterns.md` - Common issue-solution pairs organized by category
- `memory-references.md` - References to important memory IDs for context
- `solutions/` - Detailed solution guides for complex issues (future)

## Purpose

The learning cache helps Agent OS:
1. **Quickly resolve known issues** - Match error patterns to proven solutions
2. **Avoid repeated mistakes** - Learn from past failures
3. **Share knowledge** - Accumulate team wisdom over time
4. **Improve efficiency** - Reduce time debugging known problems

## Categories

### Testing Patterns
- Database locking issues
- Test isolation problems
- Flaky test solutions
- Performance optimizations

### Build Patterns
- Compilation optimizations
- Dependency issues
- Configuration problems
- CI/CD solutions

### Error Patterns
- Common runtime errors
- Framework-specific issues
- Integration problems
- Security vulnerabilities

## Usage

The learning cache is automatically consulted by:
- `troubleshoot-with-history` agent - Checks for known solutions
- `review-agent` - References common issues during reviews
- `orchestrator` agent - Routes to appropriate solutions

## Updating

Patterns are added:
1. **Manually** - When resolving new issues
2. **Automatically** - From completed GitHub issues
3. **Via agents** - During troubleshooting sessions

## Search Capabilities

Patterns can be searched by:
- Category (testing, build, errors)
- Tags (sqlite, angular, dotnet)
- Error message matching
- Solution keywords