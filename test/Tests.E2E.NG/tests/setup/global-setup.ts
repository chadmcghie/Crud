import { chromium, FullConfig } from '@playwright/test';
import { checkPortsAvailable, killProcessOnPort, killAllTestServers } from './port-utils';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting global test setup...');
  
  // Clean up any leftover servers from previous test runs
  if (process.env.CLEANUP_BEFORE_TESTS === 'true') {
    console.log('🧹 Cleaning up leftover servers from previous test runs...');
    await killAllTestServers();
  }
  
  const workers = config.workers || 1;
  const baseApiPort = 5172;
  const baseAngularPort = 4200;
  
  console.log(`🔧 Configuring ${workers} parallel test workers`);
  
  // Pre-flight check: scan all ports that will be used
  const allPorts: number[] = [];
  for (let i = 0; i < workers; i++) {
    allPorts.push(baseApiPort + i);
    allPorts.push(baseAngularPort + (i * 10));
  }
  
  console.log(`🔍 Pre-flight check: Scanning ports ${allPorts.join(', ')}...`);
  const portCheck = await checkPortsAvailable(allPorts);
  
  if (!portCheck.available) {
    console.warn(`⚠️ Warning: The following ports are already in use: ${portCheck.conflicts.join(', ')}`);
    console.warn(`This may cause test failures. Consider:`);
    console.warn(`  1. Stopping any running servers`);
    console.warn(`  2. Setting KILL_EXISTING_SERVERS=true to auto-kill conflicting processes`);
    console.warn(`  3. Using different base ports`);
    
    if (process.env.KILL_EXISTING_SERVERS === 'true') {
      console.log(`🔪 KILL_EXISTING_SERVERS is set - attempting to free up ports...`);
      for (const port of portCheck.conflicts) {
        await killProcessOnPort(port);
      }
      
      // Re-check after killing
      const recheckPorts = await checkPortsAvailable(portCheck.conflicts);
      if (!recheckPorts.available) {
        console.error(`❌ Failed to free up all ports. Still in use: ${recheckPorts.conflicts.join(', ')}`);
      } else {
        console.log(`✅ All conflicting ports have been freed up`);
      }
    }
  } else {
    console.log(`✅ All required ports are available`);
  }
  
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