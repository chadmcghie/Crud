# CRUD Feature Template

## Entity: {{ENTITY_NAME}}

### Parent Task: Implement {{ENTITY_NAME}} CRUD Operations

#### Subtasks:

- [ ] 1. Write tests for {{ENTITY_NAME}} CRUD operations
  - [ ] 1.1 Write unit tests for {{ENTITY_NAME}} domain entity
  - [ ] 1.2 Write integration tests for {{ENTITY_NAME}} repository
  - [ ] 1.3 Write integration tests for {{ENTITY_NAME}} API endpoints
  - [ ] 1.4 Write E2E tests for {{ENTITY_NAME}} UI operations

- [ ] 2. Create {{ENTITY_NAME}} domain entity
  - [ ] 2.1 Define {{ENTITY_NAME}} entity in Domain layer
  - [ ] 2.2 Add validation rules for {{ENTITY_NAME}}
  - [ ] 2.3 Create {{ENTITY_NAME}}Id value object
  - [ ] 2.4 Define domain events for {{ENTITY_NAME}}

- [ ] 3. Implement {{ENTITY_NAME}} repository
  - [ ] 3.1 Define I{{ENTITY_NAME}}Repository interface in Domain
  - [ ] 3.2 Implement {{ENTITY_NAME}}Repository in Infrastructure
  - [ ] 3.3 Configure Entity Framework mapping for {{ENTITY_NAME}}
  - [ ] 3.4 Add database migration for {{ENTITY_NAME}} table

- [ ] 4. Create {{ENTITY_NAME}} application services
  - [ ] 4.1 Create Create{{ENTITY_NAME}}Command and handler
  - [ ] 4.2 Create Update{{ENTITY_NAME}}Command and handler
  - [ ] 4.3 Create Delete{{ENTITY_NAME}}Command and handler
  - [ ] 4.4 Create Get{{ENTITY_NAME}}Query and handler
  - [ ] 4.5 Create Get{{ENTITY_NAME}}ListQuery and handler
  - [ ] 4.6 Add {{ENTITY_NAME}}Dto and mapping profiles

- [ ] 5. Implement {{ENTITY_NAME}} API endpoints
  - [ ] 5.1 Create {{ENTITY_NAME}}Controller
  - [ ] 5.2 Implement POST /api/{{ENTITY_NAME:kebab-case}}
  - [ ] 5.3 Implement GET /api/{{ENTITY_NAME:kebab-case}}/{id}
  - [ ] 5.4 Implement GET /api/{{ENTITY_NAME:kebab-case}}
  - [ ] 5.5 Implement PUT /api/{{ENTITY_NAME:kebab-case}}/{id}
  - [ ] 5.6 Implement DELETE /api/{{ENTITY_NAME:kebab-case}}/{id}

- [ ] 6. Create {{ENTITY_NAME}} Angular components
  - [ ] 6.1 Create {{ENTITY_NAME:kebab-case}}-list component
  - [ ] 6.2 Create {{ENTITY_NAME:kebab-case}}-detail component
  - [ ] 6.3 Create {{ENTITY_NAME:kebab-case}}-form component
  - [ ] 6.4 Create {{ENTITY_NAME:camelCase}}.service.ts
  - [ ] 6.5 Add routing for {{ENTITY_NAME}} pages
  - [ ] 6.6 Integrate with Angular Material components

- [ ] 7. Verify all tests pass
  - [ ] 7.1 Run backend unit tests
  - [ ] 7.2 Run backend integration tests
  - [ ] 7.3 Run Angular tests
  - [ ] 7.4 Run E2E tests
  - [ ] 7.5 Fix any failing tests

## Variables Required:
- **ENTITY_NAME**: The name of the entity (PascalCase)

## Case Transformations:
- **{{ENTITY_NAME}}**: PascalCase (e.g., ProductCategory)
- **{{ENTITY_NAME:camelCase}}**: camelCase (e.g., productCategory)
- **{{ENTITY_NAME:snake_case}}**: snake_case (e.g., product_category)
- **{{ENTITY_NAME:kebab-case}}**: kebab-case (e.g., product-category)