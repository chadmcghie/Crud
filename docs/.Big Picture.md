# Big Picture
The Crud project is intended as deeply opinionated, configured, and veted template for projects that will be a majority crud operations.  

- It's a strongly opinionated project that has been built up over time to include a lot of best practices and patterns.  
- It is not intended to be a starting point for all projects, but rather a starting point for projects that will be a majority crud.  
- It is also intended to be a starting point for projects that will be built using .NET 8 and C# 12.

## Key Features:
- **Clean Architecture**: The project is structured using the principles of Clean Architecture, ensuring a clear separation of concerns and maintainability.
- **SOLID Principles**: The codebase adheres to SOLID principles, promoting good design and architecture practices.
- **MediatR and CQRS**: The project utilizes MediatR for implementing the CQRS pattern, facilitating a clear separation between commands and queries.
- **Entity Framework Core**: The project uses Entity Framework Core for data access, providing a robust and flexible ORM solution.
- **Dependency Injection**: The project is built with dependency injection in mind, making it easy to manage dependencies and promote testability.
- **Resilience and Retry Policies**: The project incorporates Polly for implementing resilience and retry policies, enhancing the robustness of the application.
- **Comprehensive Testing**: The project includes a comprehensive testing strategy, covering unit tests, integration tests, and end-to-end tests to ensure code quality and reliability.
- **CI/CD Integration**: The project is designed to integrate seamlessly with CI/CD pipelines, facilitating automated builds, tests, and deployments.
- **Documentation**: The project includes extensive documentation, providing guidance on architecture, design choices, workflows, and more.
- **Extensibility**: The project is designed to be easily extensible, allowing developers to add new features and functionality as needed.

## High Level Directory Structure

- [Architecture] 
- [Decisions]
- [Development]
- [Misc]
- [Quality Control]

## Useful Documents

- [.Glossary.md] - Definitions of key terms and acronyms used throughout the documentation.
- [.Index.md] - Main index of the documentation.
