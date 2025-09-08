# Agent OS Memory References

> Important conversation and decision references for Agent OS context
> Last Updated: 2025-09-08

## Architecture & Design Decisions

### Clean Architecture Implementation
**Memory ID**: `mem_clean_arch_001`  
**Context**: Initial architecture setup with Domain, App, Infrastructure, Api layers  
**Key Points**:
- Domain layer has no dependencies
- App layer depends only on Domain
- Infrastructure implements interfaces from App/Domain
- Api orchestrates via dependency injection
**Tags**: `architecture`, `clean-architecture`, `design`  
**Date**: 2025-01-15

### E2E Testing Strategy
**Memory ID**: `mem_e2e_serial_001`  
**Context**: Decision to use serial execution for E2E tests  
**Key Points**:
- SQLite doesn't handle concurrent access well
- Playwright workers set to 1 for stability
- Unique database per test run pattern
**Tags**: `testing`, `e2e`, `playwright`, `sqlite`  
**Date**: 2025-08-28  
**References**: ADR-001, Issue #79

## Feature Implementations

### JWT Authentication Setup
**Memory ID**: `mem_jwt_auth_001`  
**Context**: Implementation of JWT authentication in .NET/Angular  
**Key Points**:
- Token refresh mechanism required
- Interceptor for automatic token renewal
- Secure storage in Angular using httpOnly cookies
**Tags**: `authentication`, `jwt`, `security`  
**Date**: 2025-08-29

### Redis Caching Layer
**Memory ID**: `mem_redis_cache_001`  
**Context**: Adding Redis caching for performance optimization  
**Key Points**:
- Docker compose setup for local Redis
- Repository pattern with caching decorator
- Cache invalidation strategies
**Tags**: `caching`, `redis`, `performance`  
**Date**: 2025-09-08  
**Reference**: Issue #37

## Problem Resolutions

### Database Migration Conflicts
**Memory ID**: `mem_migration_fix_001`  
**Context**: Resolving EF Core migration conflicts in team environment  
**Key Points**:
- Feature branch migration strategy
- Squash migrations before merge
- Regenerate on main branch
**Tags**: `ef-core`, `migrations`, `database`  
**Date**: 2025-07-15

### Angular Build Optimization
**Memory ID**: `mem_angular_build_001`  
**Context**: Reducing Angular build times from 8+ minutes to under 2  
**Key Points**:
- Skip Angular build environment variable
- Incremental compilation settings
- Build cache optimization
**Tags**: `angular`, `build`, `performance`  
**Date**: 2025-06-20

## Agent OS Specific

### Agent Orchestrator Design
**Memory ID**: `mem_orchestrator_001`  
**Context**: Meta-agent for delegating to specialized agents  
**Key Points**:
- Request analysis and routing logic
- Agent dependency graph
- Error handling and retry patterns
**Tags**: `agent-os`, `orchestrator`, `agents`  
**Date**: 2025-09-08  
**Reference**: Issue #154

### Template System Architecture
**Memory ID**: `mem_templates_001`  
**Context**: Reusable template system for code generation  
**Key Points**:
- Variable substitution with {{ENTITY_NAME}} syntax
- Template inheritance and composition
- Type mapping between C# and TypeScript
**Tags**: `templates`, `code-generation`, `agent-os`  
**Date**: 2025-09-08  
**Reference**: Issue #155

### Progress Tracking Implementation
**Memory ID**: `mem_progress_track_001`  
**Context**: Status dashboard for spec/task completion  
**Key Points**:
- Markdown-based status files
- Automatic aggregation across specs
- GitHub issue integration
**Tags**: `progress`, `tracking`, `agent-os`  
**Date**: 2025-09-08  
**Reference**: Issue #156

## Configuration & Setup

### Development Environment
**Memory ID**: `mem_dev_env_001`  
**Context**: Standard development environment setup  
**Key Points**:
- .NET 8 SDK required
- Node.js 18+ for Angular
- Docker for Redis/PostgreSQL
- PowerShell scripts for Windows
**Tags**: `setup`, `environment`, `development`  
**Date**: 2025-01-10

### CI/CD Pipeline Configuration
**Memory ID**: `mem_cicd_001`  
**Context**: GitHub Actions workflow setup  
**Key Points**:
- Parallel jobs for backend/frontend
- E2E tests run serially
- Docker compose for integration tests
**Tags**: `ci`, `cd`, `github-actions`  
**Date**: 2025-02-15

## Code Standards

### C# Coding Standards
**Memory ID**: `mem_csharp_standards_001`  
**Context**: Team coding standards for C# development  
**Key Points**:
- PascalCase for public members
- Explicit access modifiers
- LINQ method syntax preferred
- Async suffix for async methods
**Tags**: `standards`, `csharp`, `coding`  
**Date**: 2025-01-20

### Angular/TypeScript Standards
**Memory ID**: `mem_angular_standards_001`  
**Context**: Frontend development standards  
**Key Points**:
- Single quotes for strings
- Interface prefix with 'I'
- RxJS takeUntil pattern for subscriptions
- Barrel exports for feature modules
**Tags**: `standards`, `angular`, `typescript`  
**Date**: 2025-01-20

## Troubleshooting History

### SQLite Lock Resolution
**Memory ID**: `mem_sqlite_lock_fix_001`  
**Context**: Comprehensive fix for database locking issues  
**Key Points**:
- Serial test execution is mandatory
- Unique database names per test
- Proper disposal patterns
**Tags**: `troubleshooting`, `sqlite`, `testing`  
**Date**: 2025-08-28

### CORS Configuration Fix
**Memory ID**: `mem_cors_fix_001`  
**Context**: Resolving CORS issues between Angular and .NET API  
**Key Points**:
- Explicit origin configuration required
- AllowCredentials for auth cookies
- Order of middleware matters
**Tags**: `cors`, `api`, `troubleshooting`  
**Date**: 2025-03-10

## Quick Reference Index

### By Category
- **Architecture**: `mem_clean_arch_001`
- **Testing**: `mem_e2e_serial_001`, `mem_sqlite_lock_fix_001`
- **Authentication**: `mem_jwt_auth_001`
- **Performance**: `mem_redis_cache_001`, `mem_angular_build_001`
- **Agent OS**: `mem_orchestrator_001`, `mem_templates_001`, `mem_progress_track_001`
- **Standards**: `mem_csharp_standards_001`, `mem_angular_standards_001`

### By Technology
- **.NET/C#**: `mem_clean_arch_001`, `mem_jwt_auth_001`, `mem_csharp_standards_001`
- **Angular**: `mem_angular_build_001`, `mem_angular_standards_001`
- **Database**: `mem_migration_fix_001`, `mem_sqlite_lock_fix_001`
- **Testing**: `mem_e2e_serial_001`
- **DevOps**: `mem_cicd_001`, `mem_dev_env_001`

---

*Memory references are added as significant decisions are made and problems are resolved. Each reference should provide enough context to understand the decision without needing to access the original conversation.*