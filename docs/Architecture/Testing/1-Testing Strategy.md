# Test Plan for CrudApp

## Table of Contents
1. [Objectives](#objectives)  
2. [Test Project Breakdown](#test-project-breakdown)  
   - [Tests.Unit](#1-testsunit)  
   - [Tests.Integration](#2-testsintegration)  
   - [Tests.E2E](#3-testse2e)  
3. [Test Pyramid Strategy](#test-pyramid-strategy)  
4. [Automation & CI/CD](#automation--cicd)  
5. [Next Steps](#next-steps)  

---

## Objectives
- Ensure **business logic correctness** (Domain & Application layers).  
- Validate **infrastructure integration** (databases, external services).  
- Guarantee **API contract compliance** (endpoints behave as expected).  
- Confirm **UI workflows** (Angular & MAUI apps) behave as intended.  
- Provide a **confidence ladder**: fast unit tests → slower integration → few full E2E.  

[Back to Top ^](#test-plan-for-crudapp)

---

## Test Project Breakdown

### 1. Tests.Unit
**Scope**  
- Domain entities (aggregates, value objects, enums).  
- Application use cases / services.  
- Utility code in Shared.  

**Frameworks**  
- xUnit (modern, async-friendly).  
- FluentAssertions (readable assertions).  
- Moq/NSubstitute (mocks, stubs).  

**Examples**  
- `User.Create("badEmail")` → throws validation error.  
- `InspectionService.CalculateScore()` → correct score returned.  
- Shared date/time helper returns consistent UTC.  

[Back to Top ^](#test-plan-for-crudapp)

---

### 2. Tests.Integration
**Scope**  
- API endpoints (via in-memory TestServer / WebApplicationFactory).  
- Database access (EF Core, repositories).  
- Infrastructure (file storage, messaging, caching).  

**Frameworks & Tools**  
- xUnit + Microsoft.AspNetCore.Mvc.Testing.  
- Testcontainers for .NET (real DBs in Docker).  
- FluentAssertions.  

**Examples**  
- `POST /api/users` persists user in DB, returns `201`.  
- `GET /api/inspections/1` returns correct inspection with related entities.  
- Authentication middleware blocks requests without JWT.  

[Back to Top ^](#test-plan-for-crudapp)

---

### 3. Tests.E2E
**Scope**  
- Full-stack flows: Angular → API → DB.  
- MAUI app workflows (UI automation).  
- Critical business scenarios.  

**Frameworks & Tools**  
- Playwright (TypeScript) - Primary E2E framework.  
- Serial execution strategy for reliability.  
- SQLite with file-based cleanup.  

**Execution Strategy (Updated 2025-08-28)**  
- **Serial execution only** - No parallel workers due to SQLite/EF Core limitations.  
- **Categorized tests**: @smoke (2 min), @critical (5 min), @extended (10 min).  
- **Single browser default** - Cross-browser only for critical paths.  
- **Shared servers** - Start once, use for all tests.  

**Examples**  
- User logs in (Angular UI) → token retrieved → secured API call succeeds.  
- Submit inspection in UI → appears in database → accessible via API.  
- Regression test: CRUD flows continue to work after refactor.  

[Back to Top ^](#test-plan-for-crudapp)

---

## Test Pyramid Strategy
