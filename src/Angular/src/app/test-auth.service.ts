import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

/**
 * Test authentication service
 * Provides bypass mechanism for E2E tests while maintaining security in production
 */
@Injectable({
  providedIn: 'root'
})
export class TestAuthService {
  
  /**
   * Check if we're in E2E test mode
   * Can be controlled via environment variable or special header
   */
  isE2ETestMode(): boolean {
    // Check for E2E test token in localStorage (set by auth helper)
    const testToken = localStorage.getItem('e2e-test-mode');
    
    // Only allow in non-production environments
    return !environment.production && testToken === 'active';
  }

  /**
   * Get mock user for E2E tests
   */
  getMockUser() {
    return {
      email: 'test@example.com',
      roles: ['admin'], // Give admin access for testing
      firstName: 'Test',
      lastName: 'User'
    };
  }

  /**
   * Enable E2E test mode
   * Should only be called from test setup
   */
  enableE2EMode() {
    if (!environment.production) {
      localStorage.setItem('e2e-test-mode', 'active');
    }
  }

  /**
   * Disable E2E test mode
   */
  disableE2EMode() {
    localStorage.removeItem('e2e-test-mode');
  }
}