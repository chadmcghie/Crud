# Spec Tasks

## Tasks

- [x] 1. Update Main Playwright Configuration for Serial Execution
  - [x] 1.1 Write tests to verify serial execution behavior
  - [x] 1.2 Update playwright.config.ts with workers=1 and fullyParallel=false
  - [x] 1.3 Remove retries configuration and set to 0
  - [x] 1.4 Configure single Chromium browser as default
  - [x] 1.5 Update globalSetup and globalTeardown paths
  - [x] 1.6 Verify all tests pass with new configuration

- [x] 2. Simplify Server Management
  - [x] 2.1 Write tests for simplified server startup/shutdown
  - [x] 2.2 Create new simple global-setup.ts without persistence logic
  - [x] 2.3 Create new simple global-teardown.ts with basic cleanup
  - [x] 2.4 Remove PersistentServerManager class and related files
  - [x] 2.5 Remove lock file management code
  - [x] 2.6 Update test fixtures to use simplified setup
  - [x] 2.7 Verify server management works correctly

- [x] 3. Implement Simple Database Cleanup
  - [x] 3.1 Write tests for SQLite file-based cleanup
  - [x] 3.2 Simplify database-utils.ts to basic delete/recreate
  - [x] 3.3 Update test fixtures for per-file database reset
  - [x] 3.4 Remove complex cleanup and monitoring logic
  - [x] 3.5 Verify database isolation between test files

- [ ] 4. Update CI/CD Pipeline
  - [ ] 4.1 Write tests for CI configuration
  - [ ] 4.2 Update pr-validation.yml to use Playwright globalSetup
  - [ ] 4.3 Remove manual server startup steps from CI
  - [ ] 4.4 Simplify environment variable configuration
  - [ ] 4.5 Update test commands in CI workflow
  - [ ] 4.6 Verify CI pipeline executes successfully

- [ ] 5. Clean Up and Consolidate
  - [ ] 5.1 Write integration tests for complete flow
  - [ ] 5.2 Remove unused configuration files (parallel, improved, etc.)
  - [ ] 5.3 Update package.json scripts to simplified commands
  - [ ] 5.4 Update documentation to reflect new setup
  - [ ] 5.5 Remove deprecated test helpers and utilities
  - [ ] 5.6 Run full test suite to verify 100% reliability
  - [ ] 5.7 Measure and confirm execution times meet targets
  - [ ] 5.8 Verify all tests pass consistently