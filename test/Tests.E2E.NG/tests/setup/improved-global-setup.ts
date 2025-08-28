import { FullConfig } from '@playwright/test';
import { ImprovedServerManager, cleanupAllServers } from './improved-server-manager';
import { checkPortsAvailable, killAllTestServers } from './port-utils';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

async function improvedGlobalSetup(config: FullConfig) {
  console.log('🚀 Starting improved global test setup...');
  
  // Clean up any leftover servers from previous test runs
  if (process.env.CLEANUP_BEFORE_TESTS === 'true') {
    console.log('🧹 Cleaning up leftover servers from previous test runs...');
    await killAllTestServers();
    await cleanupAllServers();
  }
  
  const workers = config.workers || 1;
  const baseApiPort = 5172;
  const baseAngularPort = 4200;
  
  console.log(`🔧 Configuring ${workers} parallel test workers with improved server management`);
  
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
    
    // Check if these are our own servers
    const manager = ImprovedServerManager.getInstance();
    const tempDir = os.tmpdir();
    const stateFile = path.join(tempDir, 'crud-test-servers-state.json');
    
    if (fs.existsSync(stateFile)) {
      console.log(`ℹ️ Found existing server state file, servers may be reused`);
    } else {
      console.warn(`This may cause test failures. Consider:`);
      console.warn(`  1. Stopping any running servers`);
      console.warn(`  2. Setting CLEANUP_BEFORE_TESTS=true to auto-cleanup`);
    }
  } else {
    console.log(`✅ All required ports are available`);
  }
  
  // Pre-warm servers for better performance (optional)
  if (process.env.PREWARM_SERVERS === 'true') {
    console.log(`🔥 Pre-warming ${workers} servers...`);
    const manager = ImprovedServerManager.getInstance();
    
    const prewarmPromises = [];
    for (let i = 0; i < workers; i++) {
      prewarmPromises.push(
        manager.getOrCreateServer(i).catch(error => {
          console.warn(`⚠️ Failed to pre-warm server ${i}:`, error);
        })
      );
    }
    
    await Promise.all(prewarmPromises);
    console.log(`✅ Server pre-warming completed`);
  }
  
  // Set global environment variables
  process.env.TOTAL_WORKERS = workers.toString();
  process.env.PARALLEL_TESTING = 'true';
  process.env.USE_IMPROVED_SERVERS = 'true';
  
  // Create a cleanup handler for unexpected exits
  const cleanupHandler = async (signal: string) => {
    console.log(`\n🛑 Received ${signal}, cleaning up servers...`);
    await cleanupAllServers();
    process.exit(0);
  };
  
  process.on('SIGINT', () => cleanupHandler('SIGINT'));
  process.on('SIGTERM', () => cleanupHandler('SIGTERM'));
  
  console.log('✅ Improved global test setup completed');
  console.log('ℹ️ Servers will be started on-demand and reused across test runs');
}

export default improvedGlobalSetup;