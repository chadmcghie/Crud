# Compromised Tests List

**Generated**: 2025-01-28  
**Issue**: #141 - Complete list of all compromised tests  

## Overview

This document provides a comprehensive list of all tests that are being skipped or compromised due to conditional logic in the CI/CD pipeline and test configuration.

## 🚨 Completely Skipped Test Suites

### 1. E2E Tests for Dev Branch PRs
**Condition**: `github.event.pull_request.base.ref != 'main'`  
**Location**: `.github/workflows/pr-validation.yml:376`  
**Tests Affected**: Entire E2E smoke test suite (~22 tests)

**Compromised Tests**:
- Authentication flow tests
- Person CRUD operation tests  
- Role management tests
- API health check tests
- Angular application startup tests
- Database integration tests
- All user journey scenarios

### 2. Feature Branch Test Suite (When PR Exists)
**Condition**: `has_open_pr == 'true'`  
**Location**: `.github/workflows/feature-branch-tests.yml:56`  
**Tests Affected**: All feature branch validation tests

**Compromised Tests**:
- Backend unit tests (quick validation)
- Angular linting
- Angular unit tests
- Build validation
- Artifact creation verification

### 3. Cross-Browser E2E Tests
**Condition**: `process.env.CROSS_BROWSER !== 'true'`  
**Location**: `test/Tests.E2E.NG/playwright.config.ts:151-160`  
**Tests Affected**: All E2E tests in Firefox and Safari/WebKit

**Compromised Tests**:
- All smoke tests in Firefox
- All critical tests in Firefox
- All extended tests in Firefox
- All smoke tests in Safari/WebKit
- All critical tests in Safari/WebKit
- All extended tests in Safari/WebKit

## 🔄 Conditionally Filtered Tests

### 4. Extended Test Suite
**Condition**: `TEST_CATEGORY != 'extended' && TEST_CATEGORY != 'all'`  
**Location**: `test/Tests.E2E.NG/playwright.config.ts:84-86`

**Compromised Tests** (tests marked with `@extended`):
- Performance benchmark tests
- Long-running integration scenarios
- Complex user workflow tests
- Edge case validation tests
- System stress tests

### 5. Critical Test Suite (in Smoke Runs)
**Condition**: `TEST_CATEGORY == 'smoke'`  
**Location**: `test/Tests.E2E.NG/playwright.config.ts:84`

**Compromised Tests** (tests marked with `@critical` but not `@smoke`):
- Integration tests that are important but not in smoke suite
- Business-critical scenarios not marked as smoke
- Important edge cases

### 6. Smoke Test Suite (in Critical/Extended Runs)
**Condition**: `TEST_CATEGORY == 'critical'` or `TEST_CATEGORY == 'extended'`  
**Location**: `test/Tests.E2E.NG/playwright.config.ts:85-86`

**Compromised Tests** (tests marked with `@smoke` but not in current category):
- Basic functionality tests when running critical-only
- Quick validation tests when running extended-only

## 🛠️ Infrastructure/Debug Test Skipping

### 7. Database Debug Operations
**Condition**: `!process.env.DEBUG_DB || !process.env.DATABASE_PATH`  
**Location**: `test/Tests.E2E.NG/tests/fixtures/serial-test-fixture.ts:59-60`

**Compromised Tests**:
- Database size monitoring
- Database state validation
- Performance metrics collection
- Database cleanup verification

### 8. CI Diagnostic Logging
**Condition**: `test.info().retry == 0 && process.env.CI`  
**Location**: `test/Tests.E2E.NG/tests/fixtures/serial-test-fixture.ts:15`

**Compromised Functionality**:
- Database reset logging in CI
- Test execution tracing
- Debugging information collection
- Test failure diagnostics

## 📊 Impact Analysis by Test Category

### High Impact - User-Facing Functionality
- **Authentication tests**: Skipped in dev PRs (E2E level)
- **CRUD operations**: Skipped in dev PRs (E2E level)  
- **Cross-browser compatibility**: Skipped by default
- **User journeys**: Skipped in dev PRs

### Medium Impact - System Integration
- **API integration tests**: Partially skipped based on category
- **Database integration**: Debug aspects skipped
- **Performance tests**: Skipped unless extended category
- **Error handling**: Skipped in non-extended runs

### Lower Impact - Development/Debug
- **Test infrastructure validation**: Skipped in CI
- **Diagnostic logging**: Reduced in CI environments
- **Debug tooling**: Conditionally disabled

## 🎯 Specific Test Files Affected

### E2E Test Files (Skipped for Dev PRs)
```
test/Tests.E2E.NG/tests/auth/
test/Tests.E2E.NG/tests/people/
test/Tests.E2E.NG/tests/roles/
test/Tests.E2E.NG/tests/health/
test/Tests.E2E.NG/tests/api/
test/Tests.E2E.NG/tests/performance/
```

### Cross-Browser Tests (Skipped by Default)
All test files running in:
- Firefox browser context
- Safari/WebKit browser context

### Category-Filtered Tests
Tests filtered based on `@smoke`, `@critical`, `@extended` tags across all spec files.

## ⚡ Quick Reference - Skip Conditions

| Test Suite | Skip Condition | Impact Level | Files Affected |
|------------|----------------|--------------|----------------|
| Dev PR E2E | `base.ref != 'main'` | **CRITICAL** | All E2E specs |
| Feature Branch | `has_open_pr == true` | High | Unit + Integration |
| Cross-Browser | `CROSS_BROWSER != true` | High | All E2E specs |
| Extended | `TEST_CATEGORY != extended` | Medium | @extended tagged |
| Debug | `!DEBUG_DB` | Low | Debug utilities |
| CI Logging | `process.env.CI` | Low | Diagnostic logs |

## 📋 Recommendations Summary

1. **CRITICAL**: Enable E2E testing for dev branch PRs
2. **HIGH**: Document/justify cross-browser skipping policy  
3. **MEDIUM**: Review feature branch vs PR testing overlap
4. **LOW**: Add CI-friendly debugging options