import { ChildProcess, spawn } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import fetch from 'node-fetch';
import { checkPortsAvailable, killProcessOnPort, findAvailablePort } from './port-utils';

interface ServerInstance {
  apiProcess: ChildProcess | null;
  angularProcess: ChildProcess | null;
  apiPort: number;
  angularPort: number;
  database: string;
  isStarting: boolean;
  isStarted: boolean;
  startPromise?: Promise<void>;
}

// Use global to persist across Playwright worker restarts
declare global {
  var __serverPool: Map<number, ServerInstance> | undefined;
}

class ServerPoolManager {
  private static instance: ServerPoolManager;
  private servers: Map<number, ServerInstance>;
  private baseApiPort = 5172;
  private baseAngularPort = 4200;
  
  private constructor() {
    // Use global map to persist servers across worker restarts
    if (!global.__serverPool) {
      global.__serverPool = new Map();
      console.log('🔧 Initializing global server pool');
    }
    this.servers = global.__serverPool;
  }
  
  static getInstance(): ServerPoolManager {
    if (!ServerPoolManager.instance) {
      ServerPoolManager.instance = new ServerPoolManager();
    }
    return ServerPoolManager.instance;
  }
  
  async getServer(parallelIndex: number): Promise<ServerInstance> {
    console.log(`📊 ServerPool: Checking for server at parallelIndex ${parallelIndex}, current pool size: ${this.servers.size}`);
    console.log(`📊 ServerPool keys: ${Array.from(this.servers.keys()).join(', ')}`);
    
    // Check if server already exists and is started
    const existing = this.servers.get(parallelIndex);
    if (existing) {
      console.log(`📊 Found existing server for parallel ${parallelIndex}: isStarted=${existing.isStarted}, isStarting=${existing.isStarting}`);
      if (existing.isStarted) {
        console.log(`♻️ Reusing existing servers for parallel worker ${parallelIndex}`);
        return existing;
      }
      
      // If server is currently starting, wait for it
      if (existing.isStarting && existing.startPromise) {
        console.log(`⏳ Waiting for servers to start for parallel worker ${parallelIndex}`);
        await existing.startPromise;
        return existing;
      }
    }
    
    // Create new server instance
    console.log(`🆕 Creating new servers for parallel worker ${parallelIndex}`);
    const server: ServerInstance = {
      apiProcess: null,
      angularProcess: null,
      apiPort: this.baseApiPort + parallelIndex,
      angularPort: this.baseAngularPort + (parallelIndex * 10),
      database: this.createDatabasePath(parallelIndex),
      isStarting: true,
      isStarted: false
    };
    
    this.servers.set(parallelIndex, server);
    
    // Start the servers
    server.startPromise = this.startServers(server, parallelIndex)
      .then(() => {
        server.isStarting = false;
        server.isStarted = true;
      })
      .catch((error) => {
        // Remove from map if startup failed
        this.servers.delete(parallelIndex);
        throw error;
      });
    
    await server.startPromise;
    return server;
  }
  
  private createDatabasePath(parallelIndex: number): string {
    const timestamp = Date.now();
    const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
    return path.join(tempDir, `CrudTest_Parallel${parallelIndex}_${timestamp}.db`);
  }
  
  private async startServers(server: ServerInstance, parallelIndex: number): Promise<void> {
    console.log(`🚀 Starting servers for parallel worker ${parallelIndex}...`);
    
    // Check and handle port conflicts
    await this.ensurePortsAvailable(server);
    
    // Create proxy config for Angular
    this.createProxyConfig(server.apiPort, parallelIndex);
    
    // Start servers in parallel
    const [apiStarted, angularStarted] = await Promise.all([
      this.startApiServer(server, parallelIndex),
      this.startAngularServer(server, parallelIndex)
    ]);
    
    console.log(`✅ Servers ready for parallel worker ${parallelIndex}: API=${server.apiPort}, Angular=${server.angularPort}`);
  }
  
  private async ensurePortsAvailable(server: ServerInstance): Promise<void> {
    const portsToCheck = [server.apiPort, server.angularPort];
    const portCheck = await checkPortsAvailable(portsToCheck);
    
    if (!portCheck.available) {
      console.log(`⚠️ Ports in use: ${portCheck.conflicts.join(', ')}`);
      
      if (process.env.KILL_EXISTING_SERVERS === 'true') {
        for (const port of portCheck.conflicts) {
          await killProcessOnPort(port);
        }
      } else {
        // Find alternative ports
        if (portCheck.conflicts.includes(server.apiPort)) {
          server.apiPort = await findAvailablePort(server.apiPort);
          console.log(`📍 Using alternative API port: ${server.apiPort}`);
        }
        if (portCheck.conflicts.includes(server.angularPort)) {
          server.angularPort = await findAvailablePort(server.angularPort);
          console.log(`📍 Using alternative Angular port: ${server.angularPort}`);
        }
      }
    }
  }
  
  private createProxyConfig(apiPort: number, parallelIndex: number): void {
    const proxyConfig = {
      "/api": {
        "target": `http://localhost:${apiPort}`,
        "secure": false,
        "changeOrigin": true,
        "logLevel": "debug"
      }
    };
    
    const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.parallel${parallelIndex}.conf.json`);
    fs.writeFileSync(proxyPath, JSON.stringify(proxyConfig, null, 2));
  }
  
  private async startApiServer(server: ServerInstance, parallelIndex: number): Promise<void> {
    return new Promise((resolve, reject) => {
      const apiEnv = {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${server.apiPort}`,
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: `Data Source=${server.database}`
      };
      
      const apiPath = path.resolve(process.cwd(), '..', '..', 'src', 'Api');
      
      server.apiProcess = spawn('dotnet', ['run', '--no-launch-profile'], {
        cwd: apiPath,
        env: apiEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: true,
        windowsHide: true  // Hide console window on Windows
      });
      
      let apiStarted = false;
      const timeout = setTimeout(() => {
        if (!apiStarted) {
          reject(new Error(`API server timeout for parallel worker ${parallelIndex}`));
        }
      }, 120000);
      
      server.apiProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if ((output.includes('Now listening on:') || output.includes('Application started')) && !apiStarted) {
          apiStarted = true;
          clearTimeout(timeout);
          this.waitForApiHealth(server.apiPort, parallelIndex).then(resolve).catch(reject);
        }
      });
      
      server.apiProcess.stderr?.on('data', (data) => {
        console.error(`API parallel ${parallelIndex} stderr:`, data.toString());
      });
      
      server.apiProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
    });
  }
  
  private async startAngularServer(server: ServerInstance, parallelIndex: number): Promise<void> {
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
        `--port=${server.angularPort}`,
        `--proxy-config=proxy.parallel${parallelIndex}.conf.json`,
        '--poll=2000',
        '--live-reload=false',
        '--hmr=false'
      ];
      
      server.angularProcess = spawn(npmCommand, args, {
        cwd: angularPath,
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: false,
        windowsHide: true  // Hide console window on Windows
      });
      
      let angularStarted = false;
      const timeout = setTimeout(() => {
        if (!angularStarted) {
          reject(new Error(`Angular server timeout for parallel worker ${parallelIndex}`));
        }
      }, 300000);
      
      server.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if (output.includes('webpack compiled') || 
            output.includes('Compiled successfully') ||
            output.includes('Build completed')) {
          if (!angularStarted) {
            angularStarted = true;
            clearTimeout(timeout);
            setTimeout(() => {
              this.waitForAngularHealth(server.angularPort, parallelIndex).then(resolve).catch(reject);
            }, 2000);
          }
        }
      });
      
      server.angularProcess.stderr?.on('data', (data) => {
        const errorOutput = data.toString();
        if (errorOutput.includes('Error:') || errorOutput.includes('ERROR')) {
          console.error(`⚠️ Angular parallel ${parallelIndex} error:`, errorOutput);
        }
      });
      
      server.angularProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(error);
      });
    });
  }
  
  private async waitForApiHealth(port: number, parallelIndex: number): Promise<void> {
    const maxAttempts = 30;
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${port}/health`);
        if (response.ok) {
          console.log(`✅ API health check passed for parallel worker ${parallelIndex}`);
          return;
        }
      } catch (error) {
        // Not ready yet
      }
      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }
    throw new Error(`API health check failed for parallel worker ${parallelIndex}`);
  }
  
  private async waitForAngularHealth(port: number, parallelIndex: number): Promise<void> {
    const maxAttempts = 60;
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${port}`);
        if (response.ok) {
          const text = await response.text();
          if (text.includes('<app-root>') || text.includes('angular') || text.includes('<!doctype html>')) {
            console.log(`✅ Angular health check passed for parallel worker ${parallelIndex}`);
            return;
          }
        }
      } catch (error) {
        // Not ready yet
      }
      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }
    throw new Error(`Angular health check failed for parallel worker ${parallelIndex}`);
  }
  
  async stopServer(parallelIndex: number): Promise<void> {
    const server = this.servers.get(parallelIndex);
    if (!server) return;
    
    console.log(`🛑 Stopping servers for parallel worker ${parallelIndex}...`);
    
    const killSignal = process.platform === 'win32' ? 'SIGKILL' : 'SIGTERM';
    
    if (server.apiProcess) {
      try {
        server.apiProcess.kill(killSignal);
        await new Promise(resolve => setTimeout(resolve, 500));
        if (server.apiProcess && !server.apiProcess.killed) {
          server.apiProcess.kill('SIGKILL');
        }
      } catch (error) {
        console.warn(`⚠️ Error killing API process:`, error);
      }
    }
    
    if (server.angularProcess) {
      try {
        server.angularProcess.kill(killSignal);
        await new Promise(resolve => setTimeout(resolve, 500));
        if (server.angularProcess && !server.angularProcess.killed) {
          server.angularProcess.kill('SIGKILL');
        }
      } catch (error) {
        console.warn(`⚠️ Error killing Angular process:`, error);
      }
    }
    
    // Clean up ports as backup
    try {
      await killProcessOnPort(server.apiPort);
      await killProcessOnPort(server.angularPort);
    } catch (error) {
      // Ignore
    }
    
    // Clean up proxy config
    try {
      const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.parallel${parallelIndex}.conf.json`);
      if (fs.existsSync(proxyPath)) {
        fs.unlinkSync(proxyPath);
      }
    } catch (error) {
      // Ignore
    }
    
    this.servers.delete(parallelIndex);
    console.log(`✅ Servers stopped for parallel worker ${parallelIndex}`);
  }
  
  async stopAllServers(): Promise<void> {
    console.log(`🛑 Stopping all servers...`);
    const stopPromises = Array.from(this.servers.keys()).map(index => this.stopServer(index));
    await Promise.all(stopPromises);
    console.log(`✅ All servers stopped`);
  }
}

export { ServerPoolManager };