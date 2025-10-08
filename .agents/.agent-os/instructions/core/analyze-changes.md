# Analyze Changes - Impact Assessment for Ongoing Development

## Purpose
Analyze recent changes or proposed modifications to understand their impact on the existing system architecture, dependencies, and workflow.

## When to Use
- Before/after major feature implementations
- Code review preparation for complex changes
- Post-incident analysis
- Quarterly architecture health checks
- Before planning next sprint/iteration

## Input Requirements
- Git commit range or branch to analyze
- Specific files/components if targeted analysis
- Time period for change analysis (last sprint, last month, etc.)

## Analysis Framework

### 1. Change Scope Assessment (5 minutes)

```bash
# Analyze git changes
git log --oneline --since="2 weeks ago" --stat
git diff HEAD~10..HEAD --name-only | sort | uniq -c | sort -nr
```

**Output:**
- Files changed frequency ranking
- Change distribution across layers
- Hotspot identification

### 2. Architectural Layer Impact (10 minutes)

For each changed file, categorize by Clean Architecture layer:

**Domain Layer Changes:**
- Entity modifications → Breaking changes risk
- New domain concepts → Integration complexity
- Business rule changes → Testing requirements

**Application Layer Changes:**
- CQRS handler modifications → API contract impact
- Service changes → Dependency chain analysis
- DTO modifications → Frontend impact

**Infrastructure Layer Changes:**
- Repository changes → Data access impact
- Migration files → Database schema risk
- External service integration → Reliability impact

**API Layer Changes:**
- Controller modifications → Breaking API changes
- Endpoint additions → Documentation updates needed
- Authentication changes → Security review required

**UI Layer Changes:**
- Component modifications → User experience impact
- Service changes → State management review
- Routing changes → Navigation flow analysis

### 3. Dependency Impact Analysis (10 minutes)

**Backward Compatibility:**
```bash
# Check for breaking changes in public interfaces
git diff HEAD~5..HEAD -- "**/*Service.cs" "**/*Repository.cs" | grep -E "^[-+].*public"
```

**Cross-Layer Dependencies:**
- Map changed interfaces to implementations
- Identify affected test suites
- Check configuration dependencies

**External Dependencies:**
```bash
# Check package.json and .csproj changes
git diff HEAD~5..HEAD -- "**/package*.json" "**/*.csproj"
```

### 4. Risk Assessment Matrix (5 minutes)

| Risk Level | Criteria | Examples |
|------------|----------|----------|
| **High** | Breaking changes, data schema, auth | Entity removal, JWT config, DB migrations |
| **Medium** | API changes, new dependencies | New endpoints, package updates |
| **Low** | UI changes, internal refactoring | Component styling, private method changes |

### 5. Testing Impact Assessment (10 minutes)

**Test Coverage Changes:**
```bash
# Identify test files that need updates
git diff HEAD~5..HEAD --name-only | grep -E "\.(spec|test)\."
```

**Testing Strategy Required:**
- **Unit Tests:** Changed business logic, new services
- **Integration Tests:** API changes, database modifications
- **E2E Tests:** UI changes, user workflow modifications
- **Performance Tests:** Infrastructure changes, new queries

### 6. Documentation Impact (5 minutes)

**Update Requirements:**
- API documentation (OpenAPI/Swagger)
- Architecture decision records (ADRs)
- README and setup instructions
- Deployment procedures

## Output Deliverable

### Change Impact Report Template

```markdown
# Change Impact Analysis Report
**Date:** [Date]
**Scope:** [Git range or description]
**Analyst:** [Name]

## Executive Summary
- **Risk Level:** [High/Medium/Low]
- **Breaking Changes:** [Yes/No - list if yes]
- **Testing Required:** [Test types needed]
- **Deployment Impact:** [Special considerations]

## Layer-by-Layer Impact

### Domain Layer
- [Changes and impacts]

### Application Layer
- [Changes and impacts]

### Infrastructure Layer
- [Changes and impacts]

### API Layer
- [Changes and impacts]

### UI Layer
- [Changes and impacts]

## Risk Mitigation

### High Priority Actions
1. [Action item with owner]
2. [Action item with owner]

### Medium Priority Actions
1. [Action item with owner]
2. [Action item with owner]

## Testing Strategy
- [ ] Unit tests for [specific areas]
- [ ] Integration tests for [specific endpoints]
- [ ] E2E tests for [specific workflows]
- [ ] Performance tests for [specific scenarios]

## Dependencies & Rollback Plan
- **Dependencies:** [What must be deployed together]
- **Rollback Strategy:** [How to safely revert]
- **Feature Flags:** [Any toggles needed]

## Next Steps
1. [Immediate actions]
2. [Follow-up tasks]
3. [Monitoring requirements]
```

## Success Criteria
- All architectural layers analyzed for impact
- Risk level clearly identified and justified
- Testing strategy aligned with change scope
- Clear action items with owners assigned
- Rollback plan documented for high-risk changes

## Integration with Development Workflow
- Run after feature branch completion
- Include in pull request reviews
- Use for release planning
- Reference during incident post-mortems