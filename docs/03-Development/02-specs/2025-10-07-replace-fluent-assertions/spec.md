# Spec Requirements Document

> Spec: Replace FluentAssertions with xUnit Assert
> Created: 2025-10-07
> GitHub Issue: #295 - Replace FluentAssertions with xUnit Assert

## Overview

Replace the FluentAssertions library with xUnit's built-in Assert class across all test projects to reduce external dependencies and standardize on xUnit's native assertion patterns. This change will simplify the testing infrastructure, reduce package maintenance overhead, and align with standard xUnit testing practices used throughout the .NET community.

## User Stories

### Streamlined Test Infrastructure

As a developer maintaining the test suite, I want to use xUnit's built-in assertions instead of FluentAssertions, so that we have fewer external dependencies and a simpler testing infrastructure.

When writing or maintaining tests, developers will use standard xUnit Assert methods (Assert.Equal, Assert.NotNull, Assert.True, etc.) instead of FluentAssertions' fluent syntax (.Should().Be(), .Should().NotBeNull(), etc.). This reduces the cognitive load of learning an additional assertion library and ensures consistency with standard xUnit documentation and community examples.

### Reduced Dependency Maintenance

As a project maintainer, I want to minimize third-party dependencies in the test projects, so that there are fewer packages to update and fewer potential security or compatibility issues.

By removing FluentAssertions as a dependency, the project will have one less NuGet package to monitor for updates, security vulnerabilities, and breaking changes. This simplifies dependency management and reduces the attack surface of the project.

### Consistent Testing Patterns

As a new developer joining the project, I want to use familiar xUnit patterns, so that I can write tests immediately without learning additional assertion libraries.

New developers familiar with xUnit will be able to write tests using standard Assert methods without needing to learn FluentAssertions' fluent API. This reduces onboarding time and aligns with the majority of xUnit documentation and examples available online.

## Spec Scope

1. **Unit Test Migration** - Convert all FluentAssertions assertions to xUnit Assert in the Tests.Unit.Backend project (~32 test files)
2. **Integration Test Migration** - Convert all FluentAssertions assertions to xUnit Assert in the Tests.Integration.Backend project (~57 test files)
3. **Project Configuration Updates** - Remove FluentAssertions package references and global usings from both test projects
4. **Test Validation** - Ensure all tests pass after migration and maintain equivalent assertion logic
5. **Documentation Updates** - Update tech-stack.md to reflect the removal of FluentAssertions

## Out of Scope

- Changing test logic or adding new test cases (purely a refactoring exercise)
- Modifying any production code in src/ directories
- Changes to E2E tests (Tests.E2E.NG uses Playwright's expect assertions)
- Changes to Angular tests (Tests.Integration.NG uses Jasmine/Karma)

## Expected Deliverable

1. All unit and integration tests use xUnit Assert methods instead of FluentAssertions and pass successfully
2. FluentAssertions package removed from both test project .csproj files and no longer appears in global usings
3. Updated technical documentation reflecting the removal of FluentAssertions from the testing stack
