# API Endpoint Template

## Endpoint: {{HTTP_METHOD}} {{API_PATH}}

### Parent Task: Implement {{HTTP_METHOD}} {{API_PATH}} endpoint

#### Subtasks:

- [ ] 1. Write tests for {{HTTP_METHOD}} {{API_PATH}}
  - [ ] 1.1 Write unit tests for request validation
  - [ ] 1.2 Write integration tests for endpoint behavior
  - [ ] 1.3 Write tests for error scenarios
  - [ ] 1.4 Write tests for authorization rules

- [ ] 2. Create request/response models
  - [ ] 2.1 Define {{REQUEST_MODEL}} request DTO
  - [ ] 2.2 Define {{RESPONSE_MODEL}} response DTO
  - [ ] 2.3 Add validation attributes to request model
  - [ ] 2.4 Create mapping profiles for AutoMapper

- [ ] 3. Implement MediatR handler
  - [ ] 3.1 Create {{HANDLER_NAME}}{{HANDLER_TYPE}} class
  - [ ] 3.2 Implement business logic in Handle method
  - [ ] 3.3 Add dependency injection for required services
  - [ ] 3.4 Implement error handling and logging

- [ ] 4. Create controller action
  - [ ] 4.1 Add {{ACTION_NAME}} action to {{CONTROLLER_NAME}}Controller
  - [ ] 4.2 Configure route attribute [Http{{HTTP_METHOD:PascalCase}}("{{ROUTE_TEMPLATE}}")]
  - [ ] 4.3 Add authorization attributes if required
  - [ ] 4.4 Implement request validation
  - [ ] 4.5 Call MediatR handler and return response

- [ ] 5. Add OpenAPI documentation
  - [ ] 5.1 Add XML documentation comments
  - [ ] 5.2 Configure ProducesResponseType attributes
  - [ ] 5.3 Add example requests/responses
  - [ ] 5.4 Document error responses

- [ ] 6. Verify all tests pass
  - [ ] 6.1 Run unit tests
  - [ ] 6.2 Run integration tests
  - [ ] 6.3 Test endpoint manually with Swagger
  - [ ] 6.4 Verify response format and status codes

## Variables Required:
- **HTTP_METHOD**: HTTP verb (GET, POST, PUT, DELETE, PATCH)
- **API_PATH**: Full API path (e.g., /api/products/{id})
- **REQUEST_MODEL**: Name of request DTO (e.g., CreateProductRequest)
- **RESPONSE_MODEL**: Name of response DTO (e.g., ProductResponse)
- **HANDLER_NAME**: Name of MediatR handler (e.g., GetProduct)
- **HANDLER_TYPE**: Type of handler (Query or Command)
- **CONTROLLER_NAME**: Name of controller (e.g., Products)
- **ACTION_NAME**: Name of action method (e.g., GetById)
- **ROUTE_TEMPLATE**: Route template for attribute (e.g., {id})