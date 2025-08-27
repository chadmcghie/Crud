import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting global test setup...');
  
  const workers = config.workers || 1;
  const baseApiPort = 5172;
  const baseAngularPort = 4200;
  
  console.log(`🔧 Configuring ${workers} parallel test workers`);
  
  // Set up worker-specific environment variables for database isolation
  for (let i = 0; i < workers; i++) {
    const workerIndex = i;
    const apiPort = baseApiPort + workerIndex;
    const angularPort = baseAngularPort + (workerIndex * 10);
    
    // Create worker-specific database path
    const timestamp = Date.now();
    const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
    const workerDatabase = `${tempDir}${process.platform === 'win32' ? '\\' : '/'}CrudTest_Worker${workerIndex}_${timestamp}.db`;
    
    console.log(`📦 Worker ${workerIndex}: API=${apiPort}, Angular=${angularPort}, DB=${workerDatabase}`);
    
    // Store worker configuration for tests to access
    process.env[`WORKER_${workerIndex}_API_PORT`] = apiPort.toString();
    process.env[`WORKER_${workerIndex}_ANGULAR_PORT`] = angularPort.toString();
    process.env[`WORKER_${workerIndex}_DATABASE`] = workerDatabase;
  }
  
  // Set global environment variables
  process.env.TOTAL_WORKERS = workers.toString();
  process.env.PARALLEL_TESTING = 'true';
  
  console.log('✅ Global test setup completed');
}

export default globalSetup;