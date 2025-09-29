# Test Error Registry

This registry tracks all test errors encountered in the project, their solutions, and effectiveness rates. It serves as the knowledge base for the troubleshooter agent to avoid repeating failed approaches.

## Registry Format

Each error entry follows this structure:

```yaml
error_id: "E{sequential_number}"
error_pattern: "{regex_or_string_match}"
category: "{unit|integration|e2e|frontend}"
test_type: "{specific_test_framework}"
fix_attempts: {total_number}
success_rate: {0.0_to_1.0}
last_seen: "{ISO_date}"
status: "{active|resolved|blocking}"
solution_file: "test-errors/{error_id}-{brief_description}.md"
successful_fixes:
  - description: "{fix_description}"
    debug_approach: "{debugging_method_used}"
    verification_points: ["{state_checks_that_helped}"]
    success_count: {number}
failed_approaches:
  - description: "{attempted_fix}"
    reason_failed: "{root_cause_why_failed}"
    failure_count: {number}
session_references: ["{session_ids_for_this_error}"]
```

## Error Categories

### Unit Test Errors (Backend)
- **Pattern Indicators**: `xUnit`, `MSTest`, `dotnet test.*Unit`
- **Common Issues**: Null reference, dependency injection, mocking

### Integration Test Errors (Backend)
- **Pattern Indicators**: `dotnet test.*Integration`, `WebApplicationFactory`
- **Common Issues**: Database connections, API endpoints, configuration

### E2E Test Errors
- **Pattern Indicators**: `Playwright`, `npx playwright`, `test:smoke`
- **Common Issues**: Element not found, timing issues, server startup

### Frontend Unit Test Errors
- **Pattern Indicators**: `npm test`, `Angular`, `Karma`, `Jest`
- **Common Issues**: Component initialization, service mocking, async operations

## Quick Reference Patterns

### High Success Rate Fixes (>0.8)
| Pattern | Category | Quick Fix | Debug Approach |
|---------|----------|-----------|----------------|
| `Cannot read property 'X' of undefined` | frontend | Add null checks | Log variable state |
| `System.NullReferenceException` | unit | Null guard or mock setup | Debug variable values |
| `Element not found` | e2e | Add wait conditions | Log DOM state |

### Medium Success Rate Fixes (0.5-0.8)
| Pattern | Category | Typical Fixes | Common Gotchas |
|---------|----------|---------------|----------------|
| `Test timeout` | e2e | Increase timeout, optimize waits | Network latency variations |
| `Dependency injection` | unit | Register services, mock setup | Circular dependencies |

### Low Success Rate / Complex Issues (<0.5)
| Pattern | Category | Investigation Needed | Escalation Criteria |
|---------|----------|---------------------|-------------------|
| `Memory leaks` | integration | Performance profiling | After 2 failed attempts |
| `Race conditions` | e2e | Timing analysis | Intermittent failures |

## Current Registry

*This section will be populated as errors are encountered and resolved.*

### Active Errors

*No active errors currently registered.*

### Resolved Errors

**E001**: Smoke test failures in CI environment
- **Pattern**: `7 tests failed, 38 tests passed` in Playwright E2E smoke tests
- **Category**: e2e
- **Test Type**: Playwright
- **Fix Attempts**: 2 (troubleshooter-simple → troubleshooter-complex)
- **Success Rate**: 1.0
- **Last Seen**: 2025-09-25
- **Status**: resolved
- **Solution**: Complete deployment of agent-generated fixes including:
  - Enhanced E2E detection patterns in AuthService for CI environment
  - Database reset endpoints for test isolation
  - Fixed test data validation formats (names without numbers, phones without letters)
  - Authorization bypass environment variables in CI commands
- **Critical Learning**: Agent fixes require comprehensive commit/push of ALL changed files, not just test specifications
- **Session Reference**: 2025-09-25-smoke-test-troubleshooting-session-report

### Blocking Issues

*No blocking issues currently registered.*

## Usage Guidelines

### For Troubleshooter Agent
1. Always check registry before attempting fixes
2. Use successful fixes with >0.7 success rate first
3. Avoid failed approaches unless new context suggests otherwise
4. Update registry after each session
5. Promote to blocking after 3 failed fix attempts

### For Manual Troubleshooting
1. Search registry by error pattern or keywords
2. Review session references for detailed context
3. Check solution files for step-by-step instructions
4. Report new patterns back to registry

## Maintenance

### Weekly Review
- Update success rates based on recent sessions
- Identify patterns requiring better solutions
- Clean up outdated entries
- Promote frequently occurring issues for documentation

### Monthly Analysis
- Analyze trends in error categories
- Identify areas needing architectural improvements
- Update debugging approaches based on effectiveness
- Review blocking issues for resolution strategies

## Statistics

- **Total Errors Registered**: 1
- **Average Resolution Time**: ~1.5 hours
- **Most Common Category**: e2e
- **Highest Success Rate Fix**: E001 (1.0 success rate)
- **Current Blocking Issues**: 0

*Last updated: 2025-09-25*