# Angular Component Template

## Component: {{COMPONENT_NAME}}Component

### Parent Task: Implement {{COMPONENT_NAME}} Angular Component

#### Subtasks:

- [ ] 1. Write tests for {{COMPONENT_NAME}}Component
  - [ ] 1.1 Write component unit tests with Jasmine/Karma
  - [ ] 1.2 Write tests for component initialization
  - [ ] 1.3 Write tests for user interactions
  - [ ] 1.4 Write tests for service integration
  - [ ] 1.5 Write tests for error handling

- [ ] 2. Generate component structure
  - [ ] 2.1 Run: ng generate component {{COMPONENT_PATH}}
  - [ ] 2.2 Create {{COMPONENT_NAME:kebab-case}}.component.ts
  - [ ] 2.3 Create {{COMPONENT_NAME:kebab-case}}.component.html
  - [ ] 2.4 Create {{COMPONENT_NAME:kebab-case}}.component.scss
  - [ ] 2.5 Create {{COMPONENT_NAME:kebab-case}}.component.spec.ts

- [ ] 3. Implement component TypeScript
  - [ ] 3.1 Define component properties and types
  - [ ] 3.2 Inject required services in constructor
  - [ ] 3.3 Implement OnInit lifecycle hook
  - [ ] 3.4 Add {{#each METHODS}}{{this}}(), {{/each}} methods
  - [ ] 3.5 Implement OnDestroy for cleanup

- [ ] 4. Create component template
  - [ ] 4.1 Design responsive layout with Angular Material
  - [ ] 4.2 Add form controls with [(ngModel)] or reactive forms
  - [ ] 4.3 Implement *ngFor for lists
  - [ ] 4.4 Add *ngIf for conditional rendering
  - [ ] 4.5 Wire up (click) and other event handlers

- [ ] 5. Style component
  - [ ] 5.1 Add component-specific SCSS styles
  - [ ] 5.2 Use Angular Material theming variables
  - [ ] 5.3 Implement responsive breakpoints
  - [ ] 5.4 Add loading and error state styles
  - [ ] 5.5 Ensure accessibility (ARIA labels)

- [ ] 6. Integrate with services
  - [ ] 6.1 Import and inject {{SERVICE_NAME}}Service
  - [ ] 6.2 Subscribe to service observables
  - [ ] 6.3 Handle loading states
  - [ ] 6.4 Implement error handling with catchError
  - [ ] 6.5 Unsubscribe in ngOnDestroy

- [ ] 7. Configure routing
  - [ ] 7.1 Add route to app-routing.module.ts
  - [ ] 7.2 Configure route path: '{{ROUTE_PATH}}'
  - [ ] 7.3 Add route guards if needed
  - [ ] 7.4 Configure route parameters if needed
  - [ ] 7.5 Add navigation links to component

- [ ] 8. Add to module
  - [ ] 8.1 Import component in {{MODULE_NAME}}Module
  - [ ] 8.2 Add to declarations array
  - [ ] 8.3 Import required Angular Material modules
  - [ ] 8.4 Import required third-party modules
  - [ ] 8.5 Export component if needed

- [ ] 9. Implement data binding
  - [ ] 9.1 Set up @Input() properties
  - [ ] 9.2 Set up @Output() event emitters
  - [ ] 9.3 Implement two-way binding if needed
  - [ ] 9.4 Add form validation
  - [ ] 9.5 Display validation errors

- [ ] 10. Verify all tests pass
  - [ ] 10.1 Run ng test for unit tests
  - [ ] 10.2 Fix any failing tests
  - [ ] 10.3 Check code coverage
  - [ ] 10.4 Run ng lint
  - [ ] 10.5 Test component in browser

## Variables Required:
- **COMPONENT_NAME**: Name of component (PascalCase, without 'Component' suffix)
- **COMPONENT_PATH**: Path for ng generate (e.g., features/product-list)
- **SERVICE_NAME**: Name of service to inject (e.g., Product)
- **ROUTE_PATH**: Route path for component (e.g., products)
- **MODULE_NAME**: Module to declare component in (e.g., App or Feature)
- **METHODS**: Array of method names (optional)

## Example Variables:
```json
{
  "COMPONENT_NAME": "ProductList",
  "COMPONENT_PATH": "features/product-list",
  "SERVICE_NAME": "Product",
  "ROUTE_PATH": "products",
  "MODULE_NAME": "Products",
  "METHODS": ["loadProducts", "deleteProduct", "editProduct", "viewDetails"]
}
```