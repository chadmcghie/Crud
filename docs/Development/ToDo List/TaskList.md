# Architectural Roadmap - Priority Order
**Date**: 2025-08-27  
**Status**: Active (Updated)

## Overview
Based on the conformance review, this roadmap prioritizes architectural improvements by impact and dependency relationships.

## ✅ Completed Tasks

### 1. MediatR Registration Fix ✅
**Status**: ✅ **COMPLETED** (2025-08-27)
**Issue**: MediatR only scanning Api assembly instead of App assembly where handlers should live  
**Impact**: CQRS pattern not working, services bypassing command/query pattern  
**Completed Actions**: 
- ✅ Fixed MediatR registration to scan App assembly
- ✅ Implemented command/query handlers for all entities (People, Roles, Walls, Windows)
- ✅ Refactored all controllers to use IMediator pattern
- ✅ Full CQRS implementation with proper separation of concerns

### 2. FluentValidation Implementation ✅ 
**Status**: ✅ **COMPLETED** (2025-08-27)
**Issue**: Packages included but not configured or used  
**Impact**: No request validation, potential security vulnerabilities
**Completed Actions**:
- ✅ Added FluentValidation validators for all DTOs and commands
- ✅ Implemented MediatR validation pipeline behavior
- ✅ Added domain entity validators
- ✅ Created client-side validation with Angular validators
- ✅ Added global error handling middleware
- ✅ Comprehensive validation at all layers (client, API, application, domain)

### 3. API Test Failures Fix ✅
**Status**: ✅ **COMPLETED** (2025-08-27)
**Issue**: 29 API tests failing due to data persistence and timing issues  
**Impact**: Unreliable test suite blocking development confidence  
**Completed Actions**:
- ✅ Fixed transaction completion waits
- ✅ Implemented proper test data isolation
- ✅ Fixed async operation handling in cleanup
- ✅ All tests now passing reliably

## 🚨 Critical Priority (Immediate - This Week)

### 1. Authentication & Authorization (CRITICAL SECURITY GAP)
**Issue**: UseAuthorization without authentication schemes (no-op)  
**Impact**: Security gap, all endpoints public - **PRODUCTION BLOCKER**
**Action**:
- Implement JWT bearer authentication
- Add authorization policies
- Secure endpoints with [Authorize] attributes
- Consider OpenIddict for OIDC if needed

### 2. AutoMapper Configuration
**Issue**: Package installed but not configured  
**Impact**: Manual mapping causing maintenance burden  
**Action**:
- Create AutoMapper profiles in App layer
- Replace manual DTO mapping in controllers
- Configure AutoMapper in dependency injection

## 🔧 High Priority (Next Sprint)

### 3. Proper Database Migration Strategy
**Issue**: Using EnsureCreatedAsync instead of MigrateAsync  
**Impact**: Production deployment issues  
**Action**:
- Replace EnsureCreatedAsync with MigrateAsync
- Add migration rollback strategy
- Configure for production environments

### 4. Serilog Enhancement
**Issue**: Serilog referenced but not configured  
**Impact**: Missing structured logging capabilities  
**Action**:
- Configure Serilog as host logger
- Add appropriate sinks (console, file, Seq)
- Replace default logging

## 📊 Medium Priority (Future Sprints)

### 7. Multi-Tenancy Implementation
**Issue**: Finbuckle.MultiTenant missing, no schema-per-tenant strategy  
**Impact**: Cannot support multiple clients/tenants  
**Action**:
- Integrate Finbuckle.MultiTenant
- Implement tenant resolution (subdomain/header/claim)
- Configure EF model per tenant with schema switching

### 8. Caching Layer
**Issue**: No caching strategy implemented  
**Impact**: Performance bottlenecks, unnecessary database calls  
**Action**:
- Add IMemoryCache for simple caching
- Plan Redis (StackExchange.Redis) for distributed cache
- Implement cache-aside pattern

### 9. Observability & Monitoring
**Issue**: No OpenTelemetry, metrics, or tracing  
**Impact**: Limited production monitoring and debugging  
**Action**:
- Add OpenTelemetry for tracing/metrics
- Export to OTLP/Jaeger/Prometheus
- Implement health checks

## 🔮 Low Priority (Nice to Have)

### 10. Integration Testing Hardening
**Issue**: Missing Testcontainers, incomplete Respawn usage  
**Impact**: Less realistic integration tests  
**Action**:
- Add Testcontainers for database testing
- Implement WireMock.Net for external API mocking
- Enhance test isolation

### 11. CI/CD & AI Integration
**Issue**: ✅ **RESOLVED** - GitHub Actions, Dependabot, and CodeQL now implemented  
**Impact**: Automated CI/CD pipeline with security scanning  
**Completed Actions**:
- ✅ Fixed existing GitHub Actions workflow (pr-validation.yml) with port binding issues resolved
- ✅ Configured Dependabot for .NET, npm, and GitHub Actions updates
- ✅ Added CodeQL security analysis for C# and JavaScript
**Remaining Action**:
- Add AI PR reviewer integration

### 12. API Surface Enhancements
**Issue**: Only REST API, missing modern API patterns  
**Impact**: Limited client integration options  
**Action**:
- Optional: Add GraphQL (HotChocolate)
- Optional: Add OData endpoints
- Consider API versioning strategy

### 13. MAUI E2E Testing
**Issue**: MAUI E2E project is placeholder  
**Impact**: No automated testing for mobile/desktop UI  
**Action**:
- Add UI automation strategy (Appium/.NET MAUI UITest)
- Implement basic smoke tests
- Integrate with CI/CD

## 📋 Implementation Notes

### Dependencies
- **MediatR fix** enables proper CQRS implementation
- **Authentication** should come before multi-tenancy
- **Logging** supports all other monitoring efforts
- **Test fixes** enable confident development

### Risk Mitigation
- Test all changes in development environment first
- Implement feature flags for major architectural changes
- Maintain backward compatibility during transitions
- Document all configuration changes

### Success Metrics
- **MediatR**: Commands/queries processed through handlers
- **Tests**: 0% API test failure rate
- **Validation**: All requests validated, clear error messages
- **Security**: All endpoints properly authenticated/authorized

## 🎯 Current Sprint Focus
1. ~~Fix MediatR registration and add basic handlers~~ ✅ COMPLETED
2. ~~Resolve API test failures for reliable test suite~~ ✅ COMPLETED
3. ~~Implement FluentValidation~~ ✅ COMPLETED
4. **PRIORITY**: Implement Authentication & Authorization (critical security gap - production blocker)
5. Configure AutoMapper profiles to eliminate manual mapping

This roadmap balances immediate technical debt with strategic architectural improvements while maintaining system stability.