# Blocking Issues Registry

## Overview
Master registry of all blocking issues encountered in the project. This registry provides quick lookup for issue patterns and resolutions.

## Active Issues
*No active blocking issues*

## Resolved Issues
| ID | Created | Resolved | Spec | Category | Description | Resolution Summary |
|---|---|---|---|---|---|---|
| BI-2025-08-30-001 | 2025-08-30 | 2025-08-31 | test-server-optimization | test | Smoke test auth failure in CI - SQLite database path issues | Fixed TestDatabaseFactory to use current directory in CI, added 0.0.0.0 binding for Docker |
| BI-2025-08-30-002 | 2025-08-30 | 2025-08-31 | test-server-optimization | test | E2E test timeouts and API connection failures | Fixed manual cleanup timeout, hardcoded URLs, and Docker networking issues |
| BI-2025-08-31-001 | 2025-08-31 | 2025-09-05 | test-server-optimization | test | E2E tests failing in CI - Docker networking issues | Resolved by using correct playwright.config.webserver.ts configuration with Playwright's built-in webServer feature |

## Common Patterns
Document recurring issue patterns here as they emerge.

## Statistics
- Total Issues: 3
- Active: 0
- Resolved: 3
- Average Resolution Time: ~10 hours