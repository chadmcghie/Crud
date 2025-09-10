import * as os from 'os';
import * as path from 'path';

/**
 * Cross-platform temporary directory utility
 * Uses environment variables with smart fallbacks for different operating systems
 */
export function getTempDirectory(): string {
  // Option 2: Environment variables with smart fallbacks
  return process.env.TEMP ||     // Windows primary
         process.env.TMP ||      // Windows alternate
         process.env.TMPDIR ||   // Mac/Linux
         os.tmpdir();            // OS-specific fallback
}

/**
 * Creates a unique temp file path for test databases
 * @param prefix - File prefix (e.g., 'CrudTest')
 * @param extension - File extension (e.g., '.db')
 * @returns Full path to unique temp file
 */
export function getUniqueTempPath(prefix: string = 'CrudTest', extension: string = '.db'): string {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8);
  const filename = `${prefix}_${timestamp}_${random}${extension}`;
  return path.join(getTempDirectory(), filename);
}

/**
 * Creates a worker-specific temp file path for parallel test execution
 * @param workerId - Worker identifier
 * @param prefix - File prefix (e.g., 'CrudTest')
 * @param extension - File extension (e.g., '.db')
 * @returns Full path to worker-specific temp file
 */
export function getWorkerTempPath(workerId: string | number, prefix: string = 'CrudTest', extension: string = '.db'): string {
  const timestamp = Date.now();
  const filename = `${prefix}_Worker${workerId}_${timestamp}${extension}`;
  return path.join(getTempDirectory(), filename);
}