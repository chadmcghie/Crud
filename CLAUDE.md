# CLAUDE.md
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Settings 
.claude\settings.local.json


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
.scripts/LaunchApps.ps1

# Kill running servers before builds
.scripts/kill-servers.ps1

# Start API only
dotnet run --project src/Api/Api.csproj --launch-profile http

# Start Angular only
cd src/Angular && npm start
```

### Build
```bash
# Build entire solution
dotnet build solutions/Crud.sln

# Build backend only
dotnet build solutions/Crud.Backend.sln

# Build Angular
cd src/Angular && npm run build
```

### Testing
```bash
# Backend unit tests
dotnet test test/Tests.Unit.Backend/Tests.Unit.Backend.csproj

# Backend integration tests
dotnet test test/Tests.Integration.Backend/Tests.Integration.Backend.csproj

# E2E tests (using Playwright webServer - see ADR-003)
cd test/Tests.E2E.NG
npm run test:webserver   # Recommended: Playwright manages servers
npm run test:smoke       # 2-minute smoke tests only
npm run test:critical    # 5-minute critical tests
npm run test:headed      # Run with visible browser
npm run test:serial      # Serial execution with single worker

# Angular tests
cd src/Angular && npm test
```

### Code Quality & Formatting
```bash
# IMPORTANT: Always run before committing!

# .NET formatting - check for issues
dotnet format solutions/Crud.sln --verify-no-changes

# .NET formatting - auto-fix issues
dotnet format solutions/Crud.sln

# Angular linting
cd src/Angular && npm run lint
```

## E2E Testing Strategy

⚠️ **CRITICAL: DO NOT CHANGE THE TEST COMMANDS** ⚠️

The `test:smoke`, `test:critical`, and `test:extended` commands in `test/Tests.E2E.NG/package.json` require specific environment variables for CI compatibility. 

**DO NOT "simplify" them by removing the environment variables** - this will break CI!

### Why This Matters
- The complex-looking commands with environment variables are REQUIRED
- This has been broken and fixed multiple times - don't repeat the mistake
- See GitHub issue #79 for plan to eliminate the problematic config entirely

### Correct Commands (DO NOT CHANGE)
```bash
# These commands use environment variables for proper configuration
npm run test:smoke       # 2-minute smoke tests
npm run test:critical    # 5-minute critical tests  
npm run test:extended    # Extended test suite
```

### What NOT to Do
```bash
# NEVER change to these "simpler" versions - THEY BREAK CI
"test:smoke": "playwright test --grep @smoke"  # ❌ BROKEN IN CI
"test:critical": "playwright test --grep @critical"  # ❌ BROKEN IN CI
```

**UPDATE**: E2E tests use Playwright's built-in webServer configuration (now in the default `playwright.config.ts`). See `docs/Decisions/0003-E2E-Testing-Database-Use-Playwrights-webServer.md` for details.

- **Playwright webServer**: Automatic server management, unique database per test run (built into `playwright.config.ts`)
- Tests are tagged: `@smoke` (2 min), `@critical` (5 min), `@extended` (10 min)
- Database isolation via unique filenames prevents locking issues
- Serial execution strategy (`workers: 1`) for SQLite/EF Core compatibility
- CI/CD uses same webServer configuration as local development

## Project Structure

### Source Code (`src/`)
- `src/Domain/` - Business entities and logic (no dependencies)
  - `Entities/` - Domain entities (Person, Role, Wall, Window, etc.)
  - `Enums/` - Domain enumerations
  - `Events/` - Domain events
  - `Interfaces/` - Domain interfaces
  - `Specifications/` - Domain specifications
  - `Validators/` - Domain validators
  - `ValueObjects/` - Domain value objects
- `src/App/` - Application services, CQRS handlers, DTOs
  - `Abstractions/` - Application abstractions
  - `Behaviors/` - MediatR behaviors
  - `Features/` - Feature-based organization (Authentication, People, Roles, Walls, Windows)
  - `Interfaces/` - Application interfaces
  - `Mappings/` - AutoMapper profiles
  - `Models/` - Application models
  - `Services/` - Application services
- `src/Infrastructure/` - EF Core, repositories, external services
  - `Data/` - DbContext and configurations
  - `Migrations/` - Entity Framework migrations
  - `Repositories/` - Repository implementations
  - `Resilience/` - Polly resilience patterns
  - `Services/` - Infrastructure services
- `src/Api/` - ASP.NET Core Web API controllers
  - `Controllers/` - API controllers
  - `Dtos/` - Data transfer objects
  - `Mappings/` - API mapping profiles
  - `Middleware/` - Custom middleware
  - `Validators/` - API validators
- `src/Angular/` - Angular frontend application
  - `src/app/` - Angular application code
  - `src/assets/` - Static assets
  - `public/` - Public assets
  - `proxy.conf*.json` - Development proxy configurations

### Test Projects (`test/`)
- `test/Tests.Unit.Backend/` - xUnit, Moq, FluentAssertions
  - `App/` - Application layer unit tests
  - `Domain/` - Domain layer unit tests
  - `Infrastructure/` - Infrastructure layer unit tests
  - `TestData/` - Test data builders
  - `Validators/` - Validator unit tests
- `test/Tests.Integration.Backend/` - API integration tests with WebApplicationFactory
  - `Api/` - API integration tests
  - `Controllers/` - Controller integration tests
  - `E2E/` - End-to-end integration tests
  - `Infrastructure/` - Infrastructure integration tests
- `test/Tests.Integration.NG/` - Angular integration tests with Karma
- `test/Tests.E2E.NG/` - Playwright E2E tests
  - `tests/` - E2E test files organized by feature
  - `test-artifacts/` - Test artifacts and reports
  - `test-results/` - Test execution results

### Solutions (`solutions/`)
- `solutions/Crud.sln` - Complete solution file
- `solutions/Crud.Backend.sln` - Backend-only solution
- `solutions/Crud.Angular.sln` - Angular-only solution

### Documentation (`docs/`)
- `docs/Architecture/` - Architecture documentation and guidelines
- `docs/Development/` - Development guides, specs, and workflows
- `docs/Misc/` - Miscellaneous documentation and references
- `docs/QualityControl/` - Quality control and review documentation

### Scripts (`scripts/`)
- `scripts/LaunchApps.ps1` - Launch both API and Angular
- `scripts/kill-servers.ps1` - Kill running servers

### Configuration Files
- `global.json` - .NET SDK version specification
- `package-lock.json` - Root npm dependencies
- `test-formatting.sh` - Test formatting script

## Database

- **Development**: SQLite with file-based storage
- **Database Files**: Multiple SQLite databases for different environments
  - `src/Api/CrudApp.db` - Main development database
  - `src/Api/CrudAppDev.db` - Development database
  - `src/Api/CrudAppDesignTime.db` - Design-time database
  - `CrudTest_local.db` - Local test database
- **Connection strings**: Configured in `appsettings.*.json` files
- **Migrations**: `dotnet ef migrations add <name> -p src/Infrastructure -s src/Api`
- **Update database**: `dotnet ef database update -p src/Infrastructure -s src/Api`

## Clean Architecture Rules

1. **Domain** has no external dependencies
2. **App** depends only on Domain
3. **Infrastructure** implements interfaces from App/Domain
4. **Api** orchestrates via dependency injection
5. Use MediatR for all business operations
6. Repository pattern for data access
7. DTOs for API contracts, separate from domain models

## Important Rules

- **NEVER commit or push without explicit permission**
- **NEVER create documentation files unless explicitly requested**
- **ALWAYS prefer editing existing files over creating new ones**
- **ALWAYS run code formatting before committing:**
  - `dotnet format solutions/Crud.sln` for .NET code
  - `npm run lint` in src/Angular for TypeScript code

## Known Issues & Workarounds

### Settings.local.json Not Auto-Loading
- **Issue**: `.claude/settings.local.json` permissions don't load automatically at startup
- **Workaround**: Run `/permissions` command once at session start to trigger settings loading
- **Fix Applied**: Removed conflicting `"Bash(echo:*)"` from "ask" section that was overriding specific echo commands in "allow" section

## Key References

- API ports: 5172 (HTTP), 7268 (HTTPS)
- Angular port: 4200
- E2E test fix discussion: @"docs\Misc\AI Discussions\claude-task-e2e-test-serial-execution-fix-20250828.md"
- Serial testing decision: @docs\Decisions\0001-Serial-E2E-Testing.md
- Dev branch is the default branch
- No Failures Ever - We don't try and move past it.  We will troubleshoot and solve it.  Use additional tools if necessary.