# Spec Tasks

> Parent Issue: #295
> Related Issue: #209 (original request)

## Tasks

- [x] 1. Migrate Unit Tests to xUnit Assert (Issue: #296)
  - [x] 1.1 Analyze FluentAssertions usage patterns in unit tests
  - [x] 1.2 Convert Domain layer unit tests (PersonTests, RoleTests, WallTests, WindowTests, UserTests, RefreshTokenTests, PasswordResetTokenTests, ValueObjects)
  - [x] 1.3 Convert Application layer unit tests (Authentication handlers, CachingBehavior, MediatR tests)
  - [x] 1.4 Convert Infrastructure layer unit tests (Repository, Services, Validators, Polly resilience)
  - [x] 1.5 Run unit test suite and verify all tests pass
  - [x] 1.6 Review converted tests for assertion correctness and readability

- [x] 2. Migrate Integration Tests to xUnit Assert (Issue: #297)
  - [x] 2.1 Analyze FluentAssertions usage patterns in integration tests
  - [x] 2.2 Convert Controller integration tests (People, Roles, Walls, Windows, Auth, Database, Admin/Cache)
  - [x] 2.3 Convert E2E integration tests (Authentication E2E, Caching E2E)
  - [x] 2.4 Convert Configuration and validation tests (DI, Environment, Health checks, Contract tests)
  - [x] 2.5 Convert Smoke tests and infrastructure tests
  - [x] 2.6 Convert Output caching and compression tests
  - [x] 2.7 Build integration test project and verify no compilation errors
  - [x] 2.8 Review converted tests for assertion correctness and readability

- [x] 3. Update Project Configuration (Issue: #298)
  - [x] 3.1 Remove FluentAssertions package reference from Tests.Unit.Backend.csproj
  - [x] 3.2 Remove FluentAssertions global using from Tests.Unit.Backend.csproj
  - [x] 3.3 Remove FluentAssertions package reference from Tests.Integration.Backend.csproj
  - [x] 3.4 Remove FluentAssertions global using from Tests.Integration.Backend.csproj
  - [x] 3.5 Build both test projects and verify no compilation errors
  - [x] 3.6 Run full test suite (unit + integration) and verify all tests pass

- [x] 4. Update Documentation and Finalize (Issue: #299)
  - [x] 4.1 Update tech-stack.md to remove FluentAssertions reference
  - [x] 4.2 Run dotnet format on both test projects for code consistency
  - [x] 4.3 Run full test suite one final time as regression check
  - [x] 4.4 Update this tasks.md file marking all tasks complete
  - [x] 4.5 Close parent issue #295 and related issue #209
