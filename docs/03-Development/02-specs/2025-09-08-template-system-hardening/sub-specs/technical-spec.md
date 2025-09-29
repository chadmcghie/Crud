# Technical Specification: Template System Hardening

## Architecture Overview

The template system hardening builds upon the existing template engine to provide enterprise-grade code generation capabilities with full type safety and production patterns.

## Component Design

### Type System Mapper
```javascript
class TypeMapper {
  // Bidirectional type mapping
  mapCSharpToTypeScript(type) { }
  mapTypeScriptToCSharp(type) { }
  
  // Generic type handling
  mapGenericType(baseType, typeParams) { }
  
  // Custom converters
  registerCustomMapping(from, to, converter) { }
}
```

### Migration Template Engine
```javascript
class MigrationTemplateEngine {
  // Generate EF Core migrations
  createTableMigration(entity) { }
  addIndexMigration(table, columns) { }
  addRelationshipMigration(from, to, type) { }
  
  // Rollback support
  generateDownMigration(upMigration) { }
}
```

### Test Generator
```javascript
class TestGenerator {
  // Framework-specific generators
  generateXUnitTest(component) { }
  generateJasmineTest(component) { }
  generatePlaywrightTest(flow) { }
  
  // Test data builders
  createTestDataBuilder(entity) { }
}
```

## Type Mappings

### Core Type Mappings
| C# Type | TypeScript Type | Notes |
|---------|----------------|-------|
| int, long | number | |
| decimal | number | Consider precision |
| string | string | |
| bool | boolean | |
| DateTime | Date | |
| Guid | string | UUID format |
| byte[] | Uint8Array | |
| List<T> | T[] | |
| Dictionary<K,V> | Record<K, V> | |
| IEnumerable<T> | T[] | |
| Task<T> | Promise<T> | |
| T? | T \| null | Nullable handling |

### Complex Type Handling
- Enums: Generate TypeScript enums with same values
- Classes: Map to interfaces or classes based on usage
- Generics: Preserve type parameters
- Nullable reference types: Map to optional properties

## Template Categories

### Migration Templates
1. **Table Creation**
   - Entity to table mapping
   - Column definitions
   - Primary keys
   - Indexes

2. **Relationships**
   - Foreign keys
   - Many-to-many join tables
   - Self-referential
   - Polymorphic associations

3. **Data Operations**
   - Seed data
   - Data migrations
   - Bulk operations

### Test Templates
1. **Unit Tests**
   - Arrange-Act-Assert pattern
   - Mock generation
   - Test data builders
   - Assertion helpers

2. **Integration Tests**
   - WebApplicationFactory setup
   - Database fixtures
   - API testing
   - Transaction rollback

3. **E2E Tests**
   - Page object models
   - User flow tests
   - Data setup/teardown
   - Screenshot on failure

### Validation Templates
1. **Backend Validation**
   - FluentValidation rules
   - Custom validators
   - Async validation
   - Conditional rules

2. **Frontend Validation**
   - Reactive form validators
   - Custom validators
   - Cross-field validation
   - Async validators

### Performance Templates
1. **Data Access**
   - Pagination with cursor/offset
   - Projection to DTOs
   - Include strategies
   - Compiled queries

2. **Caching**
   - Memory cache
   - Distributed cache
   - Cache invalidation
   - Cache-aside pattern

3. **Bulk Operations**
   - Batch inserts
   - Bulk updates
   - Bulk deletes
   - Transaction batching

### Security Templates
1. **Authorization**
   - Policy-based
   - Role-based
   - Claims-based
   - Resource-based

2. **Authentication**
   - JWT generation
   - Token refresh
   - API keys
   - OAuth integration

## Implementation Approach

### Phase 1: Core Infrastructure
1. Extend template engine for complex scenarios
2. Implement type mapping system
3. Create template validation framework

### Phase 2: Template Implementation
1. Build migration templates
2. Create test generation templates
3. Implement validation templates

### Phase 3: Advanced Patterns
1. Add performance patterns
2. Implement security templates
3. Create MediatR patterns

### Phase 4: Testing & Hardening
1. Integration test suite
2. Performance benchmarks
3. Security audit
4. Documentation

## Quality Assurance

### Testing Strategy
- Unit tests for each template component
- Integration tests for template composition
- Compilation tests for generated code
- Runtime tests for functionality
- Performance benchmarks

### Validation Rules
- All generated code must compile
- Type mappings must be bidirectional
- Generated tests must pass
- Performance targets must be met
- Security patterns must be validated

## Performance Targets
- Template generation: < 100ms per file
- Type mapping: < 10ms per type
- Test generation: < 500ms per test suite
- Migration generation: < 200ms per entity

## Security Considerations
- No sensitive data in templates
- Parameterized queries in data access
- Input validation on all endpoints
- Authorization on all operations
- Secure defaults for all patterns

## Dependencies
- Existing template engine
- Template files in .agents/.agent-os/templates/
- Type definition files
- Framework-specific libraries

## Success Metrics
- 100% of templates generate valid code
- All generated tests pass
- Type safety maintained across languages
- Performance benchmarks met
- Security audit passed