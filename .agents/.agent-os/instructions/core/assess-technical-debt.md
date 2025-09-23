# Assess Technical Debt - Targeted Analysis for Established Systems

## Purpose
Systematically identify, categorize, and prioritize technical debt in specific areas of an established codebase to support informed refactoring decisions.

## When to Use
- Before major feature development
- After completing sprint retrospectives
- When performance issues arise
- During quarterly code health reviews
- Before team capacity planning
- When onboarding new team members

## Input Requirements
- Specific module/component/layer to analyze
- Recent performance metrics or user complaints
- Current team velocity and capacity
- Business priorities and timelines

## Technical Debt Assessment Framework

### 1. Scope Definition (5 minutes)

**Analysis Boundary:**
```bash
# Define what to analyze
TARGET_AREA="src/App/Features/Authentication"  # Example
find $TARGET_AREA -type f -name "*.cs" | wc -l
find $TARGET_AREA -type f -name "*.ts" | wc -l
```

**Focus Areas (Choose 1-3):**
- [ ] Specific feature module
- [ ] Cross-cutting concerns (logging, caching, auth)
- [ ] Performance bottlenecks
- [ ] Testing gaps
- [ ] Legacy integration points

### 2. Code Quality Metrics (15 minutes)

**Complexity Analysis:**
```bash
# C# cyclomatic complexity (if tools available)
# Look for methods with high complexity
grep -rn "if\|switch\|for\|while\|foreach" $TARGET_AREA --include="*.cs" | wc -l

# TypeScript complexity indicators
grep -rn "if\|switch\|for\|while" $TARGET_AREA --include="*.ts" | wc -l
```

**Code Smell Indicators:**
- **Large Classes:** > 300 lines
- **Long Methods:** > 20 lines
- **High Coupling:** Many dependencies
- **Low Cohesion:** Unrelated responsibilities
- **Duplicated Code:** Similar logic in multiple places

**Pattern Violations:**
- SOLID principle violations
- Clean Architecture boundary violations
- Inconsistent naming conventions
- Missing error handling
- Hardcoded values

### 3. Dependency Analysis (10 minutes)

**Internal Dependencies:**
```bash
# Map internal coupling
grep -r "using.*App\." $TARGET_AREA --include="*.cs" | cut -d: -f2 | sort | uniq -c | sort -nr
grep -r "import.*from.*app" $TARGET_AREA --include="*.ts" | cut -d: -f2 | sort | uniq -c | sort -nr
```

**External Dependencies:**
```bash
# Check for outdated packages
cd src/Angular && npm outdated
dotnet list package --outdated
```

**Circular Dependencies:**
- Layer violations (UI calling Infrastructure directly)
- Circular service dependencies
- Mutual class dependencies

### 4. Testing Debt Assessment (15 minutes)

**Test Coverage Gaps:**
```bash
# Find files without corresponding tests
find $TARGET_AREA -name "*.cs" -not -path "*/bin/*" | while read file; do
  testfile=$(echo $file | sed 's/src\//test\/Tests.Unit.Backend\//' | sed 's/\.cs$/Tests.cs/')
  [ ! -f "$testfile" ] && echo "Missing test: $file"
done
```

**Test Quality Issues:**
- Integration tests testing too much
- Unit tests with external dependencies
- Flaky tests (timing-dependent)
- Tests that don't assert meaningful behavior
- Missing edge case testing

**Test Maintainability:**
- Hard-to-understand test setup
- Duplicated test infrastructure
- Tests tightly coupled to implementation

### 5. Performance & Scalability Debt (10 minutes)

**Database Access Patterns:**
```bash
# Look for N+1 query patterns
grep -rn "foreach.*\." $TARGET_AREA --include="*.cs" | grep -i "repository\|dbcontext"

# Check for missing async/await
grep -rn "\.Result\|\.Wait(" $TARGET_AREA --include="*.cs"
```

**Common Performance Issues:**
- Synchronous calls in async context
- Missing database indexes
- Inefficient LINQ queries
- Large object graphs in memory
- Missing caching opportunities

**Scalability Concerns:**
- Hardcoded limits or timeouts
- Single points of failure
- Resource leaks (undisposed objects)
- Thread safety issues

### 6. Security & Maintainability Debt (10 minutes)

**Security Debt:**
```bash
# Look for potential security issues
grep -rn "TODO\|HACK\|FIXME" $TARGET_AREA
grep -rn "Password\|Secret\|Key" $TARGET_AREA --include="*.cs" --include="*.ts"
```

**Maintainability Issues:**
- Commented-out code
- Dead code (unreferenced methods)
- Magic numbers and strings
- Inconsistent error handling
- Missing documentation for complex logic

## Debt Categorization Matrix

### Impact vs Effort Assessment

| Category | Impact | Effort | Priority | Examples |
|----------|--------|--------|----------|----------|
| **Quick Wins** | Low-Medium | Low | High | Code formatting, dead code removal |
| **Major Projects** | High | High | Medium | Architecture refactoring, database redesign |
| **Fill-ins** | Low | Low | Low | Documentation updates, minor optimizations |
| **Questionable** | Low | High | Never | Over-engineering, premature optimizations |

### Business Impact Classification

**High Business Impact:**
- Security vulnerabilities
- Performance bottlenecks affecting users
- Bugs causing data loss or corruption
- Scalability blockers for business growth

**Medium Business Impact:**
- Developer productivity issues
- Maintenance cost increases
- Feature development velocity reduction
- Testing instability

**Low Business Impact:**
- Code style inconsistencies
- Minor performance improvements
- Theoretical future problems
- Non-critical missing features

## Output Deliverable

### Technical Debt Assessment Report

```markdown
# Technical Debt Assessment Report
**Area Analyzed:** [Specific component/module]
**Assessment Date:** [Date]
**Assessor:** [Name]
**Analysis Scope:** [What was included/excluded]

## Executive Summary
- **Overall Debt Level:** [High/Medium/Low]
- **Primary Concern:** [Biggest issue identified]
- **Recommended Action:** [Immediate priority]
- **Estimated Effort:** [Time to address top items]

## Debt Inventory

### Critical Issues (Fix Now)
1. **[Issue Name]**
   - **Location:** [File/method/component]
   - **Problem:** [Specific description]
   - **Impact:** [Business/technical impact]
   - **Effort:** [Estimated hours/days]
   - **Risk if ignored:** [What happens if not fixed]

### High Priority (Next Sprint)
[Same format as critical]

### Medium Priority (Next Quarter)
[Same format as critical]

### Low Priority (Backlog)
[Same format as critical]

## Metrics Summary

### Code Quality
- **Complexity Hotspots:** [List top 5 most complex files/methods]
- **Duplication:** [Estimated percentage of duplicated code]
- **SOLID Violations:** [Count by principle]
- **Architecture Violations:** [Cross-layer dependencies found]

### Testing Debt
- **Coverage Gaps:** [X files without tests]
- **Flaky Tests:** [List problematic tests]
- **Integration Test Scope:** [Tests that should be unit tests]
- **Missing Test Types:** [What test scenarios are missing]

### Performance Debt
- **Database Issues:** [N+1 queries, missing indexes]
- **Memory Issues:** [Large objects, potential leaks]
- **Async/Await Problems:** [Blocking calls identified]
- **Caching Opportunities:** [Where caching could help]

### Security & Maintainability
- **Security Concerns:** [Potential vulnerabilities]
- **Documentation Gaps:** [Complex code without comments]
- **Dead Code:** [Unreferenced classes/methods]
- **Configuration Issues:** [Hardcoded values, missing settings]

## Prioritization Recommendation

### Phase 1: Immediate (This Sprint)
- **Focus:** Critical issues blocking current work
- **Effort:** [X developer days]
- **Items:** [List top 3-5 items]

### Phase 2: Short-term (Next 2 Sprints)
- **Focus:** High-impact, medium-effort improvements
- **Effort:** [X developer days]
- **Items:** [List items]

### Phase 3: Medium-term (Next Quarter)
- **Focus:** Architectural improvements, major refactoring
- **Effort:** [X developer days]
- **Items:** [List items]

### Ongoing: Continuous Improvement
- **Focus:** Prevent new debt accumulation
- **Items:** [Code review standards, automated tools]

## Cost-Benefit Analysis

### Cost of Addressing Debt
- **Development Time:** [Total estimated effort]
- **Testing Time:** [Additional testing needed]
- **Risk:** [Potential for introducing bugs]
- **Opportunity Cost:** [Features not built while fixing debt]

### Cost of Ignoring Debt
- **Velocity Impact:** [Estimated slowdown percentage]
- **Maintenance Cost:** [Ongoing burden]
- **Risk:** [Potential for major failures]
- **Business Impact:** [Customer/user experience degradation]

## Implementation Strategy

### Team Capacity Planning
- **Available Capacity:** [X% of sprint capacity for debt]
- **Skill Requirements:** [Specific expertise needed]
- **Knowledge Transfer:** [Documentation/pairing needs]

### Integration with Feature Work
- **Opportunistic Refactoring:** [Clean while building features]
- **Boy Scout Rule:** [Leave code better than found]
- **Dedicated Time:** [X hours per sprint for debt]

### Monitoring & Prevention
- **Automated Tools:** [Static analysis, code metrics]
- **Code Review Focus:** [Specific debt patterns to watch]
- **Regular Assessment:** [Monthly/quarterly reviews]

## Success Metrics

### Short-term (1-3 months)
- Reduce critical issues by 80%
- Improve test coverage to X%
- Eliminate top 3 performance bottlenecks

### Medium-term (3-6 months)
- Increase development velocity by X%
- Reduce bug report volume by X%
- Improve code review cycle time

### Long-term (6-12 months)
- Maintain low technical debt levels
- Establish sustainable development practices
- Improve team satisfaction with codebase
```

## Success Criteria
- Debt assessment is specific and actionable
- Prioritization aligns with business priorities
- Effort estimates are realistic and justified
- Implementation strategy fits team capacity
- Success metrics are measurable and time-bound

## Integration with Development Process
- Run before major feature development
- Include findings in sprint planning
- Track progress in team retrospectives
- Use to justify refactoring time to stakeholders
- Feed results into plan-refactoring.md for execution