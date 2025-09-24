# CRUD Application - Big Picture Overview

## What This Project Is

A **multi-platform CRUD application** demonstrating Clean Architecture best practices with a modern tech stack. This serves as both a working application and an opinionated template for building scalable, maintainable business applications.

## Architecture Overview

**Clean Architecture + DDD** with clear separation of concerns:
- **Domain** → Business entities and logic (no dependencies)
- **Application** → Use cases, CQRS handlers, DTOs
- **Infrastructure** → EF Core, repositories, external services
- **API** → ASP.NET Core controllers, REST endpoints
- **UI** → Angular frontend, future MAUI mobile

## Tech Stack

### Backend (.NET 8)
- **Framework**: ASP.NET Core 8 with Clean Architecture
- **Data**: Entity Framework Core + SQLite
- **Patterns**: MediatR (CQRS), Repository Pattern, Specification Pattern
- **Testing**: xUnit, Moq, FluentAssertions, Playwright E2E

### Frontend (Angular 20)
- **Framework**: Angular 20 with TypeScript
- **State**: RxJS reactive programming
- **Testing**: Karma, Jasmine
- **Build**: Angular CLI with proxy configuration

### Infrastructure
- **Database**: SQLite (development), configurable for production
- **CI/CD**: GitHub Actions with automated testing
- **Quality**: Automated formatting, linting, comprehensive test suite

## Key Features

### Implemented ✅
- **Complete CRUD operations** for People, Roles, Walls, Windows
- **JWT Authentication** with secure token handling
- **Clean Architecture** with proper layer separation
- **CQRS Pattern** using MediatR for all operations
- **Comprehensive testing** (Unit, Integration, E2E)
- **CI/CD pipeline** with automated quality checks
- **Response caching** and performance optimization
- **Serial E2E testing** for reliable CI execution

### In Progress 🚧
- **API response compression** for performance
- **Enhanced error handling** and logging
- **Performance monitoring** and optimization
- **Security hardening** and audit trails

## Development Approach

### Agent-Assisted Development
This project uses **AI-powered development workflows** with structured specifications, automated task tracking, and comprehensive documentation. See [Agent Utilization Guide](../03-development/Agent-Utilization-Guide.md) for details.

### Quality-First Approach
- **No failures tolerated** - issues are resolved immediately
- **Comprehensive testing** at all levels
- **Automated quality gates** in CI/CD
- **Documentation-driven** development process

## Project Goals

1. **Demonstrate Clean Architecture** in a real-world application
2. **Provide an opinionated template** for CRUD applications
3. **Showcase modern .NET + Angular** development practices
4. **Enable rapid prototyping** of business applications
5. **Serve as a learning resource** for Clean Architecture patterns

## Getting Started

1. **Environment Setup** → See [05-environment/1-development-setup.md](../05-environment/1-development-setup.md)
2. **Architecture Understanding** → See [02-architecture/1-architecture-guidelines.md](../02-architecture/1-architecture-guidelines.md)
3. **Development Process** → See [03-development/6-workflows/development-workflow.md](../03-development/6-workflows/development-workflow.md)
4. **Testing Setup** → See [05-environment/2-testing-setup.md](../05-environment/2-testing-setup.md)

## Quick Launch

```bash
# Start both API and Angular
.scripts/LaunchApps.ps1

# Access application
# API: http://localhost:5172
# UI: http://localhost:4200
```

This overview provides the 30,000-foot view. Drill down into specific areas using the documentation structure for detailed implementation guidance.