# Angular State Management Template

## State: {{STATE_NAME}}State

### Parent Task: Implement {{STATE_NAME}} State Management

#### Subtasks:

- [ ] 1. Write tests for {{STATE_NAME}} state management
  - [ ] 1.1 Write tests for actions
  - [ ] 1.2 Write tests for reducers/effects
  - [ ] 1.3 Write tests for selectors
  - [ ] 1.4 Write integration tests for state flow
  - [ ] 1.5 Test async operations and side effects

- [ ] 2. Define state interface
  - [ ] 2.1 Create {{STATE_NAME:kebab-case}}.state.ts
  - [ ] 2.2 Define {{STATE_NAME}}State interface
  - [ ] 2.3 Define initial state constant
  - [ ] 2.4 Add loading and error properties
  - [ ] 2.5 Document state properties

- [ ] 3. Create actions
  - [ ] 3.1 Create {{STATE_NAME:kebab-case}}.actions.ts
  - [ ] 3.2 Define Load{{STATE_NAME}} action
  - [ ] 3.3 Define Load{{STATE_NAME}}Success action
  - [ ] 3.4 Define Load{{STATE_NAME}}Failure action
  - [ ] 3.5 Define Create{{STATE_NAME}} action
  - [ ] 3.6 Define Update{{STATE_NAME}} action
  - [ ] 3.7 Define Delete{{STATE_NAME}} action
  - [ ] 3.8 Add action creators with props

- [ ] 4. Implement reducers (NgRx) or state service (Akita/custom)
  - [ ] 4.1 Create {{STATE_NAME:kebab-case}}.reducer.ts
  - [ ] 4.2 Implement reducer function with createReducer
  - [ ] 4.3 Handle each action with on() functions
  - [ ] 4.4 Update state immutably
  - [ ] 4.5 Reset state on logout/clear action

- [ ] 5. Create effects (if using NgRx)
  - [ ] 5.1 Create {{STATE_NAME:kebab-case}}.effects.ts
  - [ ] 5.2 Inject Actions and services
  - [ ] 5.3 Implement load{{STATE_NAME}}$ effect
  - [ ] 5.4 Implement create{{STATE_NAME}}$ effect
  - [ ] 5.5 Implement update{{STATE_NAME}}$ effect
  - [ ] 5.6 Implement delete{{STATE_NAME}}$ effect
  - [ ] 5.7 Handle errors and dispatch failure actions

- [ ] 6. Define selectors
  - [ ] 6.1 Create {{STATE_NAME:kebab-case}}.selectors.ts
  - [ ] 6.2 Create feature selector
  - [ ] 6.3 Create select{{STATE_NAME}}State selector
  - [ ] 6.4 Create select{{STATE_NAME}}List selector
  - [ ] 6.5 Create select{{STATE_NAME}}ById selector
  - [ ] 6.6 Create selectLoading selector
  - [ ] 6.7 Create selectError selector
  - [ ] 6.8 Add memoized computed selectors

- [ ] 7. Create facade service (optional)
  - [ ] 7.1 Create {{STATE_NAME:kebab-case}}.facade.ts
  - [ ] 7.2 Inject Store in constructor
  - [ ] 7.3 Expose state observables
  - [ ] 7.4 Create dispatch methods for actions
  - [ ] 7.5 Add convenience methods

- [ ] 8. Register in module
  - [ ] 8.1 Import StoreModule in feature module
  - [ ] 8.2 Register reducer with StoreModule.forFeature
  - [ ] 8.3 Import EffectsModule
  - [ ] 8.4 Register effects with EffectsModule.forFeature
  - [ ] 8.5 Provide facade service if created

- [ ] 9. Integrate with components
  - [ ] 9.1 Inject Store or facade in components
  - [ ] 9.2 Select state with selectors
  - [ ] 9.3 Dispatch actions on user interactions
  - [ ] 9.4 Subscribe to state changes
  - [ ] 9.5 Handle loading and error states in UI

- [ ] 10. Add DevTools support
  - [ ] 10.1 Configure Redux DevTools
  - [ ] 10.2 Add action sanitization if needed
  - [ ] 10.3 Configure state sanitization
  - [ ] 10.4 Test time-travel debugging
  - [ ] 10.5 Document state structure

- [ ] 11. Verify all tests pass
  - [ ] 11.1 Run state management tests
  - [ ] 11.2 Test action dispatching
  - [ ] 11.3 Test selector memoization
  - [ ] 11.4 Test effects with marble testing
  - [ ] 11.5 Verify no memory leaks

## Variables Required:
- **STATE_NAME**: Name of state slice (PascalCase)
- **ENTITY_NAME**: Name of entity being managed (e.g., Product)
- **STATE_MANAGEMENT_LIB**: Library used (NgRx, Akita, or custom)

## Example Variables:
```json
{
  "STATE_NAME": "Product",
  "ENTITY_NAME": "Product",
  "STATE_MANAGEMENT_LIB": "NgRx"
}
```

## State Structure Example:
```typescript
interface {{STATE_NAME}}State {
  entities: {{ENTITY_NAME}}[];
  selectedId: string | null;
  loading: boolean;
  error: string | null;
  filters: FilterOptions;
  pagination: PaginationOptions;
}
```