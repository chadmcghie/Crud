// Test script to verify environment detection and authorization bypass
const { request } = require('@playwright/test');

async function testEnvironment() {
  const context = await request.newContext({
    baseURL: 'http://localhost:5172'
  });

  try {
    console.log('🔍 Testing environment detection...');

    // Test environment endpoint
    const envResponse = await context.get('/api/test/environment');
    console.log('Environment endpoint status:', envResponse.status());

    if (envResponse.ok()) {
      const envData = await envResponse.json();
      console.log('📊 Environment data:', JSON.stringify(envData, null, 2));
    } else {
      console.log('❌ Environment endpoint failed:', await envResponse.text());
    }

    // Test auth bypass detection
    const authTestResponse = await context.get('/api/test/auth-test');
    console.log('Auth test endpoint status:', authTestResponse.status());

    if (authTestResponse.ok()) {
      const authData = await authTestResponse.json();
      console.log('🔐 Auth test data:', JSON.stringify(authData, null, 2));
    } else {
      console.log('❌ Auth test endpoint failed:', await authTestResponse.text());
    }

    // Now test the actual problematic endpoint
    console.log('\n🧪 Testing actual people creation...');
    const createResponse = await context.post('/api/people', {
      data: {
        FullName: 'Test Environment Person',
        Phone: '+1-555-0123',
        RoleIds: []
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });

    console.log('People creation status:', createResponse.status());
    if (createResponse.ok()) {
      console.log('✅ People creation SUCCESS!');
      const person = await createResponse.json();
      console.log('Created person:', person);
    } else {
      const errorText = await createResponse.text();
      console.log('❌ People creation failed:', errorText);
    }

  } catch (error) {
    console.error('💥 Test error:', error.message);
  } finally {
    await context.dispose();
  }
}

testEnvironment();