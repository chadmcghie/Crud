# Architecture Conformance Issues

This document lists elements in the solution that do not conform to the established architecture guidelines documented in `docs/Architecture Guidelines.md` and related architecture documentation.

## Domain Layer Violations

- [ ] **Missing Ardalis.GuardClauses usage in domain entities** - Domain entities (`Person.cs`, `Role.cs`, `Wall.cs`, `Window.cs`) use `System.ComponentModel.DataAnnotations` instead of GuardClauses for invariant enforcement
- [ ] **DataAnnotations in Domain Layer** - Domain entities should not depend on framework-specific validation attributes; business rules should be enforced through GuardClauses and domain methods
- [ ] **Missing Ardalis.Specification usage** - No specifications are implemented for encapsulating query logic (empty `Specifications` folder in Domain project)
- [ ] **Missing IAggregateRoot interface** - Domain entities don't implement `IAggregateRoot` as specified in the architecture guidelines
- [ ] **Missing BaseEntity pattern** - Entities don't inherit from a common `BaseEntity` class
- [ ] **Public setters in domain entities** - Properties have public setters instead of private setters with domain methods for state changes

## Application Layer Violations

- [ ] **Missing MediatR Commands/Queries implementation** - MediatR is registered but no commands, queries, or handlers are implemented; controllers directly call services
- [ ] **Missing FluentValidation implementation** - FluentValidation package is referenced but no validators are implemented
- [ ] **Missing AutoMapper profiles** - AutoMapper is registered but no mapping profiles are defined
- [ ] **Missing Polly resilience policies** - Polly package is referenced but no retry/circuit breaker policies are implemented
- [ ] **Application services violate SRP** - Services like `PersonService` handle multiple concerns (validation, orchestration, business logic)

## Infrastructure Layer Violations

- [ ] **Missing Entity Framework Core** - Architecture specifies EF Core but only in-memory repositories are implemented
- [ ] **Missing Ardalis.Specification.EntityFrameworkCore** - EF Core specification integration not implemented
- [ ] **Missing generic IRepository<T> pattern** - Custom repository interfaces instead of generic `IRepository<T>`
- [ ] **Missing Serilog + OpenTelemetry** - No structured logging or tracing implementation
- [ ] **Missing caching layer** - No Redis/LazyCache implementation as specified
- [ ] **Missing Scrutor decorators** - No decorator pattern implementation for cross-cutting concerns

## Presentation Layer Violations

- [ ] **Controllers not using MediatR** - Controllers directly inject and call application services instead of using MediatR
- [ ] **Missing GraphQL endpoint** - No HotChocolate GraphQL implementation as specified
- [ ] **Exception handling in controllers** - Controllers handle domain exceptions instead of using global exception handling middleware

## Testing Layer Violations

- [ ] **Missing Moq package** - Unit test projects don't reference Moq for mocking
- [ ] **Missing Bogus package** - No test data generation library referenced
- [ ] **Missing Testcontainers** - No container-based integration testing setup
- [ ] **Empty test projects** - Unit and integration test projects contain no actual test implementations
- [ ] **Missing Respawn usage** - Respawn is referenced in Infrastructure but not used in tests

## Cross-Cutting Concerns Violations

- [ ] **Missing authentication/authorization** - No OpenIddict or ASP.NET Identity implementation
- [ ] **Missing multi-tenancy** - No Finbuckle.MultiTenant implementation
- [ ] **Missing global exception handling** - No centralized error handling middleware
- [ ] **Missing request/response logging** - No structured logging of HTTP requests/responses

## CI/CD Violations

- [ ] **Missing GitHub Actions workflows** - No CI/CD pipeline configuration
- [ ] **Missing Dependabot configuration** - No automated dependency updates
- [ ] **Missing CodeQL analysis** - No static code analysis setup
- [ ] **Missing security scanning** - No OWASP ZAP or DevSkim integration

## SOLID Principles Violations

- [ ] **Violation of Dependency Inversion Principle** - Domain entities depend on `System.ComponentModel.DataAnnotations` (framework concern)
- [ ] **Violation of Single Responsibility Principle** - Application services handle multiple concerns (validation, business logic, orchestration)
- [ ] **Missing abstractions** - Direct dependencies on concrete implementations in some areas

## Clean Architecture Violations

- [ ] **Dependency direction violation** - Domain layer depends on framework concerns (DataAnnotations)
- [ ] **Missing use case layer** - No clear separation between application orchestration and domain logic
- [ ] **Framework coupling** - Domain entities are coupled to ASP.NET Core validation framework

## Summary

The solution shows a good foundation with proper layer separation and dependency injection setup, but it currently implements a simplified CRUD pattern rather than the full Clean Architecture + DDD approach specified in the guidelines. The most critical issues are:

1. Missing MediatR implementation for CQRS pattern
2. Lack of GuardClauses in domain entities
3. Absence of proper specification patterns for queries
4. Framework coupling in the domain layer
5. Missing comprehensive testing infrastructure

## Priority Recommendations

**High Priority:**
- Implement MediatR commands/queries pattern
- Replace DataAnnotations with GuardClauses in domain entities
- Implement Ardalis.Specification for query logic
- Add proper domain entity patterns (BaseEntity, IAggregateRoot)

**Medium Priority:**
- Implement FluentValidation for request validation
- Add Entity Framework Core with proper repository pattern
- Implement structured logging with Serilog
- Add comprehensive unit and integration tests

**Low Priority:**
- Add GraphQL endpoints
- Implement multi-tenancy
- Add CI/CD pipelines
- Implement caching and resilience patterns
