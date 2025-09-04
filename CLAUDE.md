# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture

This is a multi-platform CRUD application using Clean Architecture with:
- **Backend**: .NET 8 API with Entity Framework Core, MediatR (CQRS), SQLite database
- **Frontend**: Angular 20 with TypeScript, RxJS
- **Layers**: Domain → App → Infrastructure → Api → UI (Angular/MAUI)
- **Ports**: API runs on 5172 (HTTP) and 7268 (HTTPS), Angular on 4200

## Key Commands

### Development
```bash
# Start both API and Angular (use PowerShell)
./LaunchApps.ps1

# Kill running servers before builds
./kill-servers.ps1

# Start API only
dotnet run --project src/Api/Api.csproj --launch-profile http

# Start Angular only
cd src/Angular && npm start
```

### Build
```bash
# Build entire solution
dotnet build Crud.sln

# Build backend only
dotnet build Crud.Backend.sln

# Build Angular
cd src/Angular && npm run build
```

### Testing
```bash
# Backend unit tests
dotnet test test/Tests.Unit.Backend/Tests.Unit.Backend.csproj

# Backend integration tests
dotnet test test/Tests.Integration.Backend/Tests.Integration.Backend.csproj

# E2E tests (using Playwright webServer - see ADR-002)
cd test/Tests.E2E.NG
npm run test:webserver   # Recommended: Playwright manages servers
npm run test:smoke       # 2-minute smoke tests only
npm run test:critical    # 5-minute critical tests
npm run test:headed      # Run with visible browser

# Legacy E2E test commands (being phased out)
npm run test:serial      # Old approach with custom server management
npm run test:fast        # Quick tests, assumes servers running

# Angular tests
cd src/Angular && npm test
```

### Linting
```bash
# Angular linting
cd src/Angular && npm run lint
```

## E2E Testing Strategy

**UPDATE**: E2E tests now use Playwright's built-in webServer configuration. See `docs/Decisions/0002-E2E-Testing-Database-Strategy.md`.

- **Playwright webServer**: Automatic server management, unique database per test run
- Tests are tagged: `@smoke` (2 min), `@critical` (5 min), `@extended` (10 min)
- Database isolation via unique filenames prevents locking issues
- CI/CD uses same configuration as local development

## Project Structure

- `src/Domain/` - Business entities and logic (no dependencies)
- `src/App/` - Application services, CQRS handlers, DTOs
- `src/Infrastructure/` - EF Core, repositories, external services
- `src/Api/` - ASP.NET Core Web API controllers
- `src/Angular/` - Angular frontend application
- `test/Tests.Unit.Backend/` - xUnit, Moq, FluentAssertions
- `test/Tests.E2E.NG/` - Playwright E2E tests

## Database

- Development: SQLite with file-based storage
- Connection string in appsettings: `Data Source=crud.db`
- Migrations: `dotnet ef migrations add <name> -p src/Infrastructure -s src/Api`
- Update database: `dotnet ef database update -p src/Infrastructure -s src/Api`

## Clean Architecture Rules

1. **Domain** has no external dependencies
2. **App** depends only on Domain
3. **Infrastructure** implements interfaces from App/Domain
4. **Api** orchestrates via dependency injection
5. Use MediatR for all business operations
6. Repository pattern for data access
7. DTOs for API contracts, separate from domain models
- STOP COMMITTING AND PUSHING WITHOUT ASKING ME FIRST!!!!!!!!!