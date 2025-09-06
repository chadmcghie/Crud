# MediatR CQRS Implementation Pattern

**Status**: ✅ **FULLY IMPLEMENTED** (2025-08-27)

This document describes how to implement MediatR CQRS commands and queries for controllers in the application.

## Overview

The application now uses MediatR to implement the CQRS (Command Query Responsibility Segregation) pattern. Controllers send commands and queries through MediatR instead of directly calling services.

## Pattern Structure

### 1. Commands and Queries
Located in: `src/App/Features/{EntityName}/`

**Commands.cs** - For write operations:
```csharp
public record Create{Entity}Command(...) : IRequest<{Entity}>;
public record Update{Entity}Command(...) : IRequest;
public record Delete{Entity}Command(Guid Id) : IRequest;
```

**Queries.cs** - For read operations:
```csharp
public record Get{Entity}Query(Guid Id) : IRequest<{Entity}?>;
public record List{Entity}Query : IRequest<IReadOnlyList<{Entity}>>;
```

### 2. Handlers
**CommandHandlers.cs** - Handle write operations:
```csharp
public class Create{Entity}CommandHandler(I{Entity}Service service) : IRequestHandler<Create{Entity}Command, {Entity}>
{
    public async Task<{Entity}> Handle(Create{Entity}Command request, CancellationToken cancellationToken)
    {
        return await service.CreateAsync(..., cancellationToken);
    }
}
```

**QueryHandlers.cs** - Handle read operations:
```csharp
public class Get{Entity}QueryHandler(I{Entity}Service service) : IRequestHandler<Get{Entity}Query, {Entity}?>
{
    public async Task<{Entity}?> Handle(Get{Entity}Query request, CancellationToken cancellationToken)
    {
        return await service.GetAsync(request.Id, cancellationToken);
    }
}
```

### 3. Controller Updates
Controllers inject `IMediator` instead of services:
```csharp
public class {Entity}Controller(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<{Entity}Dto>>> List(CancellationToken ct)
    {
        var items = await mediator.Send(new List{Entity}Query(), ct);
        return Ok(items.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<{Entity}Dto>> Create([FromBody] Create{Entity}Request request, CancellationToken ct)
    {
        var entity = await mediator.Send(new Create{Entity}Command(...), ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, MapToDto(entity));
    }
}
```

## Implementation Status

### ✅ Completed (All Controllers)
- **RolesController**: Fully implemented with MediatR CQRS pattern
  - Commands: CreateRoleCommand, UpdateRoleCommand, DeleteRoleCommand
  - Queries: GetRoleQuery, ListRolesQuery
  - All CRUD operations tested and working

- **PeopleController**: Fully implemented with MediatR CQRS pattern
  - Commands: CreatePersonCommand, UpdatePersonCommand, DeletePersonCommand
  - Queries: GetPersonQuery, ListPeopleQuery
  - All CRUD operations tested and working

- **WallsController**: Fully implemented with MediatR CQRS pattern
  - Commands: CreateWallCommand, UpdateWallCommand, DeleteWallCommand
  - Queries: GetWallQuery, ListWallsQuery
  - All CRUD operations tested and working

- **WindowsController**: Fully implemented with MediatR CQRS pattern
  - Commands: CreateWindowCommand, UpdateWindowCommand, DeleteWindowCommand
  - Queries: GetWindowQuery, ListWindowsQuery
  - All CRUD operations tested and working

## Benefits of MediatR CQRS Implementation

1. **Separation of Concerns**: Clear separation between commands (write) and queries (read)
2. **Decoupling**: Controllers are decoupled from specific service implementations
3. **Cross-cutting Concerns**: Easy to add pipeline behaviors for logging, validation, caching
4. **Testability**: Each handler can be tested independently
5. **Consistency**: Standardized request/response pattern across the application

## Completed Features

The MediatR CQRS implementation is now complete for all controllers with:

1. ✅ All controllers migrated to use IMediator
2. ✅ Complete command/query separation for all entities
3. ✅ Validation pipeline behavior implemented with FluentValidation
4. ✅ Error handling integrated through global middleware
5. ✅ Full test coverage for all handlers

## Additional Pipeline Behaviors

The following pipeline behaviors have been implemented:
- **ValidationBehavior**: Validates all requests using FluentValidation before handler execution
- **Global Exception Handling**: Catches and properly formats all errors