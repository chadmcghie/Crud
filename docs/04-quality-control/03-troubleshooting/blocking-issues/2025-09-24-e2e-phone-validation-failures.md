# BI-2025-09-24-002: E2E Phone Number Validation Failures

## Issue Summary
**ID**: BI-2025-09-24-002
**Created**: 2025-09-24
**Severity**: MEDIUM
**Category**: Validation
**Status**: ACTIVE

## Description
E2E tests are failing when creating people with phone numbers that should be valid. The API returns 400 Bad Request with validation errors for phone number format, even when using properly formatted phone numbers.

## Symptoms
```
People creation status: 400
Response text: {
  "type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title":"One or more validation errors occurred.",
  "status":400,
  "errors":{
    "Phone":["Phone number must be a valid format"]
  },
  "traceId":"00-1609f95fcc6631fee48af86e0d8895d0-1623682135f25839-01"
}
❌ People creation failed - this should work in Testing environment
```

## Root Cause Analysis
Phone number validation is rejecting formats that should be valid in the Testing environment. Possible causes:
1. **Validation Rules Too Strict**: Phone validation regex/rules are overly restrictive
2. **Test Data Format Issues**: Test is using phone format that doesn't match validation expectations
3. **Environment-Specific Validation**: Validation behaves differently in Testing vs other environments

## Impact
- E2E tests cannot create test people with phone numbers
- Blocks workflow testing that requires valid people records
- Affects data creation and API validation testing

## Affected Tests
- `tests/environment-test.spec.ts:34:7` - Environment Detection Tests › should allow creating people without authorization in Testing
- Potentially other tests that create people with phone numbers

## Technical Analysis
The error suggests the phone number validation is working, but the format being used in tests doesn't match the expected validation pattern. Need to investigate:
1. What phone number format is being used in tests
2. What validation pattern is configured in the API
3. Whether Testing environment should bypass or relax phone validation

## Example Phone Formats in Use
From test helpers, common formats used:
- `+1-555-0123` (with dashes and country code)
- Various faker-generated formats

## Resolution Options
1. **Fix Test Data**: Update tests to use phone formats that match validation rules
2. **Relax Validation**: Update phone validation to accept more formats
3. **Environment-Specific Rules**: Make Testing environment more permissive for phone validation
4. **Update Validation Pattern**: Modify phone validation regex to accept test formats

## Related Issues
- Related to BI-2025-09-24-001 (environment detection issues)
- May affect other data validation patterns

## Context
This issue emerged after resolving E2E navigation issues, revealing data validation problems that were previously masked by navigation failures.