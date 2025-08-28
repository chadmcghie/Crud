import { FullConfig } from '@playwright/test';
import { cleanupAllServers } from './improved-server-manager';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

async function improvedGlobalTeardown(config: FullConfig) {
  console.log('🧹 Starting improved global test teardown...');
  
  // Only cleanup servers if explicitly requested
  const shouldCleanup = process.env.CLEANUP_AFTER_TESTS === 'true' || 
                       process.env.CI === 'true';
  
  if (shouldCleanup) {
    console.log('🛑 Stopping all test servers...');
    await cleanupAllServers();
  } else {
    console.log('ℹ️ Keeping servers running for potential reuse');
    console.log('ℹ️ Set CLEANUP_AFTER_TESTS=true to stop servers after tests');
    
    // Save server state for next run
    const tempDir = os.tmpdir();
    const stateFile = path.join(tempDir, 'crud-test-servers-state.json');
    
    if (fs.existsSync(stateFile)) {
      const state = JSON.parse(fs.readFileSync(stateFile, 'utf-8'));
      console.log(`ℹ️ Active servers saved in state file:`);
      for (const [index, server] of Object.entries(state.servers)) {
        const serverData = server as any;
        const uptime = ((Date.now() - serverData.startTime) / 1000 / 60).toFixed(1);
        console.log(`   - Parallel ${index}: Running for ${uptime} minutes`);
      }
    }
  }
  
  // Clean up old proxy configs
  const angularPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular');
  const files = fs.readdirSync(angularPath);
  const oldProxyConfigs = files.filter(f => 
    f.startsWith('proxy.') && 
    f.endsWith('.conf.json') &&
    !f.includes('conf.json.example')
  );
  
  if (oldProxyConfigs.length > 0) {
    console.log(`🧹 Cleaning up ${oldProxyConfigs.length} old proxy config files...`);
    for (const file of oldProxyConfigs) {
      try {
        fs.unlinkSync(path.join(angularPath, file));
      } catch (error) {
        // Ignore
      }
    }
  }
  
  // Clean up old database files (older than 1 hour)
  const tempDir = os.tmpdir();
  const tempFiles = fs.readdirSync(tempDir);
  const oldDatabases = tempFiles.filter(f => f.startsWith('CrudTest_') && f.endsWith('.db'));
  
  const oneHourAgo = Date.now() - (60 * 60 * 1000);
  let cleanedDbs = 0;
  
  for (const dbFile of oldDatabases) {
    const dbPath = path.join(tempDir, dbFile);
    try {
      const stats = fs.statSync(dbPath);
      if (stats.mtimeMs < oneHourAgo) {
        fs.unlinkSync(dbPath);
        cleanedDbs++;
      }
    } catch (error) {
      // Ignore
    }
  }
  
  if (cleanedDbs > 0) {
    console.log(`🧹 Cleaned up ${cleanedDbs} old database files`);
  }
  
  console.log('✅ Improved global test teardown completed');
}

export default improvedGlobalTeardown;