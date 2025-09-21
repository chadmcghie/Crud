# Technical Specification

This is the technical specification for the spec detailed in @docs/03-Development/specs/2025-09-20-multi-config-e2e-testing/spec.md

## Technical Requirements

### Industry Best Practice Implementation: Testing Pyramid + Configuration Strategy

#### Tier 1: Configuration Validation Tests (Fastest - All Configs)
- **Configuration Schema Validation** - Validate all appsettings.*.json files against schema without loading application
- **Dependency Injection Validation** - Test that all services can be resolved for each environment configuration
- **Connection String Format Validation** - Verify connection string formats without actual database connections
- **Health Check Configuration** - Validate health check endpoints are properly configured for each environment
- **Middleware Pipeline Validation** - Verify middleware registration order and configuration per environment

#### Tier 2: Enhanced Integration Tests (Medium - Multiple Database Providers)
- **Repository Behavior Testing** - Run existing integration tests across SQLite, InMemory, and SqlServer providers
- **Entity Framework Provider Differences** - Test query behavior, transaction handling, and migration compatibility
- **Configuration-Dependent Service Testing** - Test services that behave differently based on configuration (caching, logging, external services)
- **Database Migration Validation** - Verify migrations work correctly across different providers

#### Tier 3: Smoke Tests Per Configuration (Fast - Critical Endpoints Only)
- **Health Endpoint Validation** - GET /health returns 200 with correct environment information
- **API Health Validation** - GET /api/health validates database connectivity and core services
- **Authentication Endpoint Testing** - POST /api/auth/login basic functionality validation
- **Configuration Information Endpoint** - Verify environment-specific settings are correctly loaded
- **External Service Connectivity** - Validate external service configurations (mocked in non-production)

#### Tier 4: E2E Tests (Slowest - Testing Configuration Only)
- **Maintain Current Approach** - Keep E2E tests on Testing configuration (SQLite) for comprehensive user journey validation
- **No Multi-Config E2E** - Avoid the historical blocking issue pattern by keeping E2E tests environment-agnostic

### Primary Implementation: Hybrid Approach with Immediate Wins

#### Phase 1: Configuration Validation Tests (IMMEDIATE WIN - Week 1-2)
- **Extend Tests.Integration.Backend Project** - Add configuration loading tests to existing test infrastructure
- **DI Container Validation** - Test service resolution across Development, Testing, Staging, Production configurations using existing TestWebApplicationFactory pattern
- **Connection String Format Validation** - Verify formats without actual connections using existing configuration system
- **Configuration Schema Validation** - JSON schema validation of appsettings files using System.Text.Json (already in project)
- **Health Check Configuration Testing** - Validate health check registration per environment

#### Phase 2: Database Provider Matrix Testing (HIGH ROI - Week 2-3)
- **Extend Existing Integration Tests** - Add provider switching to current Tests.Integration.Backend using [Theory] and [InlineData] patterns
- **Repository Behavior Validation** - Run existing repository tests across SQLite, InMemory, SqlServer providers
- **EF Core Provider Differences** - Test query translation, transaction behavior, migration compatibility
- **SqliteTestWebApplicationFactory Enhancement** - Extend current factory pattern to support provider switching

#### Phase 3: Simple Smoke Tests (VALIDATION LAYER - Week 4)
- **Health Endpoint Testing** - GET /health validation per configuration (30 seconds each)
- **API Health Validation** - GET /api/health with database connectivity testing per provider
- **Authentication Endpoint Testing** - Basic auth flow validation across configurations
- **Deployment Gate Integration** - Add to existing CI/CD workflows as lightweight validation step

### Industry Pattern Research & Documentation (DISCOVERY TASK)

**Research Position**: Use hybrid approach as baseline while exploring industry patterns for comparison

#### Microsoft (.NET Teams) Pattern Analysis
- **Research Approach** - Document how .NET teams at Microsoft handle configuration testing **compared to our hybrid approach**
- **ASP.NET Core Testing Patterns** - Analyze official Microsoft testing guidance **to validate or improve our configuration validation approach**
- **Entity Framework Testing Strategies** - Research EF Core team's provider-agnostic testing **to enhance our provider matrix testing**

#### Google (Go/Cloud) Pattern Analysis
- **Hermetic Testing Research** - Document Google's isolated test environment approach **for potential integration with our existing infrastructure**
- **Configuration as Code Patterns** - Research Google's infrastructure testing **to evaluate against our configuration validation tests**
- **Service Mesh Testing** - Analyze configuration difference testing **for applicability to our middleware pipeline validation**

#### Netflix (Java/Spring) Pattern Analysis
- **Spring Profile Testing Research** - Document Netflix's configuration testing **compared to our appsettings validation approach**
- **Chaos Engineering Integration** - Research production configuration testing **to evaluate risk vs our safe validation approach**
- **Circuit Breaker Configuration Testing** - Analyze resilience testing **for potential integration with our smoke tests**

**Research Outcome Criteria**: Industry patterns must demonstrate clear superiority over hybrid approach in coverage, risk, or alignment with existing infrastructure to replace the fallback position.

## Historical Blocking Issues & Risk Assessment

**⚠️ CRITICAL**: This spec addresses a recurring pattern that has caused multiple blocking issues:

### Previous Blocking Issues with Similar Root Causes

#### BI-2025-09-11-002: ConditionalRequestMiddleware ETag Comparison Logic Failures
- **Pattern**: "Test environment configuration preventing execution (`OutputCaching:Disabled = true`)"
- **Root Cause**: Configuration differences between environments broke functionality
- **Lesson**: "Test environment configuration can prevent functionality from executing entirely"
- **Relevance**: Multi-config testing risks creating the same environment-specific failures

#### BI-2025-09-10-001: E2E Test Failures in Staging Deployment Pipeline
- **Pattern**: "Local development environment doesn't replicate staging security constraints and performance characteristics"
- **Symptoms**: Tests work locally but fail in CI/staging due to config differences
- **Root Cause**: Environment configuration differences, timeout issues, security constraints
- **Relevance**: Attempting to run E2E tests on production configs risks identical failures

### Risk Pattern Recognition
**Consistent Pattern**:
1. Tests work in one environment (local/Testing config)
2. Tests fail in other environments (staging/production configs)
3. Root cause is always configuration differences between environments
4. Solutions require environment-specific overrides and compatibility layers

**⚠️ Implementation Warning**: This spec intentionally addresses the gap these issues revealed, but risks creating BI-2025-09-20-XXX if not carefully designed to avoid historical failure patterns.

## Architectural Decision Records (ADRs) Compliance

This implementation must comply with existing architectural decisions:

### ADR-001: Serial E2E Testing
- **Constraint**: All E2E tests must run serially (`workers: 1`) due to SQLite single-writer limitations
- **Impact**: Multi-configuration testing will execute configurations sequentially, not in parallel
- **Implementation**: Configuration matrix will run one config at a time to respect serial execution requirement

### ADR-0005: CI/CD Dependency Management Strategy
- **Principle**: "Workflow independence" and "explicit dependencies"
- **Impact**: Each configuration test job must explicitly install all required dependencies
- **Implementation**: Configuration-specific test jobs will not assume dependencies from previous jobs

### ADR-0003: E2E Testing Database Use Playwright's webServer
- **Current**: Tests use Playwright's built-in webServer configuration for server management
- **Impact**: Multi-config testing must extend webServer config to support different environment settings
- **Implementation**: Create multiple webServer configurations for different environment profiles

## Implementation Strategy & Timeline

### Phase 1: Research & Foundation (Week 1)
- **Industry Pattern Research** - Document Microsoft, Google, Netflix approaches to configuration testing
- **Historical Risk Analysis** - Deep analysis of BI-2025-09-11-002 and BI-2025-09-10-001 patterns
- **Configuration Audit** - Complete analysis of current appsettings differences across environments

### Phase 2: Configuration Validation Tests (Week 2)
- **Schema Validation Implementation** - JSON schema validation for all appsettings files
- **DI Validation Tests** - Service resolution validation across configurations
- **Health Check Validation** - Endpoint configuration testing per environment

### Phase 3: Enhanced Integration Tests (Week 3)
- **Database Provider Matrix** - Extend integration tests to run across SQLite/InMemory/SqlServer
- **Configuration-Dependent Services** - Test services with environment-specific behavior
- **Migration Compatibility** - Validate EF migrations across providers

### Phase 4: Smoke Tests Implementation (Week 4)
- **Endpoint Smoke Tests** - Fast validation of critical endpoints per configuration
- **Authentication Flow Testing** - Basic auth validation across environments
- **External Service Connectivity** - Mock-based external service testing

### Phase 5: CI/CD Integration (Week 5)
- **Workflow Integration** - Add pyramid tests to existing CI/CD workflows
- **Performance Optimization** - Ensure new tests don't slow down feedback loops
- **Documentation & Guidelines** - Complete developer and operations documentation

## External Dependencies

**No new external dependencies required** - Implementation will leverage existing technology stack:
- **xUnit** for unit and integration test framework (existing)
- **ASP.NET Core TestHost** for integration testing across configurations (existing)
- **Entity Framework InMemory** provider for safe configuration testing (existing)
- **GitHub Actions** for CI/CD workflow matrix implementation (existing)
- **JSON Schema validation** using System.Text.Json (existing in .NET 8)
- **Existing webServer configuration pattern** (per ADR-0003)

**Key Architectural Decision**: Avoid Playwright configuration matrix complexity that historically caused blocking issues