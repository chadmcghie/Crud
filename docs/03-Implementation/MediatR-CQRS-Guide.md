# MediatR CQRS Implementation Guide

This document outlines how to implement the MediatR CQRS pattern for entities in the application.

## Overview

The implementation follows Command Query Responsibility Segregation (CQRS) using MediatR:
- **Commands**: Handle write operations (Create, Update, Delete)
- **Queries**: Handle read operations (Get, List)
- **Handlers**: Process commands and queries by delegating to existing services

## Implementation Pattern

### 1. Directory Structure
```
src/App/
├── Commands/
│   └── {EntityName}/
│       ├── Create{EntityName}Command.cs
│       ├── Create{EntityName}CommandHandler.cs
│       ├── Update{EntityName}Command.cs
│       ├── Update{EntityName}CommandHandler.cs
│       ├── Delete{EntityName}Command.cs
│       └── Delete{EntityName}CommandHandler.cs
└── Queries/
    └── {EntityName}/
        ├── Get{EntityName}Query.cs
        ├── Get{EntityName}QueryHandler.cs
        ├── List{EntityName}Query.cs
        └── List{EntityName}QueryHandler.cs
```

### 2. Commands Implementation

#### Create Command
```csharp
using Domain.Entities;
using MediatR;

namespace App.Commands.{EntityName};

public record Create{EntityName}Command(string Property1, string? Property2) : IRequest<{EntityName}>;
```

#### Create Command Handler
```csharp
using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Commands.{EntityName};

public class Create{EntityName}CommandHandler(I{EntityName}Service entityService) : IRequestHandler<Create{EntityName}Command, {EntityName}>
{
    public async Task<{EntityName}> Handle(Create{EntityName}Command request, CancellationToken cancellationToken)
    {
        return await entityService.CreateAsync(request.Property1, request.Property2, cancellationToken);
    }
}
```

### 3. Queries Implementation

#### Get Query
```csharp
using Domain.Entities;
using MediatR;

namespace App.Queries.{EntityName};

public record Get{EntityName}Query(Guid Id) : IRequest<{EntityName}?>;
```

#### Get Query Handler
```csharp
using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Queries.{EntityName};

public class Get{EntityName}QueryHandler(I{EntityName}Service entityService) : IRequestHandler<Get{EntityName}Query, {EntityName}?>
{
    public async Task<{EntityName}?> Handle(Get{EntityName}Query request, CancellationToken cancellationToken)
    {
        return await entityService.GetAsync(request.Id, cancellationToken);
    }
}
```

### 4. Controller Update

Replace service injection with MediatR:

```csharp
// Before (Direct Service)
public class {EntityName}Controller(I{EntityName}Service entityService) : ControllerBase

// After (MediatR)
public class {EntityName}Controller(IMediator mediator) : ControllerBase
```

Update action methods:
```csharp
// Before
var entity = await entityService.CreateAsync(request.Name, request.Description, ct);

// After  
var entity = await mediator.Send(new Create{EntityName}Command(request.Name, request.Description), ct);
```

## Benefits

1. **Separation of Concerns**: Commands and queries are separate, making code easier to understand and maintain
2. **Single Responsibility**: Each handler has one specific responsibility
3. **Testability**: Handlers can be tested independently
4. **Cross-cutting Concerns**: Easy to add behaviors like logging, validation, caching through MediatR pipeline behaviors
5. **Minimal Changes**: Existing services remain intact, reducing risk

## Next Steps for Other Entities

To implement this pattern for People, Walls, and Windows:

1. Create Commands and Queries directories for each entity
2. Implement the command/query classes and handlers following the pattern shown for Roles
3. Update the respective controllers to use IMediator instead of direct service injection
4. Test each endpoint to ensure proper functionality

The existing service layer remains unchanged, providing a safety net during the transition.