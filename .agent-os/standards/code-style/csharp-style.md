# C# Style Guide

## Context

C# and .NET-specific coding standards for the CRUD platform backend.

## C# Conventions

### Code Structure
- Use file-scoped namespaces: `namespace MyApp.Domain;`
- One class per file, file name matches class name
- Use `using` statements at the top of files
- Group using statements: System, third-party, local

### Class Design
- Follow Single Responsibility Principle
- Use sealed classes when inheritance is not intended
- Prefer composition over inheritance
- Use readonly fields for immutable data

### Methods and Properties
- Use expression-bodied members for simple operations: `public string Name => _name;`
- Use auto-properties when no logic is needed: `public string Name { get; set; }`
- Make methods async when performing I/O operations
- Use meaningful parameter names

### Clean Architecture Patterns

#### Domain Layer
- Keep entities free of external dependencies
- Use value objects for complex data types
- Implement domain events for cross-aggregate communication
- Use guard clauses for validation: `Guard.Against.Null(parameter)`

#### Application Layer
- Use CQRS pattern with MediatR
- Implement request/response DTOs
- Use FluentValidation for input validation
- Handle cross-cutting concerns with behaviors

#### Infrastructure Layer
- Implement repository pattern with specifications
- Use Entity Framework Core with proper configuration
- Handle database migrations appropriately
- Implement proper logging and error handling

### Dependency Injection
- Register services in Program.cs or extension methods
- Use appropriate service lifetimes (Singleton, Scoped, Transient)
- Prefer constructor injection over property injection
- Use interfaces for all injectable services

### Error Handling
- Use specific exception types
- Don't catch and rethrow without adding value
- Use Result pattern for business logic errors
- Log exceptions with appropriate context

### Async/Await
- Use async/await for I/O bound operations
- Don't use async void (except for event handlers)
- Use ConfigureAwait(false) in library code
- Avoid blocking on async code with .Result or .Wait()

### Testing
- Use xUnit for unit testing
- Use FluentAssertions for readable assertions
- Mock dependencies with Moq or NSubstitute
- Use Testcontainers for integration tests
- Follow AAA pattern (Arrange, Act, Assert)

### Performance Considerations
- Use StringBuilder for string concatenation in loops
- Prefer `List<T>` over `IEnumerable<T>` when multiple iterations are needed
- Use `Span<T>` and `Memory<T>` for high-performance scenarios
- Consider using `ValueTask<T>` for frequently called async methods

### Code Documentation
- Use XML documentation comments for public APIs
- Document complex business logic
- Keep comments up to date with code changes
- Use meaningful variable and method names that reduce need for comments
