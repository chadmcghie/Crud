---
description: Test Creation Rules for Agent OS
globs:
alwaysApply: false
version: 1.0
encoding: UTF-8
---

# Test Creation Rules

## Overview

Generate comprehensive test suites following the Test Pyramid strategy: Unit → Integration → E2E.

<pre_flight_check>
  EXECUTE: @.agents/.agent-os/instructions/meta/pre-flight.md
</pre_flight_check>

<process_flow>

<step number="1" name="test_strategy_analysis">

### Step 1: Test Strategy Analysis

Analyze the feature/spec to determine appropriate test coverage across all layers.

<test_pyramid>
  <unit_tests>
    - Domain entities and value objects
    - Application services and use cases
    - Utility functions and helpers
    - Fast execution, isolated, no external dependencies
  </unit_tests>
  <integration_tests>
    - API endpoints with database
    - Repository implementations
    - External service integrations
    - Medium execution time, real infrastructure
  </integration_tests>
  <e2e_tests>
    - Full user workflows
    - Critical business scenarios
    - Cross-system integration
    - Slow execution, full stack
  </e2e_tests>
</test_pyramid>

</step>

<step number="2" name="unit_test_creation">

### Step 2: Unit Test Creation

Create unit tests for the backend (.NET) and frontend (Angular) components.

<backend_unit_tests>
  <framework>xUnit + FluentAssertions + Moq</framework>
  <coverage>
    - Domain entities: validation, business rules
    - Application services: use case logic
    - Value objects: equality, validation
    - Utility classes: pure functions
  </coverage>
  <patterns>
    - AAA pattern (Arrange, Act, Assert)
    - One assertion per test
    - Meaningful test names
    - Mock external dependencies
  </patterns>
</backend_unit_tests>

<frontend_unit_tests>
  <framework>Jasmine + Karma + Angular Testing Utilities</framework>
  <coverage>
    - Component logic and lifecycle
    - Service methods and HTTP calls
    - Pipe transformations
    - Form validation
  </coverage>
  <patterns>
    - TestBed for Angular components
    - Spy on dependencies
    - Test both success and error scenarios
    - Mock HTTP responses
  </patterns>
</frontend_unit_tests>

</step>

<step number="3" name="integration_test_creation">

### Step 3: Integration Test Creation

Create integration tests for API endpoints and database operations.

<api_integration_tests>
  <framework>xUnit + Microsoft.AspNetCore.Mvc.Testing + Testcontainers</framework>
  <coverage>
    - HTTP endpoints with real database
    - Authentication and authorization
    - Data persistence and retrieval
    - Error handling and validation
  </coverage>
  <setup>
    - WebApplicationFactory for test server
    - Testcontainers for database isolation
    - Seed test data for consistent scenarios
    - Clean up between tests
  </setup>
</api_integration_tests>

</step>

<step number="4" name="e2e_test_creation">

### Step 4: E2E Test Creation

Create end-to-end tests for critical user workflows.

<e2e_tests>
  <framework>Playwright for .NET</framework>
  <coverage>
    - Complete user journeys
    - Cross-browser compatibility
    - UI interactions and validations
    - Data flow from UI to database
  </coverage>
  <best_practices>
    - Page Object Model pattern
    - Stable selectors (data-testid)
    - Wait for elements and network requests
    - Test data isolation
  </best_practices>
</e2e_tests>

</step>

<step number="5" name="test_data_management">

### Step 5: Test Data Management

Create test data factories and helpers for consistent test scenarios.

<test_data_strategy>
  <unit_tests>
    - Object Mother pattern for complex objects
    - Builder pattern for flexible construction
    - Bogus library for fake data generation
  </unit_tests>
  <integration_tests>
    - Database seeding with known data
    - Respawn for database cleanup
    - Isolated test databases per test run
  </integration_tests>
  <e2e_tests>
    - API helpers for data setup
    - Consistent test user accounts
    - Cleanup after test completion
  </e2e_tests>
</test_data_strategy>

</step>

<step number="6" name="ci_cd_integration">

### Step 6: CI/CD Integration

Ensure tests are integrated into the build pipeline.

<pipeline_configuration>
  <github_actions>
    - Run unit tests on every PR
    - Run integration tests on main branch
    - Run E2E tests on release candidates
    - Fail fast on test failures
  </github_actions>
  <test_reporting>
    - Code coverage reports
    - Test result summaries
    - Performance metrics
    - Flaky test detection
  </test_reporting>
</pipeline_configuration>

</step>

</process_flow>

<test_file_structure>
  test/
  ├── Tests.Unit.Backend/
  │   ├── Domain/
  │   ├── Application/
  │   └── Shared/
  ├── Tests.Integration.Backend/
  │   ├── Controllers/
  │   ├── Repositories/
  │   └── Infrastructure/
  ├── Tests.E2E.NG/
  │   ├── tests/
  │   │   ├── api/
  │   │   ├── angular-ui/
  │   │   └── integration/
  │   └── helpers/
  └── Tests.Unit.Frontend/ (Angular)
      ├── components/
      ├── services/
      └── pipes/
</test_file_structure>

<post_flight_check>
  EXECUTE: @.agents/.agent-os/instructions/meta/post-flight.md
</post_flight_check>
