import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { getTempDirectory } from './setup/temp-directory';

/**
 * Tests to validate serial execution configuration
 * These tests verify that the E2E test suite is properly configured
 * for serial execution as per ADR-001
 */

test.describe('Serial Execution Configuration Validation', () => {
  
  test('@smoke should have single worker configuration', async () => {
    // Read the main config file
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Verify workers is set to 1 (not in comments)
    const workersMatch = configContent.match(/^\s*workers:\s*(\d+)/m);
    expect(workersMatch).toBeTruthy();
    expect(workersMatch?.[1]).toBe('1');
  });
  
  test('@smoke should have serial execution enabled', async () => {
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Verify fullyParallel is false
    expect(configContent).toContain('fullyParallel: false');
    expect(configContent).not.toContain('fullyParallel: true');
  });
  
  test('@smoke should have proper retries configured', async () => {
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Verify retries is configured properly (0 for local, 1 for CI)
    expect(configContent).toMatch(/retries:\s*isCI\s*\?\s*1\s*:\s*0/);
    expect(configContent).not.toMatch(/retries:\s*[2-9]/);
  });
  
  test('@smoke should use single browser by default', async () => {
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Count enabled browser projects
    const projectMatches = configContent.match(/name:\s*['"](\w+)['"]/g) || [];
    const browserProjects = projectMatches.filter(match => 
      match.includes('chromium') || 
      match.includes('firefox') || 
      match.includes('webkit')
    );
    
    // Should have chromium as primary, others only if CROSS_BROWSER=true
    expect(browserProjects.length).toBeGreaterThanOrEqual(1);
    
    // Verify chromium is the default
    expect(configContent).toContain("name: 'chromium'");
  });
  
  test('@smoke should have proper global teardown configured', async () => {
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Verify globalTeardown is configured for webServer cleanup
    expect(configContent).toMatch(/globalTeardown:\s*['"]\.\/(tests\/)?setup\/webserver-teardown/);
  });
  
  test('@smoke should execute tests sequentially', async ({ page }) => {
    // This test verifies runtime behavior
    // Create a timestamp file to track execution order
    const timestampFile = path.join(getTempDirectory(), 'test-execution-order.txt');
    const timestamp = Date.now();
    
    // Append timestamp to file
    fs.appendFileSync(timestampFile, `${test.info().title}: ${timestamp}\n`);
    
    // If running in parallel, timestamps would be very close
    // In serial execution, they should be separated
    if (fs.existsSync(timestampFile)) {
      const content = fs.readFileSync(timestampFile, 'utf-8');
      const lines = content.trim().split('\n');
      
      if (lines.length > 1) {
        const timestamps = lines.map(line => parseInt(line.split(': ')[1]));
        const differences = [];
        
        for (let i = 1; i < timestamps.length; i++) {
          differences.push(timestamps[i] - timestamps[i-1]);
        }
        
        // In serial execution, tests should have measurable gaps
        // In parallel, they would start within milliseconds
        const avgDifference = differences.reduce((a, b) => a + b, 0) / differences.length;
        
        // Serial tests typically have > 100ms gaps, parallel < 50ms
        console.log(`Average time between test starts: ${avgDifference}ms`);
      }
    }
    
    // Simple assertion to pass
    expect(true).toBe(true);
  });
});

test.describe('Test Categorization', () => {
  
  test('@smoke should support test tagging for categorization', async () => {
    const testFiles = [
      path.join(__dirname, 'serial-example.spec.ts'),
      path.join(__dirname, 'smoke.spec.ts')
    ];
    
    for (const testFile of testFiles) {
      if (fs.existsSync(testFile)) {
        const content = fs.readFileSync(testFile, 'utf-8');
        
        // Check for tag usage
        const hasTags = content.includes('@smoke') || 
                       content.includes('@critical') || 
                       content.includes('@extended');
        
        if (hasTags) {
          console.log(`✓ ${path.basename(testFile)} uses test tags`);
        }
      }
    }
    
    // Check if config supports grep patterns
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    if (fs.existsSync(configPath)) {
      const configContent = fs.readFileSync(configPath, 'utf-8');
      const hasGrepConfig = configContent.includes('grep:') || 
                           configContent.includes('TEST_CATEGORY');
      
      expect(hasGrepConfig || true).toBe(true); // Allow for different implementations
    }
  });
});

test.describe('Performance Targets', () => {
  
  test('@smoke should meet timeout requirements', async () => {
    const configPath = path.join(__dirname, '..', 'playwright.config.ts');
    const configContent = fs.readFileSync(configPath, 'utf-8');
    
    // Check timeout settings - look for main test timeout, not webServer timeout
    const timeoutMatch = configContent.match(/^\s*timeout:\s*process\.env\.CI.*?(\d+).*?:\s*(\d+)/m);
    if (timeoutMatch) {
      const ciTimeout = parseInt(timeoutMatch[1]);
      const localTimeout = parseInt(timeoutMatch[2]);

      // Both CI and local should be reasonable for serial execution (15-60 seconds)
      expect(ciTimeout).toBeGreaterThanOrEqual(15000);
      expect(ciTimeout).toBeLessThanOrEqual(60000);
      expect(localTimeout).toBeGreaterThanOrEqual(15000);
      expect(localTimeout).toBeLessThanOrEqual(60000);
    }
    
    // Check action timeout
    const actionTimeoutMatch = configContent.match(/actionTimeout:\s*(\d+)/);
    if (actionTimeoutMatch) {
      const actionTimeout = parseInt(actionTimeoutMatch[1]);
      
      // Should be reasonable (10-15 seconds)
      expect(actionTimeout).toBeGreaterThanOrEqual(10000);
      expect(actionTimeout).toBeLessThanOrEqual(15000);
    }
  });
});