import { test, expect } from './setup/api-only-fixture';

test.describe('Environment Detection Tests', () => {
  test('@smoke should detect Testing environment correctly', async ({ apiContext }) => {
    console.log('🔍 Testing environment detection...');

    // Test environment endpoint
    const envResponse = await apiContext.get('/api/test/environment');
    console.log('Environment endpoint status:', envResponse.status());

    expect(envResponse.status()).toBe(200);
    const envData = await envResponse.json();
    console.log('📊 Environment data:', JSON.stringify(envData, null, 2));

    // Verify we're in Testing environment
    expect(envData.environmentName).toBe('Testing');
    expect(envData.isTesting).toBe(true);
  });

  test('@smoke should bypass authorization in Testing environment', async ({ apiContext }) => {
    console.log('🔐 Testing authorization bypass...');

    // Test auth bypass detection
    const authTestResponse = await apiContext.get('/api/test/auth-test');
    console.log('Auth test endpoint status:', authTestResponse.status());

    expect(authTestResponse.status()).toBe(200);
    const authData = await authTestResponse.json();
    console.log('🔐 Auth test data:', JSON.stringify(authData, null, 2));

    expect(authData.shouldBypass).toBe(true);
  });

  test('@smoke should allow creating people without authorization in Testing', async ({ apiContext }) => {
    console.log('🧪 Testing actual people creation without auth...');

    const createResponse = await apiContext.post('/api/people', {
      data: {
        FullName: 'Test Environment Person',
        Phone: '+1-555-0123',
        RoleIds: []
      }
    });

    console.log('People creation status:', createResponse.status());
    console.log('Response text:', await createResponse.text());

    if (createResponse.ok()) {
      console.log('✅ People creation SUCCESS!');
      const person = await createResponse.json();
      console.log('Created person:', person);
      expect(person.fullName).toBe('Test Environment Person');
    } else {
      console.log('❌ People creation failed - this should work in Testing environment');
      expect(createResponse.status()).toBe(201); // This should pass if our fix works
    }
  });
});