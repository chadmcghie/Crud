/**
 * Test Logger - Structured logging for E2E tests
 *
 * Provides consistent logging with levels, timing, and context
 * to help diagnose test failures and track performance.
 */

import { TestInfo } from '@playwright/test';

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3
}

export interface LogContext {
  testName?: string;
  workerIndex?: number;
  testId?: string;
  timestamp?: number;
  elapsedMs?: number;
}

export interface DatabaseState {
  peopleCount: number;
  rolesCount: number;
  wallsCount?: number;
  windowsCount?: number;
}

export class TestLogger {
  private level: LogLevel;
  private context: LogContext;
  private startTime: number;
  private logs: Array<{ level: LogLevel; message: string; timestamp: number }> = [];

  constructor(testInfo?: TestInfo) {
    // Set log level based on environment
    this.level = this.getLogLevelFromEnv();

    // Initialize context
    this.context = {
      testName: testInfo?.title,
      workerIndex: testInfo?.workerIndex,
      testId: `T${Date.now()}_${Math.random().toString(36).substr(2, 6)}`,
      timestamp: Date.now()
    };

    this.startTime = Date.now();
  }

  private getLogLevelFromEnv(): LogLevel {
    const envLevel = process.env.E2E_LOG_LEVEL?.toUpperCase();
    switch (envLevel) {
      case 'DEBUG': return LogLevel.DEBUG;
      case 'INFO': return LogLevel.INFO;
      case 'WARN': return LogLevel.WARN;
      case 'ERROR': return LogLevel.ERROR;
      default: return process.env.CI ? LogLevel.INFO : LogLevel.DEBUG;
    }
  }

  private formatMessage(level: LogLevel, message: string): string {
    const elapsed = ((Date.now() - this.startTime) / 1000).toFixed(1);
    const levelIcon = this.getLevelIcon(level);
    const levelName = LogLevel[level];
    const workerInfo = this.context.workerIndex !== undefined ? `W${this.context.workerIndex}` : '';

    return `${levelIcon} [${elapsed}s] ${workerInfo} ${levelName}: ${message}`;
  }

  private getLevelIcon(level: LogLevel): string {
    switch (level) {
      case LogLevel.DEBUG: return '🔍';
      case LogLevel.INFO: return '📊';
      case LogLevel.WARN: return '⚠️';
      case LogLevel.ERROR: return '❌';
    }
  }

  private log(level: LogLevel, message: string, data?: any): void {
    if (level >= this.level) {
      const formattedMessage = this.formatMessage(level, message);

      // Store log for later retrieval
      this.logs.push({ level, message: formattedMessage, timestamp: Date.now() });

      // Output to console
      switch (level) {
        case LogLevel.ERROR:
          console.error(formattedMessage, data || '');
          break;
        case LogLevel.WARN:
          console.warn(formattedMessage, data || '');
          break;
        default:
          console.log(formattedMessage, data || '');
      }
    }
  }

  debug(message: string, data?: any): void {
    this.log(LogLevel.DEBUG, message, data);
  }

  info(message: string, data?: any): void {
    this.log(LogLevel.INFO, message, data);
  }

  warn(message: string, data?: any): void {
    this.log(LogLevel.WARN, message, data);
  }

  error(message: string, error?: Error | any): void {
    const errorDetails = error instanceof Error
      ? { message: error.message, stack: error.stack }
      : error;
    this.log(LogLevel.ERROR, message, errorDetails);
  }

  /**
   * Log test start with context
   */
  testStart(testName?: string): void {
    const name = testName || this.context.testName || 'Unknown Test';
    this.info(`Starting test: ${name}`);
  }

  /**
   * Log test completion with status
   */
  testComplete(passed: boolean): void {
    const status = passed ? '✅ PASSED' : '❌ FAILED';
    const elapsed = ((Date.now() - this.startTime) / 1000).toFixed(2);
    this.info(`Test ${status} in ${elapsed}s`);
  }

  /**
   * Log database state
   */
  logDatabaseState(state: DatabaseState, phase: 'before' | 'after'): void {
    const stateStr = `People: ${state.peopleCount}, Roles: ${state.rolesCount}`;
    const wallsInfo = state.wallsCount !== undefined ? `, Walls: ${state.wallsCount}` : '';
    const windowsInfo = state.windowsCount !== undefined ? `, Windows: ${state.windowsCount}` : '';

    this.info(`Database state (${phase}): ${stateStr}${wallsInfo}${windowsInfo}`);
  }

  /**
   * Log API operation
   */
  logApiCall(method: string, endpoint: string, statusCode?: number, duration?: number): void {
    const durationStr = duration ? ` (${duration}ms)` : '';
    const statusStr = statusCode ? ` => ${statusCode}` : '';
    this.debug(`API: ${method} ${endpoint}${statusStr}${durationStr}`);
  }

  /**
   * Log navigation
   */
  logNavigation(from: string, to: string): void {
    this.debug(`Navigation: ${from} → ${to}`);
  }

  /**
   * Log UI interaction
   */
  logInteraction(action: string, target: string, details?: string): void {
    const detailsStr = details ? ` (${details})` : '';
    this.debug(`UI: ${action} on ${target}${detailsStr}`);
  }

  /**
   * Log timing information
   */
  logTiming(operation: string, durationMs: number, threshold?: number): void {
    const message = `${operation} took ${durationMs}ms`;

    if (threshold && durationMs > threshold) {
      this.warn(`${message} (exceeded ${threshold}ms threshold)`);
    } else {
      this.debug(message);
    }
  }

  /**
   * Create a child logger with additional context
   */
  createChild(additionalContext: Partial<LogContext>): TestLogger {
    const child = new TestLogger();
    child.context = { ...this.context, ...additionalContext };
    child.startTime = this.startTime; // Preserve original start time
    return child;
  }

  /**
   * Get all logs for test reporting
   */
  getLogs(): Array<{ level: LogLevel; message: string; timestamp: number }> {
    return [...this.logs];
  }

  /**
   * Clear accumulated logs
   */
  clearLogs(): void {
    this.logs = [];
  }

  /**
   * Dump logs to console (useful for debugging failures)
   */
  dumpLogs(): void {
    console.log('\n========== Test Log Dump ==========');
    console.log(`Test: ${this.context.testName}`);
    console.log(`Worker: ${this.context.workerIndex}`);
    console.log(`Test ID: ${this.context.testId}`);
    console.log('-----------------------------------');

    this.logs.forEach(log => {
      console.log(log.message);
    });

    console.log('===================================\n');
  }
}

/**
 * Global test logger instance (singleton per test)
 */
let globalLogger: TestLogger | null = null;

export function initializeTestLogger(testInfo?: TestInfo): TestLogger {
  globalLogger = new TestLogger(testInfo);
  return globalLogger;
}

export function getTestLogger(): TestLogger {
  if (!globalLogger) {
    globalLogger = new TestLogger();
  }
  return globalLogger;
}

/**
 * Performance timer utility
 */
export class PerformanceTimer {
  private timers: Map<string, number> = new Map();
  private logger: TestLogger;

  constructor(logger?: TestLogger) {
    this.logger = logger || getTestLogger();
  }

  start(name: string): void {
    this.timers.set(name, Date.now());
    this.logger.debug(`Timer started: ${name}`);
  }

  end(name: string, threshold?: number): number {
    const startTime = this.timers.get(name);
    if (!startTime) {
      this.logger.warn(`Timer not found: ${name}`);
      return 0;
    }

    const duration = Date.now() - startTime;
    this.logger.logTiming(name, duration, threshold);
    this.timers.delete(name);

    return duration;
  }

  /**
   * Measure async operation
   */
  async measure<T>(name: string, operation: () => Promise<T>, threshold?: number): Promise<T> {
    this.start(name);
    try {
      const result = await operation();
      this.end(name, threshold);
      return result;
    } catch (error) {
      this.end(name, threshold);
      throw error;
    }
  }
}

/**
 * Decorator for logging test methods
 */
export function LoggedTest(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
  const originalMethod = descriptor.value;

  descriptor.value = async function(...args: any[]) {
    const logger = getTestLogger();
    logger.testStart(propertyKey);

    try {
      const result = await originalMethod.apply(this, args);
      logger.testComplete(true);
      return result;
    } catch (error) {
      logger.error(`Test failed: ${propertyKey}`, error);
      logger.testComplete(false);
      logger.dumpLogs();
      throw error;
    }
  };

  return descriptor;
}