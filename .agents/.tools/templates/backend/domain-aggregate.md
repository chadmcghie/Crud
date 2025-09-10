# Domain Aggregate Template

## Aggregate: {{AGGREGATE_NAME}}

### Parent Task: Implement {{AGGREGATE_NAME}} Domain Aggregate

#### Subtasks:

- [ ] 1. Write tests for {{AGGREGATE_NAME}} aggregate
  - [ ] 1.1 Write unit tests for aggregate root invariants
  - [ ] 1.2 Write tests for domain events
  - [ ] 1.3 Write tests for value objects
  - [ ] 1.4 Write tests for business rules
  - [ ] 1.5 Write tests for aggregate methods

- [ ] 2. Create aggregate root entity
  - [ ] 2.1 Define {{AGGREGATE_NAME}} aggregate root class
  - [ ] 2.2 Implement {{AGGREGATE_NAME}}Id value object
  - [ ] 2.3 Define private fields and public properties
  - [ ] 2.4 Implement private constructor for EF Core
  - [ ] 2.5 Implement factory method for creation

- [ ] 3. Implement child entities
{{#each CHILD_ENTITIES}}
  - [ ] 3.{{@index+1}} Create {{this}} entity class
{{/each}}
  - [ ] 3.x Define relationships between entities
  - [ ] 3.x Implement collection management methods

- [ ] 4. Create value objects
{{#each VALUE_OBJECTS}}
  - [ ] 4.{{@index+1}} Implement {{this}} value object
{{/each}}
  - [ ] 4.x Add equality comparison
  - [ ] 4.x Add validation in constructors

- [ ] 5. Define domain events
  - [ ] 5.1 Create {{AGGREGATE_NAME}}CreatedEvent
  - [ ] 5.2 Create {{AGGREGATE_NAME}}UpdatedEvent
  - [ ] 5.3 Create {{AGGREGATE_NAME}}DeletedEvent
{{#each CUSTOM_EVENTS}}
  - [ ] 5.{{@index+4}} Create {{this}}Event
{{/each}}

- [ ] 6. Implement business rules
  - [ ] 6.1 Define invariant validation methods
  - [ ] 6.2 Implement CanCreate{{AGGREGATE_NAME}} rule
  - [ ] 6.3 Implement CanUpdate{{AGGREGATE_NAME}} rule
  - [ ] 6.4 Implement CanDelete{{AGGREGATE_NAME}} rule
{{#each BUSINESS_RULES}}
  - [ ] 6.{{@index+5}} Implement {{this}} rule
{{/each}}

- [ ] 7. Create domain services
  - [ ] 7.1 Define I{{AGGREGATE_NAME}}DomainService interface
  - [ ] 7.2 Implement {{AGGREGATE_NAME}}DomainService
  - [ ] 7.3 Add complex business logic methods
  - [ ] 7.4 Configure dependency injection

- [ ] 8. Implement aggregate methods
  - [ ] 8.1 Add Update method with validation
  - [ ] 8.2 Add methods for managing child entities
  - [ ] 8.3 Add methods for state transitions
  - [ ] 8.4 Ensure all methods maintain invariants
  - [ ] 8.5 Raise appropriate domain events

- [ ] 9. Configure Entity Framework mapping
  - [ ] 9.1 Create {{AGGREGATE_NAME}}Configuration class
  - [ ] 9.2 Configure owned entities and value objects
  - [ ] 9.3 Configure relationships and cascades
  - [ ] 9.4 Configure indexes and constraints
  - [ ] 9.5 Add to DbContext model builder

- [ ] 10. Verify all tests pass
  - [ ] 10.1 Run domain unit tests
  - [ ] 10.2 Verify invariants are enforced
  - [ ] 10.3 Verify events are raised correctly
  - [ ] 10.4 Test all business rules

## Variables Required:
- **AGGREGATE_NAME**: Name of the aggregate root (PascalCase)
- **CHILD_ENTITIES**: Array of child entity names (optional)
- **VALUE_OBJECTS**: Array of value object names (optional)
- **CUSTOM_EVENTS**: Array of custom domain event names (optional)
- **BUSINESS_RULES**: Array of business rule names (optional)

## Example Variables:
```json
{
  "AGGREGATE_NAME": "Order",
  "CHILD_ENTITIES": ["OrderItem", "OrderShipment"],
  "VALUE_OBJECTS": ["Money", "Address", "OrderStatus"],
  "CUSTOM_EVENTS": ["OrderShipped", "OrderCancelled", "PaymentReceived"],
  "BUSINESS_RULES": ["MinimumOrderAmount", "MaximumItemQuantity", "CancellationDeadline"]
}
```