# Architecture Decisions Registry

## Active Decisions

| ID | Date | Title | Status | Impact |
|----|------|-------|--------|--------|
| ADR-001 | 2025-08-28 | [Serial E2E Testing](./0001-Serial-E2E-Testing.md) | ✅ Implemented | High |
| ADR-002 | 2025-08-29 | [E2E Database Performance Optimization](./0002-E2E-Database-Performance-Optimization.md) | ✅ Implemented | Medium |
| ADR-003 | 2025-08-29 | [E2E Testing Database Use Playwrights webServer](./0003-E2E-Testing-Database-Use-Playwrights-webServer.md) | ✅ Implemented | High |
| ADR-004 | 2025-08-30 | [Angular Async Testing Approach](./0004-Angular-Async-Testing-Approach.md) | ✅ Implemented | Medium |
| ADR-005 | 2025-09-01 | [CI-CD Dependency Management](./0005-CI-CD-Dependency-Management.md) | ✅ Implemented | Medium |

## Decision Categories

### Testing Strategy
- Serial E2E testing approach for CI reliability
- Database isolation strategies
- Angular async testing patterns

### Infrastructure
- CI/CD dependency management
- Database performance optimization

## Usage Guidelines

### Creating New ADRs
1. Use date-based numbering: `YYYY-MM-DD-ADR-XXX-title.md`
2. Follow ADR template structure
3. Update this registry
4. Link from relevant architecture documents

### ADR Template
```markdown
# ADR-XXX: Decision Title

## Status
[Proposed | Accepted | Deprecated | Superseded]

## Context
[Describe the situation requiring a decision]

## Decision
[State the architecture decision]

## Consequences
[Document positive and negative consequences]

## Implementation
[How the decision will be implemented]
```

## Cross-References
- **Architecture Guidelines**: [../1-architecture-guidelines.md](../1-architecture-guidelines.md)
- **Testing Strategy**: [../../04-Quality-Control/1-testing-strategy/registry.md](../../04-Quality-Control/1-testing-strategy/registry.md)