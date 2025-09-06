# API Specification

This is the API specification for the spec detailed in @.agent-os/specs/2025-09-06-angular-authentication-frontend/spec.md

## Frontend Integration with Existing API Endpoints

### Authentication Endpoints (Existing Backend)

#### POST /api/auth/register
**Purpose:** Register a new user account
**Request:**
```typescript
interface RegisterRequest {
  email: string;
  password: string;
}
```
**Response:** 
```typescript
interface AuthResponse {
  token: string;
  email: string;
  roles: string[];
  expiresAt: string;
}
```
**Errors:** 400 (validation), 409 (email exists)

#### POST /api/auth/login
**Purpose:** Authenticate existing user
**Request:**
```typescript
interface LoginRequest {
  email: string;
  password: string;
}
```
**Response:** AuthResponse (with Set-Cookie for refresh token)
**Errors:** 401 (invalid credentials), 400 (validation)

#### POST /api/auth/refresh
**Purpose:** Refresh access token using cookie
**Request:** No body (uses HTTP-only cookie)
**Response:** AuthResponse with new token
**Errors:** 401 (invalid/expired refresh token)

#### POST /api/auth/logout
**Purpose:** Invalidate refresh token
**Request:** No body (uses cookie)
**Response:** 200 OK
**Errors:** None (always succeeds)

### Protected Endpoints Integration

All protected endpoints require:
- Header: `Authorization: Bearer [JWT_TOKEN]`
- Automatic retry with refresh on 401
- Error handling for 403 (forbidden)

### Angular HTTP Service Configuration

```typescript
// Environment configuration
export const environment = {
  apiUrl: 'http://localhost:5172',
  production: false
};

// HTTP options for cookie inclusion
const httpOptions = {
  headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
  withCredentials: true  // Include cookies for refresh token
};
```

### Error Response Handling

Standard error format from API:
```typescript
interface ApiError {
  type: string;
  title: string;
  status: number;
  errors?: { [key: string]: string[] };
}
```

Frontend should parse and display user-friendly messages based on error type and status code.