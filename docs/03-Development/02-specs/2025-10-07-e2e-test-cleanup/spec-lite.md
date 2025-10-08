# Spec Summary (Lite)

Refactor E2E test infrastructure to remove test-specific code from Angular production files (140+ lines across auth.service.ts, app.ts, auth.guard.ts) and replace 14 timer-based waits with event-driven Playwright patterns. This eliminates test backdoors in production code, improves test reliability in CI environments, and enhances code maintainability by separating test concerns from application logic.
