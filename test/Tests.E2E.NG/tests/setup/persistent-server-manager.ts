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

const LOCK_FILE_PATH = path.join(process.cwd(), '.test-servers.lock');
const SERVER_TIMEOUT = 10 * 60 * 1000; // 10 minutes

export class PersistentServerManager {
  private static instance: PersistentServerManager | null = null;
  private apiPort: number = 5172;
  private angularPort: number = 4200;
  private database: string;
  private apiProcess: ChildProcess | null = null;
  private angularProcess: ChildProcess | null = null;
  private serverInfo: { apiUrl: string; angularUrl: string; database: string } | null = null;
  
  private constructor() {
    const timestamp = Date.now();
    const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
    this.database = path.join(tempDir, `CrudTest_Serial_${timestamp}.db`);
  }
  
  static getInstance(): PersistentServerManager {
    if (!this.instance) {
      this.instance = new PersistentServerManager();
    }
    return this.instance;
  }
  
  private readLockFile(): ServerState | null {
    try {
      if (fs.existsSync(LOCK_FILE_PATH)) {
        const content = fs.readFileSync(LOCK_FILE_PATH, 'utf-8');
        return JSON.parse(content);
      }
    } catch (error) {
      console.warn('⚠️ Failed to read lock file:', error);
    }
    return null;
  }
  
  private writeLockFile(state: ServerState): void {
    try {
      fs.writeFileSync(LOCK_FILE_PATH, JSON.stringify(state, null, 2));
    } catch (error) {
      console.warn('⚠️ Failed to write lock file:', error);
    }
  }
  
  private async isServerRunning(port: number, isAngular: boolean = false): Promise<boolean> {
    try {
      const url = isAngular ? `http://localhost:${port}` : `http://localhost:${port}/health`;
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 5000);
      
      try {
        const response = await fetch(url, { signal: controller.signal });
        clearTimeout(timeout);
        
        if (response.ok) {
          if (isAngular) {
            const text = await response.text();
            return text.includes('<app-root>') || text.includes('angular') || text.includes('<!doctype html>');
          }
          return true;
        }
        return false;
      } catch {
        clearTimeout(timeout);
        return false;
      }
    } catch {
      return false;
    }
  }
  
  async ensureServers(): Promise<{ apiUrl: string; angularUrl: string; database: string }> {
    console.log(`📍 Lock file path: ${LOCK_FILE_PATH}`);
    
    // If we already have server info cached in memory, return it
    if (this.serverInfo) {
      console.log(`♻️ Using cached server info`);
      return this.serverInfo;
    }
    
    // Check if servers are already running from a previous run
    const existing = this.readLockFile();
    console.log(`📄 Lock file exists: ${existing !== null}, content:`, existing);
    
    if (existing && (Date.now() - existing.startTime < SERVER_TIMEOUT)) {
      console.log(`🔍 Checking existing servers from lock file...`);
      
      const [apiRunning, angularRunning] = await Promise.all([
        this.isServerRunning(existing.apiPort, false),
        this.isServerRunning(existing.angularPort, true)
      ]);
      
      console.log(`🔍 Server status - API: ${apiRunning}, Angular: ${angularRunning}`);
      
      if (apiRunning && angularRunning) {
        console.log(`♻️ Reusing existing servers: API=${existing.apiPort}, Angular=${existing.angularPort}`);
        this.serverInfo = {
          apiUrl: `http://localhost:${existing.apiPort}`,
          angularUrl: `http://localhost:${existing.angularPort}`,
          database: existing.database
        };
        this.database = existing.database; // Use existing database
        return this.serverInfo;
      }
      
      console.log(`⚠️ Existing servers are not responding, starting new ones...`);
    } else if (existing) {
      console.log(`⏰ Lock file too old (${Date.now() - existing.startTime}ms old)`);
    }
    
    // Start new servers
    await this.startNewServers();
    
    this.serverInfo = {
      apiUrl: `http://localhost:${this.apiPort}`,
      angularUrl: `http://localhost:${this.angularPort}`,
      database: this.database
    };
    
    return this.serverInfo;
  }
  
  private async startNewServers(): Promise<void> {
    console.log(`🚀 Starting new servers...`);
    
    // Always kill any processes on our ports first to ensure clean slate
    console.log(`🧹 Ensuring ports ${this.apiPort} and ${this.angularPort} are free...`);
    
    try {
      await killProcessOnPort(this.apiPort);
    } catch {
      // Port might already be free
    }
    
    try {
      await killProcessOnPort(this.angularPort);
    } catch {
      // Port might already be free
    }
    
    // Wait for processes to fully terminate
    await new Promise(resolve => setTimeout(resolve, 3000));
    
    // Create proxy config
    this.createProxyConfig();
    
    // Start servers in parallel
    await Promise.all([
      this.startApiServer(),
      this.startAngularServer()
    ]);
    
    // Write lock file
    this.writeLockFile({
      apiPort: this.apiPort,
      angularPort: this.angularPort,
      database: this.database,
      startTime: Date.now()
    });
    
    console.log(`✅ Servers ready: API=${this.apiPort}, Angular=${this.angularPort}`);
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
    
    const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.conf.json`);
    fs.writeFileSync(proxyPath, JSON.stringify(proxyConfig, null, 2));
  }
  
  private async startApiServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const apiPath = path.resolve(process.cwd(), '..', '..', 'src', 'Api');
      
      // On Windows with shell:true, we need to set env vars inline
      const command = process.platform === 'win32' 
        ? `set ASPNETCORE_ENVIRONMENT=Development && set ASPNETCORE_URLS=http://localhost:${this.apiPort} && set DatabaseProvider=SQLite && set "ConnectionStrings__DefaultConnection=Data Source=${this.database}" && dotnet run --no-launch-profile`
        : 'dotnet';
      
      const args = process.platform === 'win32' ? [] : ['run', '--no-launch-profile'];
      
      const apiEnv = process.platform === 'win32' ? process.env : {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${this.apiPort}`,
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: `Data Source=${this.database}`
      };
      
      this.apiProcess = spawn(command, args, {
        cwd: apiPath,
        env: apiEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: true,
        detached: true,
        windowsHide: false  // Show console window (user requested visibility)
      });
      
      let started = false;
      const timeout = setTimeout(() => {
        if (!started) reject(new Error(`API server timeout`));
      }, 120000);
      
      // Buffer to accumulate output
      let outputBuffer = '';
      
      this.apiProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        outputBuffer += output;
        console.log(`API output: ${output.substring(0, 200)}`);
        
        // Check for various startup indicators
        if ((outputBuffer.includes('Now listening on:') || 
             outputBuffer.includes('Application started') || 
             outputBuffer.includes('Content root path:') ||
             outputBuffer.includes('service.name: Crud.Api') ||
             outputBuffer.includes('telemetry.sdk.version')) && !started) {
          started = true;
          clearTimeout(timeout);
          console.log(`API server detected as started, waiting for health check...`);
          // Give it a moment to fully start - API needs more time after telemetry output
          setTimeout(() => {
            this.waitForHealth(this.apiPort, 'API').then(resolve).catch(reject);
          }, 10000);
        }
      });
      
      this.apiProcess.stderr?.on('data', (data) => {
        console.error(`API Error: ${data}`);
      });
      
      this.apiProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
      
      this.apiProcess.unref();
    });
  }
  
  private async startAngularServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const angularEnv = {
        ...process.env,
        NG_CLI_ANALYTICS: 'false',
        NG_CLI_COMPLETION: 'false',  // Disable autocompletion prompt
        NG_DISABLE_VERSION_CHECK: 'true',
        NODE_OPTIONS: '--max-old-space-size=4096',
        CI: 'true'  // This should prevent ALL interactive prompts
      };
      
      const angularPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular');
      const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
      
      const args = [
        'start', '--',
        `--port=${this.angularPort}`,
        '--poll=2000',
        '--live-reload=false',
        '--hmr=false'
      ];
      
      this.angularProcess = spawn(npmCommand, args, {
        cwd: angularPath,
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: process.platform === 'win32',  // Windows needs shell for npm.cmd
        detached: true,
        windowsHide: false  // Show console window (user requested visibility)
      });
      
      // Continuously write 'n' to stdin to handle any prompts
      if (this.angularProcess.stdin) {
        // Send 'n' immediately
        this.angularProcess.stdin.write('n\n');
        // Keep sending 'n' every second for the first 10 seconds
        const promptHandler = setInterval(() => {
          if (this.angularProcess?.stdin?.writable) {
            this.angularProcess.stdin.write('n\n');
          }
        }, 1000);
        setTimeout(() => clearInterval(promptHandler), 10000);
      }
      
      let started = false;
      const timeout = setTimeout(() => {
        if (!started) reject(new Error(`Angular server timeout`));
      }, 300000);
      
      this.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        console.log(`Angular output: ${output.substring(0, 100)}`);
        if ((output.includes('webpack compiled') || 
             output.includes('Compiled successfully') ||
             output.includes('Build completed') ||
             output.includes('Angular Live Development Server')) && !started) {
          started = true;
          clearTimeout(timeout);
          console.log(`Angular server detected as started, waiting for health check...`);
          setTimeout(() => {
            this.waitForHealth(this.angularPort, 'Angular').then(resolve).catch(reject);
          }, 5000);
        }
      });
      
      this.angularProcess.stderr?.on('data', (data) => {
        console.error(`Angular Error: ${data}`);
      });
      
      this.angularProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
      
      this.angularProcess.unref();
    });
  }
  
  private async waitForHealth(port: number, name: string): Promise<void> {
    const maxAttempts = 60;  // Increased for slower Windows startup
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 3000);
        
        const response = await fetch(`http://localhost:${port}${name === 'API' ? '/health' : ''}`, {
          signal: controller.signal
        });
        clearTimeout(timeout);
        
        if (response.ok) {
          console.log(`✅ ${name} health check passed on port ${port}`);
          return;
        }
      } catch (error) {
        // Not ready yet
        if (attempt % 10 === 0) {
          console.log(`⏳ Still waiting for ${name} on port ${port} (attempt ${attempt}/${maxAttempts})...`);
        }
      }
      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }
    throw new Error(`${name} health check failed on port ${port}`);
  }
  
  async cleanDatabase(): Promise<void> {
    console.log(`🧹 Cleaning database: ${this.database}`);
    
    try {
      // Close existing connections by calling a reset endpoint
      const response = await fetch(`http://localhost:${this.apiPort}/api/database/reset`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ force: true })
      });
      
      if (!response.ok) {
        console.warn(`⚠️ Database reset endpoint failed: ${response.status}`);
      }
    } catch (error) {
      console.warn(`⚠️ Could not call database reset endpoint:`, error);
    }
    
    // For SQLite, we can also try to delete and recreate the file
    try {
      if (fs.existsSync(this.database)) {
        fs.unlinkSync(this.database);
        console.log(`✅ Database file deleted`);
      }
    } catch (error) {
      console.warn(`⚠️ Could not delete database file:`, error);
    }
  }
  
  static async cleanupAll(): Promise<void> {
    console.log('🧹 Cleaning up all test servers...');
    
    try {
      const existing = PersistentServerManager.instance?.readLockFile();
      
      if (existing) {
        console.log(`🛑 Stopping servers...`);
        
        await killProcessOnPort(existing.apiPort);
        await killProcessOnPort(existing.angularPort);
        
        // Clean up proxy config
        const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', 'proxy.conf.json');
        if (fs.existsSync(proxyPath)) {
          fs.unlinkSync(proxyPath);
        }
      }
      
      // Remove lock file
      if (fs.existsSync(LOCK_FILE_PATH)) {
        fs.unlinkSync(LOCK_FILE_PATH);
      }
    } catch (error) {
      console.warn('⚠️ Error during cleanup:', error);
    }
  }
}