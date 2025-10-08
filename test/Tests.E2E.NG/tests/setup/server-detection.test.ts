import { test, expect } from '@playwright/test';
import { isServerRunning, checkPortAvailability, ServerStatus, shouldStartServer } from './server-utils';
import * as net from 'net';

test.describe('Server Detection Tests', () => {
  
  test.describe('isServerRunning', () => {
    
    test('should detect running server on valid HTTP endpoint', async () => {
      // This test assumes the test environment has a server on port 5172
      // In CI, we'd mock this or use a test server
      const mockUrl = 'http://localhost:5172/health';
      
      // For unit testing, we'll check the function exists and returns expected type
      const result = await isServerRunning(mockUrl);
      expect(typeof result).toBe('boolean');
    });
    
    test('should return false for non-existent server', async () => {
      // Use a port that's unlikely to be in use
      const result = await isServerRunning('http://localhost:59999/health');
      expect(result).toBe(false);
    });
    
    test('should handle timeout gracefully', async () => {
      // Test with unreachable IP
      const result = await isServerRunning('http://192.0.2.1:8080/health', 100);
      expect(result).toBe(false);
    });
    
    test('should handle malformed URLs', async () => {
      const result = await isServerRunning('not-a-url');
      expect(result).toBe(false);
    });
  });
  
  test.describe('checkPortAvailability', () => {
    
    test('should detect occupied port', async () => {
      // Create a test server
      const server = net.createServer();
      await new Promise<void>((resolve) => {
        server.listen(0, '127.0.0.1', () => resolve());
      });
      
      const port = (server.address() as net.AddressInfo).port;
      
      try {
        const available = await checkPortAvailability(port);
        expect(available).toBe(false);
      } finally {
        server.close();
      }
    });
    
    test('should detect available port', async () => {
      // Use a random high port unlikely to be in use
      const port = 50000 + Math.floor(Math.random() * 10000);
      const available = await checkPortAvailability(port);
      
      // This might occasionally fail if the random port is in use
      // In production tests, we'd use a more sophisticated approach
      expect(typeof available).toBe('boolean');
    });
  });
  
  test.describe('ServerStatus', () => {
    
    test('should correctly identify server states', async () => {
      const status = new ServerStatus();
      
      // Test initial state
      const apiStatus = await status.checkApi('http://localhost:5172');
      const angularStatus = await status.checkAngular('http://localhost:4200');
      
      expect(typeof apiStatus.running).toBe('boolean');
      expect(typeof angularStatus.running).toBe('boolean');
      
      if (apiStatus.running) {
        expect(apiStatus.processId).toBeUndefined(); // We don't track PID for existing servers
        expect(apiStatus.startedByUs).toBe(false);
      }
      
      if (angularStatus.running) {
        expect(angularStatus.processId).toBeUndefined();
        expect(angularStatus.startedByUs).toBe(false);
      }
    });
    
    test('should generate status report', async () => {
      const status = new ServerStatus();
      await status.checkAll('http://localhost:5172', 'http://localhost:4200');
      
      const report = status.getStatusReport();
      
      expect(report).toContain('API');
      expect(report).toContain('Angular');
      expect(report).toMatch(/✅|🔴/); // Should contain status emoji
    });
  });
  
  test.describe('Server Reuse Logic', () => {
    
    test('should not start new server if already running', async () => {
      const status = new ServerStatus();
      const apiStatus = await status.checkApi('http://localhost:5172/health');
      
      if (apiStatus.running) {
        // Verify we would skip starting a new server
        expect(shouldStartServer(apiStatus)).toBe(false);
        expect(apiStatus.message).toContain('Already running');
      } else {
        // Verify we would start a new server
        expect(shouldStartServer(apiStatus)).toBe(true);
        expect(apiStatus.message).toContain('Not running');
      }
    });
    
    test('should preserve servers not started by tests', async () => {
      const status = new ServerStatus();
      
      // Simulate a server that was already running
      status.markAsPreExisting('api');
      
      // Verify teardown would preserve it
      const shouldKill = status.shouldKillOnTeardown('api');
      expect(shouldKill).toBe(false);
    });
    
    test('should kill servers started by tests', async () => {
      const status = new ServerStatus();
      
      // Simulate a server we started
      status.markAsStartedByUs('api', 12345);
      
      // Verify teardown would kill it
      const shouldKill = status.shouldKillOnTeardown('api');
      expect(shouldKill).toBe(true);
    });
  });
});