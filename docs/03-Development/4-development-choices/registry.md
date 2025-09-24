# Development Choices Registry

## Active Choices

| ID | Date | Type | Title | Status | Impact |
|----|------|------|-------|--------|--------|
| DC-001 | 2025-08-27 | Pattern | [MediatR CQRS Pattern](./2025-08-27-DC-001-mediatr-cqrs-pattern.md) | ✅ Implemented | High |
| DC-002 | 2025-08-27 | Package | [Polly Improvements](./2025-08-27-DC-002-polly-improvements.md) | ✅ Implemented | Medium |
| DC-003 | 2025-08-27 | Guideline | [Command Handler Checklist](./2025-08-27-DC-003-command-handler-checklist.md) | ✅ Active | Medium |

## Choice Categories

### Patterns & Practices
- CQRS implementation with MediatR
- Command handler development standards
- Repository pattern usage

### Package Choices
- Resilience patterns with Polly
- Dependency injection practices
- Testing framework selections

### Guidelines & Checklists
- Development workflows
- Code quality standards
- Implementation checklists

## Usage Guidelines

### Creating New Development Choices
1. Use date-based naming: `YYYY-MM-DD-DC-XXX-title.md`
2. Categorize: Pattern, Package, Guideline, Standard
3. Update this registry
4. Cross-reference in architecture documents

### Choice Template
```markdown
# Development Choice: Title

**Date**: YYYY-MM-DD
**Type**: [Pattern | Package | Guideline | Standard]
**Status**: [Proposed | Active | Deprecated]

## Context
[Why this choice was made]

## Decision
[What was chosen and rationale]

## Implementation
[How to implement this choice]

## Guidelines
[Specific guidelines for developers]
```

## Cross-References
- **Architecture Decisions**: [../../02-Architecture/Decisions/registry.md](../../02-Architecture/Decisions/registry.md)
- **Product Roadmap**: [../1-product/roadmap.md](../1-product/roadmap.md)
- **Testing Strategy**: [../../04-Quality-Control/1-testing-strategy/registry.md](../../04-Quality-Control/1-testing-strategy/registry.md)