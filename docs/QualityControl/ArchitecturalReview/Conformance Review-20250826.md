# Conformance Review - 2025-08-26

Scope
- Compare current solution against project docs (docs/README.md and docs/05-Quality Control/Testing/1-Testing Strategy.md).
- Note what conforms, what is missing, and concrete actions.

Summary
- Clean Architecture structure: Present and consistent (Domain, App, Infrastructure, Api, Angular, Maui).
- Core packages in use: EF Core, Ardalis.GuardClauses, Ardalis.Specification, MediatR, AutoMapper, FluentValidation, Polly, Swagger, xUnit, Playwright.
- Major gaps vs. docs/strategy: Minimal API project missing, multi-tenancy not implemented, observability and security not configured, caching and GraphQL absent, integration testing stack (Testcontainers/Respawn usage) incomplete, MediatR registration scope likely incorrect.

Architecture & Layering
- Conforms
  - Layered projects exist (Domain/App/Infrastructure/Api/Angular/Maui).
  - Infrastructure exposes EF Core DbContext, migrations, and repository impls per entity (EfPerson/Role/Wall/Window).
  - App contains service interfaces and implementations; DI extension AddApplication present.
  - Api hosts controllers and swagger, wires DI.
- Deviations / Risks
  - docs/README lists Api.Min (Minimal API). Not present in solution.
  - Application relies on services, but MediatR is registered (in Api) without any handlers. If CQRS via MediatR is intended, handlers should be added and assemblies registered.

Packages vs. Agreed Stack
- Present
  - EF Core (SqlServer/Sqlite), Migrations.
  - Ardalis.GuardClauses, Ardalis.Specification (Domain).
  - MediatR, AutoMapper, FluentValidation, Polly (App/Api).
  - Swagger (Swashbuckle) in Api.
  - Serilog.Extensions.Logging referenced (Infrastructure) but not configured in host.
  - Playwright included for E2E.NG; xUnit across tests.
- Missing (from prior plan/docs)
  - Multi-tenancy: Finbuckle.MultiTenant not present; no schema-per-tenant wiring.
  - Caching: no MemoryCache/DistributedCache or Redis client usage.
  - Observability: no OpenTelemetry, metrics, or tracing wiring.
  - Security/Identity: no OpenIddict/JWT/ASP.NET Identity; Api calls UseAuthorization without UseAuthentication.
  - API surface: no GraphQL (HotChocolate) or OData.
  - Messaging/Background: no Quartz.NET/Hangfire/MassTransit.
  - CI/CD & AI: no .github/workflows, CodeQL, Dependabot, or AI PR reviewer config.

API Layer
- Found controllers for People/Roles/Walls/Windows with DTOs, swagger enabled.
- JSON options: camelCase, enum string, custom UTC DateTime converters (good).
- CORS policy for Angular (localhost) present.
- UseAuthorization is enabled without auth schemes (no-op). Consider removing until auth is added or add authentication configuration.
- MediatR registration likely wrong: builder.Services.AddMediatR(... typeof(Program).Assembly) registers only Api assembly. If handlers live in App, add App assembly to scanning.

Domain Layer
- Entities present: Person, Role, Wall, Window.
- Guard clauses package included; minimal invariant usage observed (Person has DataAnnotations and RowVersion). Consider adding Guard clauses in constructors for invariants.
- Specification package present; no concrete specifications found in Domain yet. If using Specification pattern, add them (e.g., PersonByNameSpec, PeopleWithRolesSpec).

Application Layer
- Service interfaces and service implementations exist for Person/Role/Wall/Window, registered via AddApplication.
- AutoMapper and FluentValidation packages are included; no mapping profiles/validators located. If intended, add profiles/validators and wire Validator integration (e.g., FluentValidation ASP.NET or MediatR pipeline behavior).
- Polly present; no policy registration/usage yet. Add resilience where appropriate (DB calls, external IO).

Infrastructure Layer
- EF Core DbContext with DbSets for all entities.
- Migrations present (InitialCreate) and EnsureCreatedAsync is called at startup (SQLite/SQLServer) — OK for dev/test; prefer MigrateAsync for production.
- Repositories exist per entity; consistent with App abstractions.
- Serilog.Extensions.Logging referenced but no Serilog host config; logging currently defaults to Microsoft.Extensions.Logging. Add Serilog host configuration if structured logging is required.

Testing Strategy Alignment
- Unit tests: xUnit, Moq, FluentAssertions, AutoFixture present (Tests.Unit.Backend) — aligns with docs.
- Integration tests: project exists with Microsoft.AspNetCore.Mvc.Testing and EF Sqlite; no Testcontainers package, and Respawn is not referenced here (Respawn is referenced in Infrastructure). Consider moving Respawn to tests and adding Testcontainers for DB realism.
- E2E:
  - Angular E2E uses Playwright (good).
  - MAUI E2E/Integration projects exist but lack a UI automation stack (e.g., .NET MAUI UITest/Appium) — currently placeholders.
- Gaps vs. Testing plan
  - Testcontainers not set up for DB-backed integration tests.
  - No WireMock.Net for external API mocking (if needed later).
  - CI not executing tests (no workflows committed).

Multi-Tenancy
- Program.cs contains E2E-focused env var handling (worker-specific SQLite DB names, tenant prefix logging), but no tenant resolution, no schema-per-tenant configuration, no global query filters.
- Finbuckle.MultiTenant not integrated; no schema strategy implemented.

Observability & Logging
- No OpenTelemetry or metrics exporters configured.
- Serilog sink/host configuration not set; consider replacing default logging with Serilog and adding sinks (console, Seq, file) as needed.

Security
- No authentication configured; UseAuthorization without schemes is a no-op.
- No JWT/OpenIddict/Identity setup; endpoints are public.

CI/CD & AI PR Review
- No GitHub Actions workflows, Dependabot, or CodeQL found in repo.
- No AI PR review configuration.

Actionable Backlog
1) Minimal API parity
- Add src/Api.Min project (as per docs) with at least one endpoint mirroring Api.

2) MediatR registration and usage
- Register MediatR with App assembly: AddServicesFromAssemblyContaining<App.DependencyInjection>() or explicit assembly list.
- If CQRS is desired, add command/query handlers in App and refactor controllers to IMediator.

3) Validation & Mapping
- Add FluentValidation validators for request DTOs (Api) or commands (App) and wire FluentValidation.
- Add AutoMapper profiles in App and use mapper in services/controllers.

4) Multi-tenancy (schema-per-tenant)
- Introduce Finbuckle.MultiTenant.
- Implement ITenantInfo store and resolution (subdomain/header/claim).
- Configure EF model per tenant to set schema; add global filters or schema switch in DbContext.

5) Caching
- Add IMemoryCache for simple caching; plan for Redis (StackExchange.Redis) for distributed cache when needed.

6) Observability & Logging
- Configure Serilog as host logger; add sinks.
- Add OpenTelemetry for tracing/metrics; export to OTLP/Jaeger/Prometheus as appropriate.

7) Security
- Add authentication (JWT bearer) and authorization policies.
- Consider OpenIddict for OIDC provider if needed.

8) Integration testing hardening
- Add Testcontainers for DBs; use Respawn in tests to reset DB between tests.
- Add WireMock.Net for external API stubs (if/when needed).

9) E2E MAUI
- Add a UI automation strategy for .NET MAUI (e.g., Appium or .NET MAUI UITest) and basic smoke test.

10) CI/CD & AI
- Add GitHub Actions workflows: build, unit/integration/e2e test, artifact, deploy.
- Add Dependabot and CodeQL.
- Add AI PR reviewer action (e.g., CodeRabbit or Copilot for PRs).

11) API Surface
- Optional: Add GraphQL (HotChocolate) or OData endpoints per earlier plan.

12) Migrations
- Replace EnsureCreatedAsync with MigrateAsync for production paths.

Notable Positives
- Clear separation of concerns across projects.
- Good early investment in testing projects and Playwright for Angular E2E.
- DTOs and JSON settings are consistent (camelCase, enum strings, UTC handling).
- SQLite-based test isolation and worker DB naming logic support parallel E2E.

Appendix: Items observed in code
- Program.cs: CORS for Angular, Swagger in Dev, custom DateTime UTC converters, AddAutoMapper scans App & Infrastructure assemblies; MediatR currently only scanning Api assembly.
- Infrastructure: EF Core SqlServer/Sqlite; repositories and migrations in place; EnsureDatabaseAsync calls EnsureCreated.
- Tests: Unit/Integration/E2E projects scaffolded; only E2E.NG uses Playwright; others appear minimal.
