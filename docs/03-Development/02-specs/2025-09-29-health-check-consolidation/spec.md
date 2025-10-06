# Spec Requirements Document

> Spec: Health Check Consolidation
> Created: 2025-09-29
> GitHub Issue: #262 - Consolidate Health Check Systems

## Overview

Consolidate three separate health check implementations (HealthController, ApiHealthController, and ASP.NET Core Health Checks middleware) into a single standardized system following industry best practices for liveness and readiness probes. This refactoring will eliminate duplicate code, reduce maintenance burden, and provide a clearer separation of concerns between different health check purposes.

## User Stories

### Operations Team - Kubernetes Health Monitoring

As an operations engineer, I want standardized health check endpoints that follow Kubernetes conventions (liveness and readiness probes), so that I can reliably monitor application health and configure automatic pod restarts when needed.

The ops team needs to configure Kubernetes deployment manifests with health check endpoints. Currently, there are 5 different endpoints (/health, /health/quick, /api/health, /health/system, /health/ready) with unclear purposes and overlapping functionality. The consolidation will provide clear, documented endpoints: /health for liveness (is the app alive?), /health/ready for readiness (is the app ready for traffic?), and /health/detailed for troubleshooting.

### Development Team - Integration Testing

As a developer writing integration tests, I want a single, well-defined health endpoint that provides consistent responses across all environments, so that my tests don't break due to health check endpoint changes or inconsistencies.

Currently, integration tests hit /health (HealthController) while CI/CD and Playwright use /health/ready (middleware). Having multiple implementations with different behaviors creates confusion and fragile tests. After consolidation, all tests will use the same ASP.NET Core health check infrastructure with consistent response formats.

### Support Team - Production Troubleshooting

As a support engineer, I want a detailed health endpoint that shows comprehensive system status (database, cache, dependencies), so that I can quickly diagnose production issues without needing to restart services.

The current /api/health endpoint provides detailed info but is implemented as a separate custom controller with duplicate logic. The consolidated system will provide /health/detailed with the same comprehensive information but backed by the standard ASP.NET Core health check framework, making it more maintainable and extensible.

## Spec Scope

1. **Remove Duplicate Controllers** - Delete HealthController.cs and ApiHealthController.cs to eliminate code duplication
2. **Standardize on ASP.NET Core Health Checks** - Use built-in middleware with proper tag-based filtering for liveness/readiness separation
3. **Configure Three Endpoints** - Map /health (liveness), /health/ready (readiness), /health/detailed (verbose diagnostics)
4. **Update Integration Tests** - Modify tests to use new endpoint structure without breaking existing functionality
5. **Add Health Check Documentation** - Document endpoint purposes, response formats, and usage examples for operations and development teams

## Out of Scope

- Adding new health check implementations (e.g., Redis, external API checks)
- Changing the DatabaseHealthCheck logic or warm-up behavior
- Modifying Playwright configuration (already uses /health/ready)
- Adding health check UI or dashboard

## Expected Deliverable

1. **Single Health Check System** - Only ASP.NET Core Health Checks middleware in use, with custom controllers removed
2. **Three Clear Endpoints** - /health (liveness), /health/ready (readiness), /health/detailed (diagnostics) all returning appropriate responses
3. **Passing Tests** - All integration tests updated and passing with new endpoint structure (445/446 tests still passing)