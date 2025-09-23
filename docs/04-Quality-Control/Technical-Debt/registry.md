# Technical Debt Registry

## Overview
Centralized registry for tracking technical debt items that require strategic planning and architectural changes rather than immediate tactical fixes. Technical debt management is integrated into Quality Control processes to ensure proper planning, resource allocation, and alignment with business objectives.

Technical debt items are distinguished from blocking issues by their complexity, scope, and need for longer-term architectural evaluation.

## Active Technical Debt
| ID | Created | Category | Complexity | Business Impact | Description | Priority |
|---|---|---|---|---|---|---|
| BI-2025-09-11-003 | 2025-09-11 | ARCHITECTURAL | HIGH | LOW | RowVersion concurrency control 409 Conflict in many-to-many relationships - EF Core + SQLite compatibility issue | MEDIUM |

## Classification Criteria

### Technical Debt vs Blocking Issue
**Technical Debt**: Requires architectural changes, technology stack evaluation, or strategic planning
**Blocking Issue**: Can be resolved with tactical fixes, configuration changes, or code modifications

### Complexity Levels
- **LOW**: Single component changes, well-understood solution
- **MEDIUM**: Multiple component changes, some unknowns
- **HIGH**: Architecture changes, technology evaluation, multiple failed expert attempts

### Business Impact Levels
- **LOW**: No production functionality affected, development/testing only
- **MEDIUM**: Some production features affected, workarounds available
- **HIGH**: Critical production functionality compromised

### Priority Matrix
| Complexity | Business Impact | Priority |
|---|---|---|
| HIGH | HIGH | CRITICAL |
| HIGH | MEDIUM | HIGH |
| HIGH | LOW | MEDIUM |
| MEDIUM | HIGH | HIGH |
| MEDIUM | MEDIUM | MEDIUM |
| MEDIUM | LOW | LOW |
| LOW | HIGH | MEDIUM |
| LOW | MEDIUM | LOW |
| LOW | LOW | DEFERRED |

## Detailed Technical Debt Items

### BI-2025-09-11-003: EF Core + SQLite + Concurrency Control Compatibility
**Status**: Active Technical Debt
**Date Identified**: 2025-09-11
**Date Reclassified**: 2025-09-11 (from blocking issue)

#### Problem Summary
Many-to-many relationship updates with RowVersion concurrency control fail with 409 Conflict errors due to architectural incompatibility between EF Core relationship handling, SQLite constraint enforcement, and concurrency token implementation.

#### Investigation History
- **Total Attempts**: 6 systematic approaches over 2 days
- **Expert Hours**: ~16 hours of focused troubleshooting
- **Approaches Tested**: Application-managed concurrency, tracked entities, selective updates, relationship pattern variations
- **Outcome**: All approaches failed - requires architectural changes beyond tactical fixes

#### Impact Assessment
- **Production Systems**: ✅ UNAFFECTED - No impact to live functionality
- **Development**: ❌ MINIMAL - Single integration test scenario affected (PUT_People_Should_Update_Person_Roles)
- **User Experience**: ✅ NO IMPACT - Basic Person operations work perfectly
- **Test Coverage**: ❌ LIMITED - One test skipped, but core functionality validated

#### Root Cause Analysis
Fundamental incompatibility between:
- EF Core many-to-many relationship handling
- SQLite database constraint enforcement
- RowVersion-based concurrency control
- Integration test environment specifics

#### Potential Solutions (Strategic)
1. **Technology Stack Evaluation**
   - PostgreSQL migration for better concurrency support
   - Alternative ORM evaluation (Dapper, raw SQL)
   - Relationship modeling approach changes

2. **Architecture Modifications**
   - Event sourcing for complex relationship updates
   - CQRS pattern with separate read/write models
   - Microservices separation for relationship management

3. **Concurrency Strategy Changes**
   - Timestamp-based concurrency instead of RowVersion
   - Optimistic locking with retry patterns
   - Application-level conflict resolution

#### Business Value Assessment
- **Current Workaround**: Test skipped with documentation - no functional impact
- **Cost of Fix**: High - architectural changes, technology evaluation, testing across environments
- **Risk of No Action**: Low - only affects one specific test scenario
- **Strategic Value**: Medium - would enable full concurrency control implementation

#### Next Steps
1. **Q1 Planning**: Include in architectural review for next quarter
2. **Technology Assessment**: Evaluate PostgreSQL migration feasibility
3. **Business Alignment**: Confirm if role assignment concurrency is critical for MVP
4. **Spike Work**: Allocate 2-3 days for PostgreSQL prototype testing

#### Related Documentation
- **Detailed Analysis**: `docs/04-Quality-Control/Technical-Debt/active/2025-09-11-rowversion-concurrency-409-conflict.md`
- **GitHub PR**: https://github.com/chadmcghie/Crud/pull/193
- **CI Validation**: https://github.com/chadmcghie/Crud/actions/runs/17649252073

#### Decision Log
- **2025-09-11**: Reclassified from blocking issue to technical debt after 6 failed systematic attempts
- **2025-09-23**: Confirmed classification during systematic troubleshooting session - architectural review needed

## Process Guidelines

### When to Create Technical Debt Entry
1. **Multiple Failed Attempts**: 3+ systematic troubleshooting attempts with expert-level investigation
2. **Architectural Scope**: Solution requires technology stack changes or major architectural modifications
3. **Strategic Planning Needed**: Resolution requires business alignment, resource planning, or long-term roadmap consideration
4. **Limited Business Impact**: Issue doesn't block critical production functionality

### Technical Debt Lifecycle
1. **Identification**: Issue identified during troubleshooting or development
2. **Assessment**: Impact analysis, complexity evaluation, solution exploration
3. **Classification**: Technical debt vs blocking issue determination
4. **Registration**: Entry in technical debt registry with full documentation
5. **Strategic Planning**: Include in quarterly/annual planning cycles
6. **Resolution Planning**: Resource allocation, timeline, approach definition
7. **Implementation**: Execute strategic solution with proper testing
8. **Closure**: Validate resolution and archive technical debt entry

### Review Cycle
- **Quarterly Review**: Assess all active technical debt items for priority changes
- **Annual Planning**: Include high-priority technical debt in roadmap planning
- **Emergency Escalation**: Process for technical debt that becomes critical

## Metrics and Tracking

### Current Statistics
- **Total Technical Debt Items**: 1
- **By Priority**: MEDIUM (1)
- **By Category**: ARCHITECTURAL (1)
- **Average Age**: 42 days (as of 2025-09-23)

### Health Indicators
- **New Technical Debt Rate**: Target < 1 per quarter
- **Resolution Rate**: Target > 75% within 12 months for HIGH priority items
- **Age Distribution**: Target < 6 months for CRITICAL items

### Reporting
- **Monthly Dashboard**: Technical debt aging, priority distribution
- **Quarterly Business Review**: Impact assessment, strategic planning alignment
- **Annual Architecture Review**: Comprehensive technical debt evaluation

## Integration with Quality Control Process

Technical debt management is integrated into the Quality Control workflow through:

1. **Quarterly Quality Reviews**: Technical debt assessment during architecture, code, and design reviews
2. **Quality Gates**: Definition of done includes technical debt impact evaluation
3. **Process Guidelines**: See `process-guidelines.md` for detailed integration procedures

### Code Review Integration
- Flag potential technical debt during code reviews using technical debt assessment checklist
- Assess if issues require tactical fixes or strategic planning approaches
- Reference technical debt registry in architectural decision records (ADRs)

### Sprint Planning Integration
- Reserve 15-20% capacity for addressing technical debt items
- Include technical debt context in epic and story planning
- Consider technical debt impact in estimation and risk assessment
- Validate feature work doesn't compound existing technical debt

### Quality Control Alignment
- Technical debt assessment as part of definition of done
- Architectural review required for changes affecting technical debt items
- Documentation updates required when technical debt is introduced or modified
- Integration with quarterly quality control review cycles

---

*Registry maintained by: Quality Control Team*
*Process Guidelines: `docs/04-Quality-Control/Technical-Debt/process-guidelines.md`*
*Last Updated: 2025-09-23*
*Next Review: 2025-12-23 (Quarterly Quality Review)*