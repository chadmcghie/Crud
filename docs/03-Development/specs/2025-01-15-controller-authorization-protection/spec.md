# Spec Requirements Document

> Spec: Controller Authorization Protection
> Created: 2025-01-15
> GitHub Issue: #177 - Auth Decorators on Controllers
> Status: IN PROGRESS

## Overview

Implement authorization protection on all business API controllers to secure endpoints that are currently publicly accessible. While JWT authentication infrastructure is fully implemented and working, the business controllers (People, Roles, Walls, Windows) lack the `[Authorize]` attributes needed to enforce authentication and authorization policies.

## User Stories

### Secure Business Endpoints

As an authenticated user, I want to access business data (People, Roles, Walls, Windows) only when properly authenticated, so that sensitive information is protected from unauthorized access.

Currently, all business endpoints are publicly accessible without authentication. Users should be required to authenticate and have appropriate roles to access these resources.

### Role-Based Access Control

As an admin user, I want to have elevated permissions for managing business data, so that I can perform administrative operations while regular users have limited access.

The system should enforce role-based access control where Admin users can perform all operations (Create, Read, Update, Delete) while regular Users have read-only access to most resources.

### Consistent Security Model

As a developer, I want all API endpoints to follow the same security model, so that the application has consistent protection across all resources.

All business controllers should use the same authorization patterns as the AuthController, ensuring consistent security enforcement throughout the API.

## Spec Scope

1. **Controller Authorization** - Add `[Authorize]` attributes to all business controllers
2. **Role-Based Policies** - Implement appropriate role-based authorization policies
3. **Endpoint Protection** - Secure all CRUD operations with proper authorization
4. **Testing** - Update integration tests to handle authentication requirements
5. **Documentation** - Update API documentation to reflect authentication requirements

## Out of Scope

- New authentication mechanisms (JWT is already implemented)
- New authorization policies (existing policies are sufficient)
- Frontend changes (Angular already has authentication)
- Database schema changes
- New user management features

## Expected Deliverable

1. All business API endpoints require authentication
2. Role-based access control is enforced on all operations
3. Integration tests pass with proper authentication
4. API documentation reflects authentication requirements
5. Security gap is eliminated (no more publicly accessible business endpoints)

## Implementation Requirements

### Controllers to Secure

- **PeopleController** - All CRUD operations require authentication
- **RolesController** - All CRUD operations require authentication  
- **WallsController** - All CRUD operations require authentication
- **WindowsController** - All CRUD operations require authentication

### Authorization Policies

- **UserOrAdmin** - For read operations (GET endpoints)
- **AdminOnly** - For write operations (POST, PUT, DELETE endpoints)

### Testing Requirements

- Update integration tests to include authentication
- Verify unauthorized access returns 401/403
- Verify authorized access works correctly
- Test both User and Admin role access

## Success Criteria

- [ ] All business controllers have `[Authorize]` attributes
- [ ] Role-based policies are properly applied
- [ ] Integration tests pass with authentication
- [ ] Unauthorized access returns appropriate HTTP status codes
- [ ] Authorized access works for both User and Admin roles
- [ ] API documentation is updated
- [ ] Critical Issues Summary is updated to reflect completion

## Risk Assessment

**Low Risk** - This is a straightforward implementation that adds existing authorization attributes to controllers. The authentication infrastructure is already working and tested.

**Mitigation**: Thorough testing of all endpoints with different user roles to ensure no functionality is broken.

## Dependencies

- JWT Authentication system (✅ Already implemented)
- Authorization policies (✅ Already implemented)
- Integration test framework (✅ Already implemented)
- Angular authentication frontend (✅ Already implemented)
