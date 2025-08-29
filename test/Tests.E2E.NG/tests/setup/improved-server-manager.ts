import { ChildProcess, spawn } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import fetch from 'node-fetch';
import { checkPortsAvailable, killProcessOnPort, findAvailablePort } from './port-utils';
import * as os from 'os';

interface ServerConfig {
  apiPort: number;
  angularPort: number;
  database: string;
  proxyConfigPath: string;
}

interface ServerInstance {
  config: ServerConfig;
  apiProcess: ChildProcess | null;
  angularProcess: ChildProcess | null;
  isHealthy: boolean;
  lastHealthCheck: number;
  startTime: number;
  parallelIndex: number;
}

// Global singleton for server management
class ImprovedServerManager {
  private static instance: ImprovedServerManager;
  private servers: Map<number, ServerInstance> = new Map();
  private readonly lockFile: string;
  private readonly stateFile: string;
  private readonly baseApiPort = 5172;
  private readonly baseAngularPort = 4200;
  private healthCheckInterval: NodeJS.Timeout | null = null;
  
  private constructor() {
    const tempDir = os.tmpdir();
    this.lockFile = path.join(tempDir, 'crud-test-servers.lock');
    this.stateFile = path.join(tempDir, 'crud-test-servers-state.json');
    this.loadState();
    this.startHealthMonitoring();
  }
  
  static getInstance(): ImprovedServerManager {
    if (!ImprovedServerManager.instance) {
      ImprovedServerManager.instance = new ImprovedServerManager();
    }
    return ImprovedServerManager.instance;
  }
  
  private loadState(): void {
    try {
      if (fs.existsSync(this.stateFile)) {
        const state = JSON.parse(fs.readFileSync(this.stateFile, 'utf-8'));
        // Verify servers are still running
        for (const [index, server] of Object.entries(state.servers)) {
          const serverData = server as any;
          const parallelIndex = parseInt(index);
          
          // Check if server is recent (started within last 10 minutes)
          if (Date.now() - serverData.startTime < 10 * 60 * 1000) {
            this.servers.set(parallelIndex, {
              config: serverData.config,
              apiProcess: null, // We don't have the process handle
              angularProcess: null,
              isHealthy: false, // Will be checked
              lastHealthCheck: 0,
              startTime: serverData.startTime,
              parallelIndex
            });
          }
        }
      }
    } catch (error) {
      console.warn('⚠️ Failed to load server state:', error);
    }
  }
  
  private saveState(): void {
    try {
      const state = {
        servers: Object.fromEntries(
          Array.from(this.servers.entries()).map(([index, server]) => [
            index,
            {
              config: server.config,
              startTime: server.startTime,
              isHealthy: server.isHealthy,
              lastHealthCheck: server.lastHealthCheck
            }
          ])
        ),
        lastUpdate: Date.now()
      };
      fs.writeFileSync(this.stateFile, JSON.stringify(state, null, 2));
    } catch (error) {
      console.warn('⚠️ Failed to save server state:', error);
    }
  }
  
  private startHealthMonitoring(): void {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }
    
    // Check server health every 30 seconds
    this.healthCheckInterval = setInterval(async () => {
      for (const [index, server] of this.servers.entries()) {
        if (Date.now() - server.lastHealthCheck > 30000) {
          await this.checkServerHealth(server);
        }
      }
    }, 30000);
  }
  
  private async checkServerHealth(server: ServerInstance): Promise<boolean> {
    try {
      // Check API health
      const apiResponse = await fetch(`http://localhost:${server.config.apiPort}/health`);
      if (!apiResponse.ok) {
        server.isHealthy = false;
        return false;
      }
      
      // Check Angular health
      const angularResponse = await fetch(`http://localhost:${server.config.angularPort}`);
      if (!angularResponse.ok) {
        server.isHealthy = false;
        return false;
      }
      
      server.isHealthy = true;
      server.lastHealthCheck = Date.now();
      return true;
    } catch (error) {
      server.isHealthy = false;
      return false;
    }
  }
  
  async getOrCreateServer(parallelIndex: number): Promise<ServerInstance> {
    // Try to reuse existing server
    const existing = this.servers.get(parallelIndex);
    if (existing && existing.isHealthy) {
      const isStillHealthy = await this.checkServerHealth(existing);
      if (isStillHealthy) {
        console.log(`♻️ Reusing healthy servers for parallel ${parallelIndex}`);
        return existing;
      }
    }
    
    // Create new server configuration
    const config: ServerConfig = {
      apiPort: this.baseApiPort + parallelIndex,
      angularPort: this.baseAngularPort + (parallelIndex * 10),
      database: this.createDatabasePath(parallelIndex),
      proxyConfigPath: path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.improved${parallelIndex}.conf.json`)
    };
    
    // Check for port conflicts
    const portsAvailable = await checkPortsAvailable([config.apiPort, config.angularPort]);
    if (!portsAvailable.available) {
      console.log(`⚠️ Ports in use for parallel ${parallelIndex}, finding alternatives...`);
      
      if (portsAvailable.conflicts.includes(config.apiPort)) {
        config.apiPort = await findAvailablePort(config.apiPort);
      }
      if (portsAvailable.conflicts.includes(config.angularPort)) {
        config.angularPort = await findAvailablePort(config.angularPort);
      }
    }
    
    // Create server instance
    const server: ServerInstance = {
      config,
      apiProcess: null,
      angularProcess: null,
      isHealthy: false,
      lastHealthCheck: 0,
      startTime: Date.now(),
      parallelIndex
    };
    
    // Start servers
    await this.startServer(server);
    
    // Store server
    this.servers.set(parallelIndex, server);
    this.saveState();
    
    return server;
  }
  
  private createDatabasePath(parallelIndex: number): string {
    const tempDir = os.tmpdir();
    const timestamp = Date.now();
    return path.join(tempDir, `CrudTest_Improved_P${parallelIndex}_${timestamp}.db`);
  }
  
  private async startServer(server: ServerInstance): Promise<void> {
    console.log(`🚀 Starting improved servers for parallel ${server.parallelIndex}...`);
    
    // Create proxy configuration
    this.createProxyConfig(server.config);
    
    // Start servers with better process management
    await Promise.all([
      this.startApiServer(server),
      this.startAngularServer(server)
    ]);
    
    // Wait for servers to be healthy
    await this.waitForHealthy(server);
    
    console.log(`✅ Servers ready for parallel ${server.parallelIndex}`);
  }
  
  private createProxyConfig(config: ServerConfig): void {
    const proxyConfig = {
      "/api": {
        "target": `http://localhost:${config.apiPort}`,
        "secure": false,
        "changeOrigin": true,
        "logLevel": "error", // Reduce log spam
        "timeout": 120000, // 2 minute timeout
        "proxyTimeout": 120000,
        "onProxyReq": {
          "retries": 3,
          "retryDelay": 1000
        }
      }
    };
    
    fs.writeFileSync(config.proxyConfigPath, JSON.stringify(proxyConfig, null, 2));
  }
  
  private async startApiServer(server: ServerInstance): Promise<void> {
    return new Promise((resolve, reject) => {
      const env = {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${server.config.apiPort}`,
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: `Data Source=${server.config.database}`,
        // Reduce logging to minimize output
        Logging__LogLevel__Default: 'Warning',
        Logging__LogLevel__Microsoft: 'Warning',
        Logging__LogLevel__System: 'Warning'
      };
      
      const apiPath = path.resolve(process.cwd(), '..', '..', 'src', 'Api');
      
      // Use detached mode on Windows to prevent terminal windows
      const isWindows = process.platform === 'win32';
      
      server.apiProcess = spawn(
        'dotnet',
        ['run', '--no-launch-profile', '--no-build'],
        {
          cwd: apiPath,
          env,
          stdio: ['ignore', 'pipe', 'pipe'], // Ignore stdin
          detached: isWindows,
          windowsHide: true, // Hide window on Windows
          shell: false
        }
      );
      
      if (isWindows && server.apiProcess.pid) {
        // On Windows, unref the process so it can run independently
        server.apiProcess.unref();
      }
      
      let started = false;
      const timeout = setTimeout(() => {
        if (!started) {
          reject(new Error(`API server timeout for parallel ${server.parallelIndex}`));
        }
      }, 120000);
      
      server.apiProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if ((output.includes('Now listening on:') || output.includes('Application started')) && !started) {
          started = true;
          clearTimeout(timeout);
          resolve();
        }
      });
      
      server.apiProcess.stderr?.on('data', (data) => {
        const error = data.toString();
        // Only log actual errors, not warnings
        if (error.includes('Error') || error.includes('Exception')) {
          console.error(`API Error (parallel ${server.parallelIndex}):`, error);
        }
      });
      
      server.apiProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
    });
  }
  
  private async startAngularServer(server: ServerInstance): Promise<void> {
    return new Promise((resolve, reject) => {
      const env = {
        ...process.env,
        NG_CLI_ANALYTICS: 'false',
        NODE_OPTIONS: '--max-old-space-size=4096',
        // Suppress Angular CLI output
        NG_CLI_ANALYTICS_SHARE: 'false'
      };
      
      const angularPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular');
      const proxyConfigName = path.basename(server.config.proxyConfigPath);
      
      const isWindows = process.platform === 'win32';
      const npmCommand = isWindows ? 'npm.cmd' : 'npm';
      
      const args = [
        'start', '--',
        `--port=${server.config.angularPort}`,
        `--proxy-config=${proxyConfigName}`,
        '--poll=5000', // Less frequent polling
        '--live-reload=false',
        '--hmr=false',
        '--progress=false', // Disable progress output
        '--no-open' // Don't open browser
      ];
      
      server.angularProcess = spawn(
        npmCommand,
        args,
        {
          cwd: angularPath,
          env,
          stdio: ['ignore', 'pipe', 'pipe'],
          detached: isWindows,
          windowsHide: true,
          shell: false
        }
      );
      
      if (isWindows && server.angularProcess.pid) {
        server.angularProcess.unref();
      }
      
      let started = false;
      const timeout = setTimeout(() => {
        if (!started) {
          reject(new Error(`Angular server timeout for parallel ${server.parallelIndex}`));
        }
      }, 300000);
      
      server.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if ((output.includes('webpack compiled') || 
             output.includes('Compiled successfully') ||
             output.includes('Build completed')) && !started) {
          started = true;
          clearTimeout(timeout);
          // Give Angular a moment to fully initialize
          setTimeout(() => resolve(), 2000);
        }
      });
      
      server.angularProcess.stderr?.on('data', (data) => {
        const error = data.toString();
        if (error.includes('Error') || error.includes('ERROR')) {
          console.error(`Angular Error (parallel ${server.parallelIndex}):`, error);
        }
      });
      
      server.angularProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
    });
  }
  
  private async waitForHealthy(server: ServerInstance): Promise<void> {
    const maxAttempts = 60;
    const delayMs = 2000;
    
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      const isHealthy = await this.checkServerHealth(server);
      if (isHealthy) {
        server.isHealthy = true;
        return;
      }
      
      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }
    
    throw new Error(`Servers failed health check for parallel ${server.parallelIndex}`);
  }
  
  async stopServer(parallelIndex: number): Promise<void> {
    const server = this.servers.get(parallelIndex);
    if (!server) return;
    
    console.log(`🛑 Stopping servers for parallel ${parallelIndex}...`);
    
    // Kill processes gracefully
    if (server.apiProcess) {
      try {
        if (process.platform === 'win32') {
          // On Windows, use taskkill for cleaner shutdown
          spawn('taskkill', ['/pid', server.apiProcess.pid!.toString(), '/f', '/t']);
        } else {
          server.apiProcess.kill('SIGTERM');
        }
      } catch (error) {
        console.warn(`Failed to stop API process:`, error);
      }
    }
    
    if (server.angularProcess) {
      try {
        if (process.platform === 'win32') {
          spawn('taskkill', ['/pid', server.angularProcess.pid!.toString(), '/f', '/t']);
        } else {
          server.angularProcess.kill('SIGTERM');
        }
      } catch (error) {
        console.warn(`Failed to stop Angular process:`, error);
      }
    }
    
    // Clean up ports
    await killProcessOnPort(server.config.apiPort);
    await killProcessOnPort(server.config.angularPort);
    
    // Clean up proxy config
    try {
      if (fs.existsSync(server.config.proxyConfigPath)) {
        fs.unlinkSync(server.config.proxyConfigPath);
      }
    } catch (error) {
      // Ignore
    }
    
    // Remove from map
    this.servers.delete(parallelIndex);
    this.saveState();
    
    console.log(`✅ Servers stopped for parallel ${parallelIndex}`);
  }
  
  async cleanup(): Promise<void> {
    console.log('🧹 Cleaning up all test servers...');
    
    // Stop health monitoring
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
      this.healthCheckInterval = null;
    }
    
    // Stop all servers
    const stopPromises = Array.from(this.servers.keys()).map(index => this.stopServer(index));
    await Promise.all(stopPromises);
    
    // Clean up state file
    try {
      if (fs.existsSync(this.stateFile)) {
        fs.unlinkSync(this.stateFile);
      }
      if (fs.existsSync(this.lockFile)) {
        fs.unlinkSync(this.lockFile);
      }
    } catch (error) {
      // Ignore
    }
    
    console.log('✅ Cleanup completed');
  }
  
  getServerUrl(parallelIndex: number, type: 'api' | 'angular'): string {
    const server = this.servers.get(parallelIndex);
    if (!server) {
      throw new Error(`No server found for parallel ${parallelIndex}`);
    }
    
    return type === 'api' 
      ? `http://localhost:${server.config.apiPort}`
      : `http://localhost:${server.config.angularPort}`;
  }
  
  getDatabase(parallelIndex: number): string {
    const server = this.servers.get(parallelIndex);
    if (!server) {
      throw new Error(`No server found for parallel ${parallelIndex}`);
    }
    
    return server.config.database;
  }
}

export { ImprovedServerManager };

// Export convenience functions
export async function ensureTestServer(parallelIndex: number): Promise<{
  apiUrl: string;
  angularUrl: string;
  database: string;
}> {
  const manager = ImprovedServerManager.getInstance();
  const server = await manager.getOrCreateServer(parallelIndex);
  
  return {
    apiUrl: manager.getServerUrl(parallelIndex, 'api'),
    angularUrl: manager.getServerUrl(parallelIndex, 'angular'),
    database: manager.getDatabase(parallelIndex)
  };
}

export async function stopTestServer(parallelIndex: number): Promise<void> {
  const manager = ImprovedServerManager.getInstance();
  await manager.stopServer(parallelIndex);
}

export async function cleanupAllServers(): Promise<void> {
  const manager = ImprovedServerManager.getInstance();
  await manager.cleanup();
}