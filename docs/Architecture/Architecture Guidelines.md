# Clean Architecture Strawman (with Packages)

This document outlines a suggested architecture and package stack for building
a generic CRUD platform in .NET, following Clean Architecture + DDD principles.

---

## Layers

### Domain
- **Entities/Aggregates**: Customer, Order, etc.
- **Ardalis.GuardClauses** → Enforce invariants.
- **Ardalis.Specification** → Encapsulate query logic.
- **Interfaces**: `IRepository<T>`, domain services.

### Application
- **MediatR** → Commands, queries, notifications.
- **FluentValidation** (or DataAnnotations/Valit) → Request validation.
- **AutoMapper / Mapster** → DTO ↔ Entity mapping.
- **Polly** → Retry & resilience policies.

### Infrastructure
- **Entity Framework Core** (+ `Ardalis.Specification.EntityFrameworkCore`) → DB persistence.
- **Dapper** → Read-heavy scenarios.
- **StackExchange.Redis / LazyCache** → Caching.
- **Serilog + OpenTelemetry** → Logging & tracing.
- **Quartz.NET** → Background jobs & scheduling.
- **Scrutor** → Decorators (logging, caching, auditing).

### Presentation
- **ASP.NET Core Web API / Minimal APIs** → Endpoints.
- **Swagger/Swashbuckle** → API docs.
- **HotChocolate GraphQL** → Schema-first endpoint.
- **Playwright** → E2E test automation.

### Cross-Cutting
- **Finbuckle.MultiTenant** → Multi-tenant strategy.
- **OpenIddict + ASP.NET Identity** → AuthN/AuthZ.
- **OWASP ZAP + DevSkim** → Penetration/security testing.
- **xUnit + Moq + Bogus + Testcontainers** → Unit & integration testing.
- **Respawn** → DB resets between tests.

---

## CI/CD
- **GitHub Actions** → Build, test, deploy.
- **Dependabot** → Dependency updates.
- **CodeQL** → Static analysis.
- **AI PR Review** (Copilot for PRs, CodeRabbit, CodiumAI) → Automated review.

---

## Mermaid Diagram

```mermaid
flowchart TD

  subgraph Presentation
    A[ASP.NET Core API]
    B[Swagger / GraphQL]
  end

  subgraph Application
    C[MediatR]
    D[FluentValidation]
    E[AutoMapper / Mapster]
    F[Polly]
  end

  subgraph Domain
    G[Entities + Aggregates]
    H[Ardalis.GuardClauses]
    I[Ardalis.Specification]
    J[IRepository<T>]
  end

  subgraph Infrastructure
    K[EF Core + Specification.EF]
    L[Dapper]
    M[Redis / LazyCache]
    N[Serilog + OpenTelemetry]
    O[Quartz.NET]
    P[Scrutor Decorators]
  end

  subgraph CrossCutting
    Q[Finbuckle.MultiTenant]
    R[OpenIddict / ASP.NET Identity]
    S[Testing Stack (xUnit, Moq, Bogus, Playwright)]
    T[Security (ZAP, DevSkim)]
  end

  subgraph CI_CD
    U[GitHub Actions]
    V[Dependabot]
    W[CodeQL]
    X[AI PR Review]
  end

  %% Dependencies / IoC relationships
  A --> C
  B --> C
  C --> D
  C --> E
  C --> J
  D --> G
  E --> G
  J --> I
  J --> K
  J --> L
  G --> H
  K --> M
  K --> N
  P --> J
  Q --> J
  R --> A
  S --> A
  S --> K
  T --> A
  U --> S
  U --> T
  V --> U
  W --> U
  X --> U
