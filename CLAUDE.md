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

The `test:smoke`, `test:critical`, and `test:extended` commands in `test/Tests.E2E.NG/package.json` MUST use `playwright.config.webserver.ts`. 

**DO NOT "simplify" them to use the default playwright.config.ts** - this will break CI!

### Why This Matters
- The default `playwright.config.ts` uses `global-setup.ts` which fails in CI
- The complex-looking commands with environment variables are REQUIRED
- This has been broken and fixed multiple times - don't repeat the mistake
- See GitHub issue #79 for plan to eliminate the problematic config entirely

### Correct Commands (DO NOT CHANGE)
```bash
# These commands MUST use playwright.config.webserver.ts
npm run test:smoke       # Uses webserver config - WORKS IN CI
npm run test:critical    # Uses webserver config - WORKS IN CI  
npm run test:extended    # Uses webserver config - WORKS IN CI
npm run test:webserver   # Full E2E suite with webserver config
```

### What NOT to Do
```bash
# NEVER change to these "simpler" versions - THEY BREAK CI
"test:smoke": "playwright test --grep @smoke"  # ❌ BROKEN IN CI
"test:critical": "playwright test --grep @critical"  # ❌ BROKEN IN CI
```

**UPDATE**: E2E tests use Playwright's built-in webServer configuration. See `docs/Decisions/0003-E2E-Testing-Database-Use-Playwrights-webServer.md` for details.

- **Playwright webServer**: Automatic server management, unique database per test run (recommended approach)
- Tests are tagged: `@smoke` (2 min), `@critical` (5 min), `@extended` (10 min)
- Database isolation via unique filenames prevents locking issues
- Serial execution strategy (`workers: 1`) for SQLite/EF Core compatibility
- CI/CD uses same webServer configuration as local development

## Project Structure

- `src/Domain/` - Business entities and logic (no dependencies)
- `src/App/` - Application services, CQRS handlers, DTOs
- `src/Infrastructure/` - EF Core, repositories, external services
- `src/Api/` - ASP.NET Core Web API controllers
- `src/Angular/` - Angular frontend application
- `test/Tests.Unit.Backend/` - xUnit, Moq, FluentAssertions
- `test/Tests.Integration.Backend/` - API integration tests with WebApplicationFactory
- `test/Tests.Integration.NG/` - Angular integration tests with Karma
- `test/Tests.E2E.NG/` - Playwright E2E tests
- `.claude/` - Claude Code configuration and custom tools

## Claude Configuration (.claude folder)

The `.claude` folder contains project-specific Claude Code configurations and tools:

### Structure
- `.claude/settings.local.json` - Pre-approved actions and permissions
- `.claude/commands/` - Custom slash commands for common workflows
- `.claude/agents/` - Specialized agent configurations
- `.claude/.agent-os/` - Agent OS standards and instructions

### Available Commands
Located in `.claude/commands/`:
- `analyze-product` - Analyze codebase and install Agent OS
- `create-spec` - Create specifications for features
- `create-tasks` - Break down work into manageable tasks
- `document-blocker` - Document blocking issues
- `execute-tasks` - Execute planned tasks
- `plan-product` - Plan product features and architecture
- `summarize-thread` - Summarize conversation threads
- `troubleshoot-issues` - Debug and resolve problems

### Specialized Agents
Located in `.claude/agents/`:
- `test-runner` - Run tests and analyze failures
- `file-creator` - Create files with proper structure
- `git-workflow` - Handle git operations and PRs
- `project-manager` - Track tasks and roadmaps
- `context-fetcher` - Retrieve relevant documentation
- `date-checker` - Determine current date
- `document-blocking-issue` - Document critical blockers
- `troubleshoot-with-history` - Debug with context history

### Agent OS Standards
Located in `.claude/.agent-os/`:
- `standards/` - Code style guides (C#, JS, CSS, HTML), best practices
- `instructions/` - Core workflows for analysis, specs, tasks, execution

### Recommended Additional Folders
Consider adding these to `.claude/` for better context:
- `templates/` - File templates for common patterns
- `snippets/` - Reusable code snippets
- `workflows/` - Multi-step process definitions
- `context/` - Project-specific context and decisions

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
## Important Rules

- **NEVER commit or push without explicit permission**
- **NEVER create documentation files unless explicitly requested**
- **ALWAYS prefer editing existing files over creating new ones**
- **ALWAYS run code formatting before committing:**
  - `dotnet format solutions/Crud.sln` for .NET code
  - `npm run lint` in src/Angular for TypeScript code

## Key References

- API ports: 5172 (HTTP), 7268 (HTTPS)
- Angular port: 4200
- E2E test fix discussion: @"docs\Misc\AI Discussions\claude-task-e2e-test-serial-execution-fix-20250828.md"
- Serial testing decision: @docs\Decisions\0001-Serial-E2E-Testing.md
- Dev branch is the default branch