# Architecture Conformance Issues

This document lists elements in the solution that do not conform to the established architecture guidelines documented in `docs/Architecture Guidelines.md` and related architecture documentation.

**Last Updated:** January 2025 - Document has been significantly updated to reflect current codebase state. Many previously identified issues have been resolved through package installations and infrastructure improvements.

## Domain Layer Violations

- [x] **~~Missing Ardalis.GuardClauses usage in domain entities~~** - ✅ **RESOLVED**: Ardalis.GuardClauses package is now installed in Domain project
- [ ] **DataAnnotations in Domain Layer** - Domain entities still use `System.ComponentModel.DataAnnotations` instead of GuardClauses for invariant enforcement
- [x] **~~Missing Ardalis.Specification usage~~** - ✅ **RESOLVED**: Ardalis.Specification package is now installed in Domain project (though no specifications are implemented yet)
- [ ] **Missing IAggregateRoot interface** - Domain entities don't implement `IAggregateRoot` as specified in the architecture guidelines
- [ ] **Missing BaseEntity pattern** - Entities don't inherit from a common `BaseEntity` class
- [ ] **Public setters in domain entities** - Properties have public setters instead of private setters with domain methods for state changes
- [ ] **GuardClauses not used in practice** - Despite package being installed, entities still use DataAnnotations instead of GuardClauses

## Application Layer Violations

- [ ] **Missing MediatR Commands/Queries implementation** - MediatR is registered in API but no commands, queries, or handlers are implemented; controllers directly call services
- [x] **~~Missing FluentValidation implementation~~** - ✅ **RESOLVED**: FluentValidation package is installed in App project (though no validators are implemented yet)
- [ ] **Missing AutoMapper profiles** - AutoMapper is registered but no mapping profiles are defined
- [x] **~~Missing Polly resilience policies~~** - ✅ **RESOLVED**: Polly package is now installed in App project (though no policies are implemented yet)
- [ ] **Application services violate SRP** - Services like `PersonService` handle multiple concerns (validation, orchestration, business logic)
- [ ] **FluentValidation not used in practice** - Despite package being installed, no validators are implemented
- [ ] **Polly not used in practice** - Despite package being installed, no resilience policies are implemented

## Infrastructure Layer Violations

- [x] **~~Missing Entity Framework Core~~** - ✅ **RESOLVED**: Entity Framework Core is now implemented with ApplicationDbContext, migrations, and multiple database providers (SQL Server, SQLite, In-Memory)
- [ ] **Missing Ardalis.Specification.EntityFrameworkCore** - EF Core specification integration not implemented (though base Specification package is installed)
- [ ] **Missing generic IRepository<T> pattern** - Custom repository interfaces instead of generic `IRepository<T>`
- [ ] **Missing Serilog + OpenTelemetry** - No structured logging or tracing implementation
- [ ] **Missing caching layer** - No Redis/LazyCache implementation as specified
- [ ] **Missing Scrutor decorators** - No decorator pattern implementation for cross-cutting concerns
- [x] **~~Missing Respawn usage~~** - ✅ **RESOLVED**: Respawn package is installed and used in integration tests

## Presentation Layer Violations

- [ ] **Controllers not using MediatR** - Controllers directly inject and call application services instead of using MediatR
- [ ] **Missing GraphQL endpoint** - No HotChocolate GraphQL implementation as specified
- [ ] **Exception handling in controllers** - Controllers handle domain exceptions instead of using global exception handling middleware

## Testing Layer Violations

- [x] **~~Missing Moq package~~** - ✅ **RESOLVED**: Moq package is installed and used in unit tests
- [x] **~~Missing Bogus package~~** - ✅ **RESOLVED**: AutoFixture is used instead of Bogus for test data generation
- [ ] **Missing Testcontainers** - No container-based integration testing setup (using SQLite for integration tests instead)
- [x] **~~Empty test projects~~** - ✅ **RESOLVED**: Comprehensive unit tests and integration tests are now implemented with good coverage
- [x] **~~Missing Respawn usage~~** - ✅ **RESOLVED**: Respawn is used in integration tests for database cleanup
- [x] **~~Missing FluentAssertions~~** - ✅ **RESOLVED**: FluentAssertions is installed and used in all test projects

## Cross-Cutting Concerns Violations

- [ ] **Missing authentication/authorization** - No OpenIddict or ASP.NET Identity implementation
- [ ] **Missing multi-tenancy** - No Finbuckle.MultiTenant implementation
- [ ] **Missing global exception handling** - No centralized error handling middleware
- [ ] **Missing request/response logging** - No structured logging of HTTP requests/responses

## CI/CD Violations

- [x] **~~Missing GitHub Actions workflows~~** - ✅ **RESOLVED**: GitHub Actions workflow `pr-validation.yml` is implemented with comprehensive CI/CD pipeline
- [x] **~~Missing Dependabot configuration~~** - ✅ **RESOLVED**: Dependabot configured for .NET, Angular, E2E tests, and GitHub Actions dependencies with weekly updates
- [x] **~~Missing CodeQL analysis~~** - ✅ **RESOLVED**: CodeQL analysis implemented for C# and JavaScript with automated scanning on push/PR and weekly scheduled runs
- [x] **~~Missing security scanning~~** - ✅ **RESOLVED**: DevSkim (SAST) integrated in PR validation, OWASP ZAP (DAST) comprehensive workflow with staging integration

## SOLID Principles Violations

- [ ] **Violation of Dependency Inversion Principle** - Domain entities depend on `System.ComponentModel.DataAnnotations` (framework concern)
- [ ] **Violation of Single Responsibility Principle** - Application services handle multiple concerns (validation, business logic, orchestration)
- [ ] **Missing abstractions** - Direct dependencies on concrete implementations in some areas

## Clean Architecture Violations

- [ ] **Dependency direction violation** - Domain layer depends on framework concerns (DataAnnotations)
- [ ] **Missing use case layer** - No clear separation between application orchestration and domain logic
- [ ] **Framework coupling** - Domain entities are coupled to ASP.NET Core validation framework

## Summary

**SIGNIFICANT PROGRESS MADE**: The solution has evolved considerably since the original assessment. Many foundational packages and infrastructure components have been implemented, including:

✅ **Resolved Issues:**
- Entity Framework Core with multiple database providers
- Comprehensive unit and integration testing with Moq, FluentAssertions, and AutoFixture
- GitHub Actions CI/CD pipeline
- Required packages installed (GuardClauses, Specification, FluentValidation, Polly, MediatR)

**Remaining Critical Issues:**
1. **Implementation Gap**: While packages are installed, many are not actually used (GuardClauses, FluentValidation, MediatR, Polly)
2. **Domain Layer Coupling**: Entities still use DataAnnotations instead of GuardClauses
3. **Missing CQRS Implementation**: MediatR is registered but no commands/queries are implemented
4. **Architectural Patterns**: Missing BaseEntity, IAggregateRoot, and proper domain methods

## Priority Recommendations

**High Priority (Implementation Gap - Packages Installed but Not Used):**
- Implement MediatR commands/queries pattern to replace direct service calls in controllers
- Replace DataAnnotations with GuardClauses in domain entities for proper invariant enforcement
- Implement FluentValidation validators for request validation
- Add proper domain entity patterns (BaseEntity, IAggregateRoot, private setters)

**Medium Priority:**
- Implement Ardalis.Specification patterns for query logic encapsulation
- Add Polly resilience policies for external service calls
- Implement structured logging with Serilog + OpenTelemetry
- Add global exception handling middleware
- Implement AutoMapper profiles for DTO mapping

**Low Priority:**
- Add GraphQL endpoints with HotChocolate
- Implement multi-tenancy with Finbuckle.MultiTenant
- ~~Add Dependabot and CodeQL to GitHub Actions~~ ✅ **COMPLETED**
- Implement caching layer (Redis/LazyCache)
- ~~Add security scanning (OWASP ZAP, DevSkim)~~ ✅ **COMPLETED**
