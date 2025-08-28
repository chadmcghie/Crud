# TypeScript/Angular Style Guide

## Context

TypeScript and Angular-specific coding standards for the CRUD platform.

## TypeScript Conventions

### Type Definitions
- Always use explicit types for function parameters and return values
- Use interfaces for object shapes: `interface User { id: number; name: string; }`
- Use type aliases for union types: `type Status = 'active' | 'inactive' | 'pending'`
- Prefer `unknown` over `any` when type is truly unknown

### Angular-Specific Patterns

#### Components
- Use OnPush change detection strategy when possible
- Implement lifecycle interfaces explicitly: `implements OnInit, OnDestroy`
- Use reactive forms over template-driven forms
- Unsubscribe from observables in `ngOnDestroy`

#### Services
- Make services injectable with `@Injectable({ providedIn: 'root' })`
- Use dependency injection for all external dependencies
- Return observables from HTTP operations
- Handle errors using RxJS operators (`catchError`, `retry`)

#### Observables & RxJS
- Use async pipe in templates when possible
- Prefer `takeUntil` pattern for subscription management
- Use appropriate RxJS operators: `map`, `filter`, `switchMap`, `mergeMap`
- Avoid nested subscriptions

### Code Organization
- Group imports: Angular core, third-party, local
- Use barrel exports (`index.ts`) for feature modules
- Keep components focused on presentation logic
- Move business logic to services
- Use feature modules to organize related functionality

### Error Handling
- Use try-catch blocks for synchronous operations
- Use RxJS `catchError` for asynchronous operations
- Provide meaningful error messages
- Log errors appropriately for debugging

### Testing
- Write unit tests for all services and components
- Use TestBed for Angular component testing
- Mock dependencies using jasmine spies
- Test both success and error scenarios
