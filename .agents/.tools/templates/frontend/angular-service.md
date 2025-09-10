# Angular Service Template

## Service: {{SERVICE_NAME}}Service

### Parent Task: Implement {{SERVICE_NAME}} Angular Service

#### Subtasks:

- [ ] 1. Write tests for {{SERVICE_NAME}}Service
  - [ ] 1.1 Write service unit tests with Jasmine/Karma
  - [ ] 1.2 Mock HttpClient with HttpClientTestingModule
  - [ ] 1.3 Test each service method
  - [ ] 1.4 Test error handling scenarios
  - [ ] 1.5 Test retry logic and caching if applicable

- [ ] 2. Generate service
  - [ ] 2.1 Run: ng generate service {{SERVICE_PATH}}
  - [ ] 2.2 Create {{SERVICE_NAME:kebab-case}}.service.ts
  - [ ] 2.3 Create {{SERVICE_NAME:kebab-case}}.service.spec.ts
  - [ ] 2.4 Configure providedIn: 'root'

- [ ] 3. Define service interface
  - [ ] 3.1 Create {{MODEL_NAME}}.model.ts interface
  - [ ] 3.2 Define request DTOs if needed
  - [ ] 3.3 Define response DTOs if needed
  - [ ] 3.4 Add TypeScript enums for constants
  - [ ] 3.5 Document interfaces with JSDoc

- [ ] 4. Implement HTTP methods
  - [ ] 4.1 Inject HttpClient in constructor
  - [ ] 4.2 Define base API URL from environment
  - [ ] 4.3 Implement get{{MODEL_NAME}}() method
  - [ ] 4.4 Implement get{{MODEL_NAME}}ById(id) method
  - [ ] 4.5 Implement create{{MODEL_NAME}}(data) method
  - [ ] 4.6 Implement update{{MODEL_NAME}}(id, data) method
  - [ ] 4.7 Implement delete{{MODEL_NAME}}(id) method

- [ ] 5. Add RxJS operators
  - [ ] 5.1 Import required operators (map, catchError, retry, etc.)
  - [ ] 5.2 Add retry logic for failed requests
  - [ ] 5.3 Transform responses with map operator
  - [ ] 5.4 Implement error handling with catchError
  - [ ] 5.5 Add loading state management with tap

- [ ] 6. Implement caching (if applicable)
  - [ ] 6.1 Create cache storage with BehaviorSubject
  - [ ] 6.2 Implement cache invalidation logic
  - [ ] 6.3 Add cache timeout mechanism
  - [ ] 6.4 Provide force refresh option
  - [ ] 6.5 Test cache behavior

- [ ] 7. Add state management
  - [ ] 7.1 Create private BehaviorSubject for state
  - [ ] 7.2 Expose public Observable for components
  - [ ] 7.3 Implement state update methods
  - [ ] 7.4 Add loading and error states
  - [ ] 7.5 Implement optimistic updates if needed

- [ ] 8. Configure HTTP headers
  - [ ] 8.1 Set Content-Type headers
  - [ ] 8.2 Add authentication headers if needed
  - [ ] 8.3 Configure CORS headers if needed
  - [ ] 8.4 Add custom headers for API versioning
  - [ ] 8.5 Implement HTTP interceptor if needed

- [ ] 9. Add utility methods
  - [ ] 9.1 Implement search/filter methods
  - [ ] 9.2 Add pagination support
  - [ ] 9.3 Implement sorting methods
  - [ ] 9.4 Add data transformation utilities
  - [ ] 9.5 Create error message formatters

- [ ] 10. Verify all tests pass
  - [ ] 10.1 Run ng test for service tests
  - [ ] 10.2 Test with real API endpoints
  - [ ] 10.3 Verify error handling works
  - [ ] 10.4 Check memory leaks with subscriptions
  - [ ] 10.5 Run ng lint

## Variables Required:
- **SERVICE_NAME**: Name of service (PascalCase, without 'Service' suffix)
- **SERVICE_PATH**: Path for ng generate (e.g., services/product)
- **MODEL_NAME**: Name of model/entity (e.g., Product)
- **API_ENDPOINT**: Base API endpoint (e.g., /api/products)

## Example Variables:
```json
{
  "SERVICE_NAME": "Product",
  "SERVICE_PATH": "services/product",
  "MODEL_NAME": "Product",
  "API_ENDPOINT": "/api/products"
}
```