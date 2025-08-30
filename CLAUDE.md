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
dotnet build Crud.All.sln

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

# E2E tests (serial execution - see ADR-001)
cd test/Tests.E2E.NG
npm run test:serial      # Full suite with server management
npm run test:fast        # Quick tests, assumes servers running
npm run test:smoke       # 2-minute smoke tests only
npm run test:single      # Run single test file

# Angular tests
cd src/Angular && npm test
```

### Linting
```bash
# Angular linting
cd src/Angular && npm run lint
```

## E2E Testing Strategy

**IMPORTANT**: E2E tests run serially (single worker) due to SQLite limitations. See `docs/Decisions/0001-Serial-E2E-Testing.md`.

- Tests are tagged: `@smoke` (2 min), `@critical` (5 min), `@extended` (10 min)
- Server management is automatic in `test:serial` configuration
- Database cleanup via file deletion between tests
- Use `test:fast` for development with manually started servers

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