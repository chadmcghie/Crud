# Technical Stack

## Core Framework
- **Application Framework:** ASP.NET Core Web API 8.0
- **Language:** C# 12 with nullable reference types
- **Architecture:** Clean Architecture with CQRS

## Frontend
- **JavaScript Framework:** Angular 20.2.0
- **Import Strategy:** node
- **CSS Framework:** Custom CSS with Angular Material components
- **UI Component Library:** Angular Material (planned)
- **Fonts Provider:** System fonts
- **Icon Library:** Material Icons (planned)

## Data Layer
- **Database System:** SQLite (development) / SQL Server (production)
- **ORM:** Entity Framework Core 8.x
- **Migrations:** EF Core Migrations

## Infrastructure
- **Application Hosting:** IIS / Azure App Service / Docker
- **Database Hosting:** Local SQLite / Azure SQL / SQL Server
- **Asset Hosting:** Static file middleware / CDN
- **Deployment Solution:** GitHub Actions CI/CD

## Testing
- **Unit Testing:** xUnit with FluentAssertions
- **Integration Testing:** WebApplicationFactory with TestContainers
- **E2E Testing:** Playwright
- **Code Coverage:** Coverlet

## Development Tools
- **Code Repository URL:** https://github.com/chadmcghie/crud
- **Package Manager:** NuGet (.NET) / npm (Angular)
- **Build Tools:** MSBuild / Angular CLI
- **Code Quality:** ESLint, Prettier, .editorconfig

## Patterns & Libraries
- **Mediator:** MediatR 13.0.0
- **Mapping:** AutoMapper 12.0.1
- **Validation:** FluentValidation 12.0.0
- **Resilience:** Polly 8.6.3
- **Logging:** Serilog
- **Guard Clauses:** Ardalis.GuardClauses
- **API Documentation:** Swagger/OpenAPI