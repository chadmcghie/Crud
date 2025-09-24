# Technical Specification

This is the technical specification for the spec detailed in [spec.md](../spec.md)

## Technical Requirements

### Angular Service Layer
- **AuthService**: Core authentication service using HttpClient and RxJS
  - Login method returning Observable<AuthResponse>
  - Register method with user creation
  - Logout method clearing tokens and state
  - Token refresh logic with automatic retry
  - Current user BehaviorSubject for reactive state
  - IsAuthenticated and role checking methods

### Component Layer
- **LoginComponent**: Reactive form with email/password fields
  - Form validation (required, email format)
  - Error message display
  - Loading state during authentication
  - Redirect to return URL after success

- **RegisterComponent**: User registration form
  - Email and password fields with validation
  - Password confirmation with match validator
  - Terms acceptance checkbox
  - Success message and auto-login flow

### Security Infrastructure
- **AuthGuard**: CanActivate guard for route protection
  - Check authentication status
  - Redirect to login with return URL
  - Role-based authorization checks

- **AuthInterceptor**: HTTP interceptor for API calls
  - Attach JWT Bearer token to requests
  - Handle 401 responses
  - Trigger token refresh on expiration
  - Queue requests during refresh

### State Management
- **Token Storage**: Secure token management
  - Store access token in memory/session storage
  - Rely on HTTP-only cookie for refresh token
  - Clear tokens on logout
  
- **User Context**: Application-wide user state
  - Current user observable
  - Role-based permissions
  - Persist user info across refreshes

### Integration Requirements
- API base URL configuration (http://localhost:5172)
- CORS handling for cross-origin requests
- Error handling with user-friendly messages
- Loading indicators during async operations

### Performance Criteria
- Login/register response within 2 seconds
- Token refresh transparent to user
- Immediate UI updates on auth state change
- Minimal bundle size impact (<50KB added)