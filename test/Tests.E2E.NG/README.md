# End-to-End Tests for Angular Application and API

This project contains comprehensive Playwright end-to-end tests for the Angular CRUD application and its API endpoints.

## Test Coverage

### Angular UI Tests
- **Roles Management** (`tests/angular-ui/roles.spec.ts`)
  - Create, read, update, delete roles
  - Form validation and error handling
  - UI interactions and state management

- **People Management** (`tests/angular-ui/people.spec.ts`)
  - Create, read, update, delete people
  - Role assignment and management
  - Form validation and error handling

- **Application Navigation** (`tests/angular-ui/app-navigation.spec.ts`)
  - Tab switching and navigation
  - Responsive design testing
  - Layout and styling verification

### API Tests
- **Roles API** (`tests/api/roles-api.spec.ts`)
  - Full CRUD operations for roles
  - HTTP status code validation
  - Data validation and error handling
  - Concurrent operations testing

- **People API** (`tests/api/people-api.spec.ts`)
  - Full CRUD operations for people
  - Role assignment and relationship management
  - Data validation and edge cases
  - Referential integrity testing

- **Walls API** (`tests/api/walls-api.spec.ts`)
  - Full CRUD operations for walls
  - Numeric field validation
  - Timestamp integrity
  - Complex data structure handling

### Integration Tests
- **Full Workflow** (`tests/integration/full-workflow.spec.ts`)
  - End-to-end user workflows
  - Mixed UI and API operations
  - Data consistency across operations
  - Error scenario handling

## Project Structure

```
Tests.E2E.NG/
├── tests/
│   ├── angular-ui/          # Angular UI tests
│   ├── api/                 # API endpoint tests
│   ├── integration/         # Integration tests
│   └── helpers/             # Test utilities and helpers
│       ├── api-helpers.ts   # API interaction helpers
│       ├── page-helpers.ts  # UI interaction helpers
│       └── test-data.ts     # Test data generators
├── playwright.config.ts     # Playwright configuration
├── package.json            # Node.js dependencies
├── tsconfig.json           # TypeScript configuration
└── README.md               # This file
```

## Prerequisites

1. **.NET 8 SDK** - For running the API
2. **Node.js 18+** - For running Angular and Playwright
3. **Angular CLI** - For the Angular application

## Setup

1. **Install Node.js dependencies:**
   ```bash
   cd test/Tests.E2E.NG
   npm install
   ```

2. **Install Playwright browsers:**
   ```bash
   npm run install-browsers
   ```

3. **Ensure the API and Angular applications are configured:**
   - API should run on `http://localhost:5000`
   - Angular should run on `http://localhost:4200`

## Running Tests

### All Tests
```bash
npm test
```

### Specific Test Suites
```bash
# Angular UI tests only
npx playwright test tests/angular-ui

# API tests only
npx playwright test tests/api

# Integration tests only
npx playwright test tests/integration
```

### Interactive Mode
```bash
# Run tests with UI
npm run test:ui

# Run tests in headed mode (visible browser)
npm run test:headed

# Debug mode
npm run test:debug
```

### Test Reports
```bash
# View HTML report
npm run report
```

## Configuration

The tests are configured to:
- **Automatically start** the API and Angular servers before running tests
- **Run in parallel** across multiple browsers (Chrome, Firefox, Safari)
- **Capture screenshots** on failure
- **Record videos** on failure
- **Generate traces** for debugging

### Environment Configuration

The `playwright.config.ts` file includes:
- **Base URL**: `http://localhost:4200`
- **API Server**: Automatically started on port 5000
- **Angular Server**: Automatically started on port 4200
- **Browsers**: Chrome, Firefox, WebKit
- **Retries**: 2 retries on CI, 0 locally
- **Parallel execution**: Enabled

## Test Data Management

Tests use generated test data to ensure:
- **Isolation**: Each test runs with fresh data
- **Randomization**: Reduces test interdependencies
- **Cleanup**: Automatic cleanup after each test

### Test Data Generators
- `generateTestRole()`: Creates random role data
- `generateTestPerson()`: Creates random person data
- `generateTestWall()`: Creates random wall data

## Helper Classes

### ApiHelpers
Provides methods for direct API interactions:
- CRUD operations for all entities
- Bulk cleanup operations
- Error handling and validation

### PageHelpers
Provides methods for UI interactions:
- Form filling and submission
- Navigation and tab switching
- Element verification and validation

## Best Practices

1. **Test Isolation**: Each test cleans up its data
2. **Parallel Execution**: Tests can run concurrently
3. **Error Handling**: Comprehensive error scenario coverage
4. **Data Validation**: Both positive and negative test cases
5. **Cross-browser Testing**: Ensures compatibility
6. **Responsive Testing**: Mobile and desktop viewports

## Debugging

### Failed Tests
1. Check the HTML report: `npm run report`
2. View screenshots in `test-results/`
3. Watch recorded videos for failed tests
4. Use trace viewer for detailed debugging

### Local Development
```bash
# Run specific test file
npx playwright test tests/angular-ui/roles.spec.ts

# Run with debug mode
npx playwright test --debug tests/angular-ui/roles.spec.ts

# Run in headed mode
npx playwright test --headed tests/angular-ui/roles.spec.ts
```

## Continuous Integration

The tests are configured for CI environments:
- **Retry Logic**: Failed tests are retried automatically
- **Parallel Workers**: Optimized for CI performance
- **Artifact Collection**: Screenshots, videos, and traces
- **Server Management**: Automatic startup and cleanup

## Troubleshooting

### Common Issues

1. **Port Conflicts**: Ensure ports 4200 and 5000 are available
2. **Browser Installation**: Run `npm run install-browsers`
3. **Server Startup**: Check that both API and Angular start correctly
4. **Network Issues**: Verify localhost connectivity

### Debug Commands
```bash
# Check Playwright installation
npx playwright --version

# List available browsers
npx playwright install --dry-run

# Test configuration
npx playwright test --list
```

## Contributing

When adding new tests:
1. Follow the existing test structure
2. Use the helper classes for common operations
3. Ensure proper cleanup in `afterEach` hooks
4. Add appropriate test data generators
5. Include both positive and negative test cases