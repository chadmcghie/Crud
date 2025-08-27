# Crud - Multi-Platform Application

A comprehensive multi-platform application built with .NET 8, featuring Clean Architecture principles and multiple UI frameworks.

## 🏗️ Architecture

This project follows Clean Architecture principles with the following structure:

- **Domain Layer**: Core business logic and entities
- **Infrastructure Layer**: Data access and external service implementations
- **Application Layer**: Use cases and business rules
- **API Layer**: RESTful API endpoints
- **UI Layers**: Multiple frontend implementations

## 🚀 Technologies

- **Backend**: .NET 8, Entity Framework Core
- **APIs**: 
  - ASP.NET Core Web API
- **Frontends**:
  - Angular (Web)
  - .NET MAUI (Cross-platform mobile/desktop)
- **Testing**: Unit, Integration, and E2E tests

## 📁 Project Structure

```
src/
├── Domain/           # Core business logic and entities
├── Infrastructure/   # Data access and external services
├── App/             # Application layer (use cases)
├── Api/             # ASP.NET Core Web API
├── Angular/         # Angular web frontend
├── Maui/            # .NET MAUI cross-platform app
└── Shared/          # Shared components and utilities

test/
├── Tests.Unit.Backend/      # Backend unit tests
├── Tests.Integration.Backend/ # Backend integration tests
├── Tests.Integration.NG/    # Angular integration tests
├── Tests.Integration.Maui/  # MAUI integration tests
├── Tests.E2E.NG/           # Angular end-to-end tests
└── Tests.E2E.Maui/         # MAUI end-to-end tests

docs/
├── 01-Getting Started/     # Project setup and overview
├── 02-Architecture/        # Architecture decisions and patterns
├── 03-Business Domain/     # Domain-specific documentation
├── 04-Project Management/  # Roadmaps and team strategy
└── 05-Quality Control/     # Testing strategies and code reviews
```

## 🛠️ Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js (for Angular development)
- Visual Studio 2022 or VS Code
- Git

### Building the Solution

1. Clone the repository
2. Open `Crud.All.sln` in Visual Studio
3. Restore NuGet packages
4. Build the solution

### Running Individual Components

#### Backend APIs
```bash
# API
cd src/Api
dotnet run
```

#### Angular Frontend
```bash
cd src/Angular
npm install
ng serve
```

#### MAUI Application
```bash
cd src/Maui
dotnet build
dotnet run
```

## 🧪 Testing

The project includes comprehensive testing strategies:

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **End-to-End Tests**: Test complete user workflows

Run tests using:
```bash
# All tests
dotnet test

# Specific test project
dotnet test test/Tests.Unit.Backend/
```

## 🚀 CI/CD Pipeline

Our continuous integration and deployment pipeline ensures code quality and automates deployments:

### Branch Strategy
- **`dev`** - Default branch, integration point for all features
- **`main`** - Production branch, requires management approval
- **`feature/*`** - Feature development branches
- **`bugfix/*`** - Bug fix branches
- **`hotfix/*`** - Emergency production fixes

### Automated Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `feature-branch-tests.yml` | Push to feature/* | Quick unit tests for rapid feedback |
| `pr-validation.yml` | PR to dev | Full test suite validation |
| `deploy-staging.yml` | Merge to dev | Auto-deploy to staging environment |
| `deploy-production.yml` | Merge to main | Production deployment with approvals |

### Development Workflow
1. Create feature branch from `dev`
2. Push commits (triggers quick tests)
3. Open PR to `dev` (triggers full test suite)
4. After merge, auto-deploys to staging
5. Management reviews staging
6. PR from `dev` to `main` for production

For detailed CI/CD configuration, see [Branch Protection Rules](.github/BRANCH_PROTECTION_RULES.md)

## 📚 Documentation

Comprehensive documentation is available in the `docs/` directory:

- [Big Picture](docs/01-Getting%20Started/Big%20Picture.md) - High-level project overview
- [Architecture Guidelines](docs/Architecture%20Guidelines.md) - Design principles and patterns
- [Testing Strategy](docs/05-Quality%20Control/Testing/1-Testing%20Strategy.md) - Testing approach and guidelines

## 🤝 Contributing

1. Follow the established architecture patterns
2. Write tests for new features
3. Update documentation as needed
4. Follow SOLID principles and Clean Architecture

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🏆 Features

- **Clean Architecture**: Separation of concerns with clear layer boundaries
- **Multi-Platform**: Web, mobile, and desktop support
- **Comprehensive Testing**: Unit, integration, and E2E test coverage
- **Modern Stack**: Latest .NET 8 and Angular versions
- **Scalable Design**: Built for growth and maintainability
