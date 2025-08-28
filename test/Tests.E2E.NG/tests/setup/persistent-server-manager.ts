import { ChildProcess, spawn } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import fetch from 'node-fetch';
import { checkPortsAvailable, killProcessOnPort } from './port-utils';

interface ServerState {
  apiPort: number;
  angularPort: number;
  database: string;
  startTime: number;
  pid?: number;
}

interface LockFile {
  servers: Map<number, ServerState>;
  lastUpdate: number;
}

const LOCK_FILE_PATH = path.join(process.cwd(), '.test-servers.lock');
const SERVER_TIMEOUT = 5 * 60 * 1000; // 5 minutes

export class PersistentServerManager {
  private parallelIndex: number;
  private apiPort: number;
  private angularPort: number;
  private database: string;
  private apiProcess: ChildProcess | null = null;
  private angularProcess: ChildProcess | null = null;
  
  constructor(parallelIndex: number) {
    this.parallelIndex = parallelIndex;
    this.apiPort = 5172 + parallelIndex;
    this.angularPort = 4200 + (parallelIndex * 10);
    
    const timestamp = Date.now();
    const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
    this.database = path.join(tempDir, `CrudTest_P${parallelIndex}_${timestamp}.db`);
  }
  
  private readLockFile(): LockFile {
    try {
      if (fs.existsSync(LOCK_FILE_PATH)) {
        const content = fs.readFileSync(LOCK_FILE_PATH, 'utf-8');
        const parsed = JSON.parse(content);
        return {
          servers: new Map(Object.entries(parsed.servers).map(([k, v]) => [parseInt(k), v as ServerState])),
          lastUpdate: parsed.lastUpdate
        };
      }
    } catch (error) {
      console.warn('⚠️ Failed to read lock file:', error);
    }
    return { servers: new Map(), lastUpdate: Date.now() };
  }
  
  private writeLockFile(lock: LockFile): void {
    try {
      const serializable = {
        servers: Object.fromEntries(lock.servers),
        lastUpdate: Date.now()
      };
      fs.writeFileSync(LOCK_FILE_PATH, JSON.stringify(serializable, null, 2));
    } catch (error) {
      console.warn('⚠️ Failed to write lock file:', error);
    }
  }
  
  private async isServerRunning(port: number, isAngular: boolean = false): Promise<boolean> {
    try {
      const url = isAngular ? `http://localhost:${port}` : `http://localhost:${port}/health`;
      const response = await fetch(url);
      if (response.ok) {
        if (isAngular) {
          // Check if it's actually Angular
          const text = await response.text();
          return text.includes('<app-root>') || text.includes('angular') || text.includes('<!doctype html>');
        }
        return true;
      }
      return false;
    } catch {
      return false;
    }
  }
  
  async ensureServers(): Promise<{ apiUrl: string; angularUrl: string; database: string }> {
    const lock = this.readLockFile();
    const existing = lock.servers.get(this.parallelIndex);
    
    // Check if we have a recent, running server
    if (existing && (Date.now() - existing.startTime < SERVER_TIMEOUT)) {
      // Verify servers are actually running
      const [apiRunning, angularRunning] = await Promise.all([
        this.isServerRunning(existing.apiPort, false),
        this.isServerRunning(existing.angularPort, true)
      ]);
      
      if (apiRunning && angularRunning) {
        console.log(`♻️ Reusing existing servers for parallel ${this.parallelIndex}: API=${existing.apiPort}, Angular=${existing.angularPort}`);
        return {
          apiUrl: `http://localhost:${existing.apiPort}`,
          angularUrl: `http://localhost:${existing.angularPort}`,
          database: existing.database
        };
      }
      
      console.log(`⚠️ Existing servers for parallel ${this.parallelIndex} are not responding, starting new ones...`);
    }
    
    // Start new servers
    console.log(`🚀 Starting new servers for parallel ${this.parallelIndex}...`);
    
    // Check port availability
    const portsToCheck = [this.apiPort, this.angularPort];
    const portCheck = await checkPortsAvailable(portsToCheck);
    
    if (!portCheck.available) {
      console.log(`⚠️ Ports in use: ${portCheck.conflicts.join(', ')}, killing existing processes...`);
      for (const port of portCheck.conflicts) {
        await killProcessOnPort(port);
      }
    }
    
    // Create proxy config
    this.createProxyConfig();
    
    // Start servers
    await Promise.all([
      this.startApiServer(),
      this.startAngularServer()
    ]);
    
    // Update lock file
    lock.servers.set(this.parallelIndex, {
      apiPort: this.apiPort,
      angularPort: this.angularPort,
      database: this.database,
      startTime: Date.now()
    });
    this.writeLockFile(lock);
    
    console.log(`✅ Servers ready for parallel ${this.parallelIndex}: API=${this.apiPort}, Angular=${this.angularPort}`);
    
    return {
      apiUrl: `http://localhost:${this.apiPort}`,
      angularUrl: `http://localhost:${this.angularPort}`,
      database: this.database
    };
  }
  
  private createProxyConfig(): void {
    const proxyConfig = {
      "/api": {
        "target": `http://localhost:${this.apiPort}`,
        "secure": false,
        "changeOrigin": true,
        "logLevel": "debug"
      }
    };
    
    const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.p${this.parallelIndex}.conf.json`);
    fs.writeFileSync(proxyPath, JSON.stringify(proxyConfig, null, 2));
  }
  
  private async startApiServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const apiEnv = {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${this.apiPort}`,
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: `Data Source=${this.database}`
      };
      
      const apiPath = path.resolve(process.cwd(), '..', '..', 'src', 'Api');
      
      this.apiProcess = spawn('dotnet', ['run', '--no-launch-profile'], {
        cwd: apiPath,
        env: apiEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: true,
        detached: true,  // Allow process to persist
        windowsHide: true  // Hide console window on Windows
      });
      
      let started = false;
      const timeout = setTimeout(() => {
        if (!started) reject(new Error(`API server timeout`));
      }, 120000);
      
      this.apiProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if ((output.includes('Now listening on:') || output.includes('Application started')) && !started) {
          started = true;
          clearTimeout(timeout);
          this.waitForHealth(this.apiPort, 'API').then(resolve).catch(reject);
        }
      });
      
      this.apiProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
      
      // Unref to allow process to persist
      this.apiProcess.unref();
    });
  }
  
  private async startAngularServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const angularEnv = {
        ...process.env,
        NG_CLI_ANALYTICS: 'false',
        NODE_OPTIONS: '--max-old-space-size=4096'
      };
      
      const angularPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular');
      const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
      
      const args = [
        'start', '--',
        `--port=${this.angularPort}`,
        `--proxy-config=proxy.p${this.parallelIndex}.conf.json`,
        '--poll=2000',
        '--live-reload=false',
        '--hmr=false'
      ];
      
      this.angularProcess = spawn(npmCommand, args, {
        cwd: angularPath,
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: false,
        detached: true,  // Allow process to persist
        windowsHide: true  // Hide console window on Windows
      });
      
      let started = false;
      const timeout = setTimeout(() => {
        if (!started) reject(new Error(`Angular server timeout`));
      }, 300000);
      
      this.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if ((output.includes('webpack compiled') || 
             output.includes('Compiled successfully') ||
             output.includes('Build completed')) && !started) {
          started = true;
          clearTimeout(timeout);
          setTimeout(() => {
            this.waitForHealth(this.angularPort, 'Angular').then(resolve).catch(reject);
          }, 2000);
        }
      });
      
      this.angularProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
      
      // Unref to allow process to persist
      this.angularProcess.unref();
    });
  }
  
  private async waitForHealth(port: number, name: string): Promise<void> {
    const maxAttempts = 30;
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${port}${name === 'API' ? '/health' : ''}`);
        if (response.ok) {
          console.log(`✅ ${name} health check passed on port ${port}`);
          return;
        }
      } catch {
        // Not ready yet
      }
      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }
    throw new Error(`${name} health check failed on port ${port}`);
  }
  
  static async cleanupAll(): Promise<void> {
    console.log('🧹 Cleaning up all test servers...');
    
    // Read lock file to get all server ports
    try {
      if (fs.existsSync(LOCK_FILE_PATH)) {
        const content = fs.readFileSync(LOCK_FILE_PATH, 'utf-8');
        const lock = JSON.parse(content);
        
        for (const [index, server] of Object.entries(lock.servers)) {
          const state = server as ServerState;
          console.log(`🛑 Stopping servers for parallel ${index}...`);
          
          await killProcessOnPort(state.apiPort);
          await killProcessOnPort(state.angularPort);
          
          // Clean up proxy config
          const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.p${index}.conf.json`);
          if (fs.existsSync(proxyPath)) {
            fs.unlinkSync(proxyPath);
          }
        }
        
        // Remove lock file
        fs.unlinkSync(LOCK_FILE_PATH);
      }
    } catch (error) {
      console.warn('⚠️ Error during cleanup:', error);
    }
  }
}