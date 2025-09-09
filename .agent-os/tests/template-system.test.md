# Template System Tests

## Test Suite: Template System Functionality

### Test 1: Directory Structure Creation
**Description**: Verify that the `.agent-os/templates/` directory structure is created correctly
**Expected**:
- `.agent-os/templates/` directory exists
- Subdirectories exist: `backend/`, `frontend/`, `common/`
**Test Steps**:
1. Check if `.agent-os/templates/` exists
2. Verify subdirectory structure
3. Confirm proper permissions

### Test 2: Template File Loading
**Description**: Verify that template files can be loaded and read correctly
**Expected**:
- Template files are readable
- Template content is preserved exactly
- File encoding is UTF-8
**Test Steps**:
1. Load each template file
2. Verify content integrity
3. Check encoding

### Test 3: Variable Substitution - Basic
**Description**: Test basic variable substitution in templates
**Input**: Template with `{{ENTITY_NAME}}` placeholder
**Variables**: `{ "ENTITY_NAME": "Product" }`
**Expected**: All instances of `{{ENTITY_NAME}}` replaced with "Product"
**Test Steps**:
1. Load template with variables
2. Apply substitution
3. Verify all placeholders replaced

### Test 4: Variable Substitution - Multiple Variables
**Description**: Test multiple variable substitutions in a single template
**Input**: Template with `{{ENTITY_NAME}}`, `{{API_PATH}}`, `{{TABLE_NAME}}`
**Variables**: 
```json
{
  "ENTITY_NAME": "Product",
  "API_PATH": "/api/products",
  "TABLE_NAME": "Products"
}
```
**Expected**: All placeholders correctly replaced
**Test Steps**:
1. Load template with multiple variables
2. Apply all substitutions
3. Verify complete replacement

### Test 5: Variable Substitution - Case Transformations
**Description**: Test variable transformations (PascalCase, camelCase, snake_case, kebab-case)
**Input**: Template with `{{ENTITY_NAME}}`, `{{ENTITY_NAME:camelCase}}`, `{{ENTITY_NAME:snake_case}}`, `{{ENTITY_NAME:kebab-case}}`
**Variables**: `{ "ENTITY_NAME": "ProductCategory" }`
**Expected**:
- `{{ENTITY_NAME}}` → "ProductCategory"
- `{{ENTITY_NAME:camelCase}}` → "productCategory"
- `{{ENTITY_NAME:snake_case}}` → "product_category"
- `{{ENTITY_NAME:kebab-case}}` → "product-category"
**Test Steps**:
1. Load template with case transformations
2. Apply transformations
3. Verify all cases correct

### Test 6: Template Validation
**Description**: Verify template validation catches errors
**Test Cases**:
- Missing required variables
- Invalid variable names
- Unclosed placeholders
- Nested placeholders
**Expected**: Clear error messages for each validation failure
**Test Steps**:
1. Test each error condition
2. Verify appropriate error message
3. Ensure no partial substitution

### Test 7: Backend Template - CRUD Feature
**Description**: Test the crud-feature.md template generates correct task structure
**Input**: CRUD feature template for "Product" entity
**Expected**:
- Domain entity tasks created
- Repository tasks created
- Service/Handler tasks created
- API endpoint tasks created
- Test tasks included
**Test Steps**:
1. Load crud-feature.md template
2. Apply Product entity variables
3. Verify all CRUD operations included

### Test 8: Backend Template - API Endpoint
**Description**: Test the api-endpoint.md template
**Input**: API endpoint template for GET /api/products
**Expected**:
- Controller action task
- Request/Response DTO tasks
- Validation task
- Integration test task
**Test Steps**:
1. Load api-endpoint.md template
2. Apply endpoint variables
3. Verify proper REST conventions

### Test 9: Backend Template - Domain Aggregate
**Description**: Test the domain-aggregate.md template
**Input**: Domain aggregate template for "Order" with "OrderItem" children
**Expected**:
- Aggregate root entity task
- Child entity tasks
- Value objects tasks
- Domain events tasks
- Invariant rules tasks
**Test Steps**:
1. Load domain-aggregate.md template
2. Apply aggregate variables
3. Verify DDD patterns followed

### Test 10: Frontend Template - Angular Component
**Description**: Test the angular-component.md template
**Input**: Angular component template for "ProductList"
**Expected**:
- Component TypeScript task
- Component HTML template task
- Component styles task
- Component spec test task
- Service integration task
**Test Steps**:
1. Load angular-component.md template
2. Apply component variables
3. Verify Angular best practices

### Test 11: Frontend Template - Angular Service
**Description**: Test the angular-service.md template
**Input**: Angular service template for "ProductService"
**Expected**:
- Service class task
- HTTP methods tasks
- RxJS operators task
- Service spec test task
**Test Steps**:
1. Load angular-service.md template
2. Apply service variables
3. Verify dependency injection setup

### Test 12: Frontend Template - Angular State
**Description**: Test the angular-state.md template
**Input**: State management template for "ProductState"
**Expected**:
- State interface task
- Actions tasks
- Reducers/Effects tasks
- Selectors tasks
- State tests task
**Test Steps**:
1. Load angular-state.md template
2. Apply state variables
3. Verify state management pattern

### Test 13: Template Composition
**Description**: Test combining multiple templates
**Input**: CRUD feature requiring both backend and frontend templates
**Expected**:
- Templates can be composed
- No variable conflicts
- Proper task ordering
**Test Steps**:
1. Load multiple templates
2. Compose into single task list
3. Verify coherent task structure

### Test 14: Template Inheritance
**Description**: Test template inheritance/extension
**Input**: Base template extended by specific template
**Expected**:
- Base template tasks included
- Extended tasks added
- Overrides applied correctly
**Test Steps**:
1. Load base template
2. Apply extension
3. Verify inheritance chain

### Test 15: Integration with create-tasks.md
**Description**: Test that create-tasks.md can use templates
**Input**: Request to create tasks using template
**Expected**:
- Template selection works
- Variables prompted for
- Tasks generated correctly
**Test Steps**:
1. Invoke create-tasks with template
2. Provide required variables
3. Verify tasks.md updated

### Test 16: Error Recovery
**Description**: Test graceful error handling
**Test Cases**:
- Template file not found
- Invalid template syntax
- Runtime substitution errors
**Expected**: Clear error messages and no corruption
**Test Steps**:
1. Test each error scenario
2. Verify error messages helpful
3. Ensure no side effects

### Test 17: Performance - Large Templates
**Description**: Test performance with large templates
**Input**: Template with 100+ variables and 500+ lines
**Expected**: 
- Substitution completes in < 1 second
- Memory usage reasonable
**Test Steps**:
1. Create large template
2. Measure substitution time
3. Monitor memory usage

### Test 18: Special Characters Handling
**Description**: Test handling of special characters in variables
**Input**: Variables containing quotes, backslashes, newlines
**Expected**: Proper escaping and preservation
**Test Steps**:
1. Test various special characters
2. Verify proper escaping
3. Ensure no injection issues

## Test Execution Plan

### Phase 1: Unit Tests
- Tests 1-6: Core functionality
- Tests 7-12: Individual templates
- Run after each implementation step

### Phase 2: Integration Tests  
- Tests 13-15: Template composition and integration
- Test 16: Error scenarios
- Run after core implementation complete

### Phase 3: Performance Tests
- Test 17: Large template handling
- Test 18: Special character edge cases
- Run as final validation

## Success Criteria
- All 18 tests passing
- No template substitution errors
- Templates generate valid, compilable code
- Integration with existing Agent OS tools confirmed