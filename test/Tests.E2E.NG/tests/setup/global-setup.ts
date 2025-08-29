import { FullConfig } from '@playwright/test';
import { checkPortsAvailable, killProcessOnPort, killAllTestServers } from './port-utils';
import { PersistentServerManager } from './persistent-server-manager';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting global test setup...');
  
  // Clean up any leftover servers from previous test runs
  if (process.env.CLEANUP_BEFORE_TESTS === 'true') {
    console.log('🧹 Cleaning up leftover servers from previous test runs...');
    await killAllTestServers();
  }
  
  // For serial execution, we only need one set of servers
  const apiPort = 5172;
  const angularPort = 4200;
  
  console.log(`🔧 Configuring serial test execution (1 worker)`);
  
  // Pre-flight check: scan ports
  const allPorts = [apiPort, angularPort];
  console.log(`🔍 Pre-flight check: Scanning ports ${allPorts.join(', ')}...`);
  const portCheck = await checkPortsAvailable(allPorts);
  
  if (!portCheck.available) {
    console.warn(`⚠️ Warning: The following ports are already in use: ${portCheck.conflicts.join(', ')}`);
    console.warn(`This may cause test failures. Consider:`);
    console.warn(`  1. Stopping any running servers`);
    console.warn(`  2. Setting KILL_EXISTING_SERVERS=true to auto-kill conflicting processes`);
    
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
  
  // Always start servers in global setup for serial execution
  console.log('🚀 Starting servers in global setup...');
  const manager = PersistentServerManager.getInstance();
  const serverInfo = await manager.ensureServers();
  console.log(`✅ Servers started and ready: API=${serverInfo.apiUrl}, Angular=${serverInfo.angularUrl}`);
  console.log(`📁 Database: ${serverInfo.database}`);
  
  // Set environment variables for serial execution
  process.env.SERIAL_TESTING = 'true';
  process.env.API_PORT = apiPort.toString();
  process.env.ANGULAR_PORT = angularPort.toString();
  
  console.log('✅ Global test setup completed');
}

export default globalSetup;