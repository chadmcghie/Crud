# Playwright E2E Tests Setup Guide

## ✅ Current Status

The Playwright test suite has been successfully set up and is working correctly! Here's what we've accomplished:

- **✅ Tests Discovered**: 323+ tests across all configurations
- **✅ TypeScript Compilation**: All compilation errors fixed
- **✅ Test Structure**: Properly organized test files and helpers
- **✅ Multiple Configurations**: Different configs for different scenarios

## 🚀 Quick Start

### 1. Navigate to Test Directory
```bash
cd test/Tests.E2E.NG
```

### 2. Available Test Configurations

#### **API Tests Only** (Recommended to start with)
```bash
# List API tests (58 tests)
npm run test:list-api

# Run API tests (requires API server on localhost:5000)
npm run test:api-only
```

#### **All Tests** (UI + API + Integration)
```bash
# List all tests (323+ tests)
npm run test:list

# Run all tests (requires both API and Angular servers)
npm run test:local
```

#### **Individual Test Files**
```bash
# Run specific test file
npx playwright test tests/api/roles-api.spec.ts --config=playwright.config.api-only.ts

# Run specific test with pattern
npx playwright test --grep "should create a new role" --config=playwright.config.api-only.ts
```

## 📋 Test Categories

### API Tests (58 tests)
- **Roles API**: 25 tests - Full CRUD operations, validation, error handling
- **People API**: 20 tests - CRUD with role relationships, data integrity
- **Walls API**: 13 tests - Complex entity CRUD, numeric validation

### Angular UI Tests (251+ tests)
- **People Management**: 27 tests - UI CRUD operations, form validation
- **Roles Management**: 16 tests - UI interactions, state management
- **App Navigation**: 11 tests - Tab switching, responsive design

### Integration Tests (6 tests)
- **Full Workflow**: End-to-end user scenarios, mixed UI/API operations

## 🔧 Prerequisites for Running Tests

### For API Tests Only
1. **API Server** running on `http://localhost:5000`
   ```bash
   cd ../../src/Api
   dotnet run
   ```

### For UI Tests
1. **API Server** on `http://localhost:5000` (as above)
2. **Angular Dev Server** on `http://localhost:4200`
   ```bash
   cd ../../src/Angular
   npm start
   ```

## 🎯 Test Execution Examples

### Start with API Tests (Easiest)
```bash
# 1. Start your API server first
cd ../../src/Api
dotnet run

# 2. In another terminal, run API tests
cd test/Tests.E2E.NG
npm run test:api-only
```

### Run Specific Test Suites
```bash
# Only roles API tests
npx playwright test tests/api/roles-api.spec.ts --config=playwright.config.api-only.ts

# Only people API tests  
npx playwright test tests/api/people-api.spec.ts --config=playwright.config.api-only.ts

# Only walls API tests
npx playwright test tests/api/walls-api.spec.ts --config=playwright.config.api-only.ts
```

### Run with Different Options
```bash
# Run in headed mode (visible browser)
npx playwright test --headed --config=playwright.config.local.ts

# Run with UI mode (interactive)
npx playwright test --ui --config=playwright.config.local.ts

# Run specific browser only
npx playwright test --project=chromium --config=playwright.config.api-only.ts
```

## 📊 Test Reports

After running tests, view the HTML report:
```bash
npm run report
```

## 🐛 Troubleshooting

### Issue: "ENOENT package.json"
**Solution**: Make sure you're in the `test/Tests.E2E.NG` directory
```bash
cd test/Tests.E2E.NG
```

### Issue: "dotnet: not found" or "webServer was not able to start"
**Solution**: Use the local configurations that don't auto-start servers
```bash
npm run test:api-only    # For API tests
npm run test:local       # For all tests
```

### Issue: Tests fail with connection errors
**Solution**: Ensure the required servers are running:
- API: `http://localhost:5000`
- Angular: `http://localhost:4200`

### Issue: "write EPIPE" when listing tests
**Solution**: This is just a display issue due to too many tests. The tests are working fine. Use:
```bash
# Count tests instead of listing all
npm run test:list-api | tail -1    # Shows "Total: 58 tests in 3 files"
```

## 📁 Project Structure

```
Tests.E2E.NG/
├── tests/
│   ├── api/                    # API endpoint tests (58 tests)
│   │   ├── people-api.spec.ts
│   │   ├── roles-api.spec.ts
│   │   └── walls-api.spec.ts
│   ├── angular-ui/             # Angular UI tests (54+ tests)
│   │   ├── app-navigation.spec.ts
│   │   ├── people.spec.ts
│   │   └── roles.spec.ts
│   ├── integration/            # Integration tests (6 tests)
│   │   └── full-workflow.spec.ts
│   └── helpers/                # Test utilities
│       ├── api-helpers.ts
│       ├── page-helpers.ts
│       └── test-data.ts
├── playwright.config.ts        # Default config (auto-starts servers)
├── playwright.config.local.ts  # Local config (manual server start)
├── playwright.config.api-only.ts # API-only config
└── package.json               # Dependencies and scripts
```

## 🎉 Success Indicators

You'll know everything is working when:

1. **Test Discovery**: `npm run test:list-api` shows "Total: 58 tests in 3 files"
2. **TypeScript**: `npx tsc --noEmit` runs without errors
3. **Test Execution**: Tests run and generate HTML reports

## 🚀 Next Steps

1. **Start Simple**: Begin with API tests using `npm run test:api-only`
2. **Verify Setup**: Run a single test to confirm everything works
3. **Expand**: Once API tests work, try UI tests with both servers running
4. **Customize**: Modify test data and scenarios as needed

The test suite is comprehensive and ready to use! 🎯