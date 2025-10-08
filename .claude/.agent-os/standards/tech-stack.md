# Tech Stack

## Context

Global tech stack defaults for Agent OS projects, overridable in project-specific `.agents/.agent-os/product/tech-stack.md`.

## Backend (.NET)
- App Framework: ASP.NET Core 8.0+
- Language: C# 12+ (.NET 8)
- Architecture: Clean Architecture + DDD
- Primary Database: In-Memory (development), SQL Server/PostgreSQL (production)
- ORM: Entity Framework Core
- API Style: RESTful Web API + Minimal APIs
- Validation: FluentValidation + Data Annotations
- Mapping: AutoMapper/Mapster
- Dependency Injection: Built-in ASP.NET Core DI
- Testing: xUnit + FluentAssertions + Moq + Testcontainers

## Frontend (Angular)
- JavaScript Framework: Angular 20+
- Language: TypeScript 5.9+
- Build Tool: Angular CLI + Vite
- Package Manager: npm
- Node Version: 22 LTS
- CSS Framework: TailwindCSS (latest)
- UI Components: Angular Material (optional)
- State Management: RxJS + Angular Services
- HTTP Client: Angular HttpClient
- Testing: Jasmine + Karma + Playwright

## Development & Deployment
- Font Provider: Google Fonts
- Font Loading: Self-hosted for performance
- Icons: Angular Material Icons / Lucide
- Application Hosting: IIS / Docker / Cloud platforms
- Database Hosting: SQL Server / PostgreSQL
- Database Backups: Automated
- Asset Storage: Local / Cloud storage
- CI/CD Platform: GitHub Actions
- CI/CD Trigger: Push to main/staging branches
- Tests: Unit → Integration → E2E pipeline
- Production Environment: main branch
- Staging Environment: staging branch

## Architecture Layers
- Domain: Entities, Value Objects, Domain Services
- Application: Use Cases, DTOs, Interfaces
- Infrastructure: Repositories, External Services, EF Core
- Presentation: Controllers, Minimal APIs, Angular Components
