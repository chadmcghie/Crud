# Development Best Practices

## Context

Global development guidelines for Agent OS projects, aligned with Clean Architecture and SOLID principles.

<conditional-block context-check="core-principles">
IF this Core Principles section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Core Principles already in context"
ELSE:
  READ: The following principles

## Core Principles

### SOLID Principles
- **Single Responsibility**: Each class/method has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Clients shouldn't depend on interfaces they don't use
- **Dependency Inversion**: Depend on abstractions, not concretions

### Clean Architecture Layers
- **Domain**: Core business logic, entities, value objects (no dependencies)
- **Application**: Use cases, DTOs, interfaces (depends only on Domain)
- **Infrastructure**: Data access, external services (implements Application interfaces)
- **Presentation**: Controllers, UI components (depends on Application)

### Keep It Simple (KISS)
- Implement code in the fewest lines possible
- Avoid over-engineering solutions
- Choose straightforward approaches over clever ones
- Follow YAGNI (You Aren't Gonna Need It)

### Optimize for Readability
- Prioritize code clarity over micro-optimizations
- Write self-documenting code with clear variable names
- Add comments for "why" not "what"
- Use meaningful method and class names

### DRY (Don't Repeat Yourself)
- Extract repeated business logic to domain services
- Extract repeated UI markup to reusable components
- Create utility functions for common operations
- Use inheritance and composition appropriately

### File Structure & Organization
- Keep files focused on a single responsibility
- Group related functionality together
- Use consistent naming conventions (PascalCase for C#, camelCase for TypeScript)
- Organize by feature, not by file type
</conditional-block>

<conditional-block context-check="dependencies" task-condition="choosing-external-library">
IF current task involves choosing an external library:
  IF Dependencies section already read in current context:
    SKIP: Re-reading this section
    NOTE: "Using Dependencies guidelines already in context"
  ELSE:
    READ: The following guidelines
ELSE:
  SKIP: Dependencies section not relevant to current task

## Dependencies

### Choose Libraries Wisely
When adding third-party dependencies:
- Select the most popular and actively maintained option
- Check the library's GitHub repository for:
  - Recent commits (within last 6 months)
  - Active issue resolution
  - Number of stars/downloads
  - Clear documentation
</conditional-block>
