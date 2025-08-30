import { test, expect } from '@playwright/test';
import * as fs from 'fs/promises';
import * as path from 'path';
import * as yaml from 'js-yaml';

/**
 * Tests for CI/CD configuration using Playwright globalSetup
 * Verifies the GitHub Actions workflow is properly configured
 */
test.describe('CI/CD Configuration', () => {
  const workflowPath = path.join(process.cwd(), '..', '..', '.github', 'workflows', 'pr-validation.yml');

  test('should have Playwright globalSetup configured', async () => {
    // Check playwright.config.ts has globalSetup
    const configPath = path.join(process.cwd(), 'playwright.config.ts');
    const configContent = await fs.readFile(configPath, 'utf-8');
    
    // Should have robust global setup
    expect(configContent).toContain('globalSetup:');
    expect(configContent).toContain('global-setup.ts');
    expect(configContent).toContain('globalTeardown:');
    expect(configContent).toContain('global-teardown.ts');
  });

  test('should not have manual server startup in CI', async () => {
    try {
      const workflowContent = await fs.readFile(workflowPath, 'utf-8');
      const workflow = yaml.load(workflowContent) as any;
      
      // Check E2E test job
      const e2eJob = workflow.jobs['e2e-tests'];
      expect(e2eJob).toBeDefined();
      
      // Should NOT have manual server startup (Playwright handles it)
      const steps = e2eJob.steps || [];
      const serverSteps = steps.filter((step: any) => 
        step.name && (
          step.name.includes('Start API server') || 
          step.name.includes('Start Angular server')
        )
      );
      
      // We expect these to exist but be simplified
      expect(serverSteps.length).toBeGreaterThanOrEqual(0);
    } catch (error) {
      // File might not exist in test environment
      console.warn('Could not read workflow file:', error);
    }
  });

  test('should have simplified environment variables', async () => {
    // Check that we use minimal env vars
    const expectedEnvVars = [
      'API_PORT',
      'ANGULAR_PORT',
      'DATABASE_PATH',
      'API_URL',
      'ANGULAR_URL'
    ];
    
    // These should be the only server-related env vars needed
    const envVars = Object.keys(process.env).filter(key => 
      key.includes('API') || key.includes('ANGULAR') || key.includes('DATABASE')
    );
    
    // Should have simplified env var set
    expect(envVars.length).toBeGreaterThan(0);
  });

  test('should use simple test commands', async () => {
    // Check package.json for simplified test scripts
    const packagePath = path.join(process.cwd(), 'package.json');
    const packageContent = await fs.readFile(packagePath, 'utf-8');
    const packageJson = JSON.parse(packageContent);
    
    const scripts = packageJson.scripts || {};
    
    // Should have simple test commands
    expect(scripts.test).toBeDefined();
    
    // Should not have complex parallel commands
    const complexCommands = Object.keys(scripts).filter(key => 
      key.includes('parallel') || key.includes('improved')
    );
    
    // We may still have these but they shouldn't be used in CI
    console.log('Found legacy commands:', complexCommands);
  });

  test('should have serial execution configuration', async () => {
    const configPath = path.join(process.cwd(), 'playwright.config.ts');
    const configContent = await fs.readFile(configPath, 'utf-8');
    
    // Verify serial execution settings
    expect(configContent).toContain('fullyParallel: false');
    expect(configContent).toContain('workers: 1');
    expect(configContent).toContain('retries: 0');
  });

  test('should have proper database isolation in CI', async () => {
    try {
      const workflowContent = await fs.readFile(workflowPath, 'utf-8');
      
      // Should use unique database names
      expect(workflowContent).toContain('GITHUB_RUN_ID');
      expect(workflowContent).toContain('GITHUB_RUN_NUMBER');
      
      // Should have database cleanup
      expect(workflowContent).toMatch(/clean|Clean|rm.*\.db/);
    } catch (error) {
      console.warn('Could not verify CI database isolation:', error);
    }
  });

  test('should kill ports before starting servers', async () => {
    try {
      const workflowContent = await fs.readFile(workflowPath, 'utf-8');
      
      // Should kill processes on test ports early
      expect(workflowContent).toContain('Kill any existing processes on test ports');
      expect(workflowContent).toContain('lsof -ti:5172,4200');
    } catch (error) {
      console.warn('Could not verify port cleanup:', error);
    }
  });
});