import { spawn, ChildProcess } from 'child_process';
import { WorkerInfo } from '@playwright/test';
import fetch from 'node-fetch';
import * as fs from 'fs';
import * as path from 'path';
import { WorkerStartupQueue } from './worker-queue';

export class WorkerServerManager {
  private apiProcess: ChildProcess | null = null;
  private angularProcess: ChildProcess | null = null;
  private apiPort: number;
  private angularPort: number;
  private workerDatabase: string;
  private workerIndex: number;

  constructor(workerInfo: WorkerInfo) {
    this.workerIndex = workerInfo.workerIndex;
    this.apiPort = 5172 + this.workerIndex;
    this.angularPort = 4200 + (this.workerIndex * 10);
    
    const timestamp = Date.now();
    const tempDir = process.platform === 'win32' ? process.env.TEMP || 'C:\\temp' : '/tmp';
    this.workerDatabase = path.join(tempDir, `CrudTest_Worker${this.workerIndex}_${timestamp}.db`);
    
    console.log(`🔧 Worker ${this.workerIndex}: API=${this.apiPort}, Angular=${this.angularPort}, DB=${this.workerDatabase}`);
  }

  async startServers(): Promise<void> {
    // Use queue to prevent port conflicts
    const queue = WorkerStartupQueue.getInstance();
    await queue.enqueue(this.workerIndex);
    
    try {
      console.log(`🚀 Starting servers for worker ${this.workerIndex}...`);
      
      // Create worker-specific proxy config for Angular
      this.createProxyConfig();
      
      // Start API server
      await this.startApiServer();
      
      // Start Angular dev server
      await this.startAngularServer();
      
      console.log(`✅ Worker ${this.workerIndex} servers ready`);
    } finally {
      // Mark worker as complete in queue
      queue.complete(this.workerIndex);
    }
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
    
    const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.worker${this.workerIndex}.conf.json`);
    fs.writeFileSync(proxyPath, JSON.stringify(proxyConfig, null, 2));
    console.log(`📝 Created proxy config for worker ${this.workerIndex} at ${proxyPath}`);
  }

  private async startApiServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const apiEnv = {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: `http://localhost:${this.apiPort}`,
        WORKER_DATABASE: this.workerDatabase,
        WORKER_INDEX: this.workerIndex.toString(),
        DatabaseProvider: 'SQLite',
        ConnectionStrings__DefaultConnection: `Data Source=${this.workerDatabase}`
      };

      const apiPath = path.resolve(process.cwd(), '..', '..', 'src', 'Api');
      console.log(`🔧 Starting API from: ${apiPath}`);
      
      this.apiProcess = spawn('dotnet', ['run', '--no-launch-profile'], {
        cwd: apiPath,
        env: apiEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: true
      });

      let apiStarted = false;
      const timeout = setTimeout(() => {
        if (!apiStarted) {
          reject(new Error(`API server for worker ${this.workerIndex} failed to start within timeout`));
        }
      }, 120000); // 2 minutes

      this.apiProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if (output.includes('Now listening on:') || output.includes('Application started')) {
          if (!apiStarted) {
            apiStarted = true;
            clearTimeout(timeout);
            this.waitForApiHealth().then(resolve).catch(reject);
          }
        }
      });

      this.apiProcess.stderr?.on('data', (data) => {
        console.error(`API Worker ${this.workerIndex} stderr:`, data.toString());
      });

      this.apiProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(new Error(`API server for worker ${this.workerIndex} failed: ${error.message}`));
      });
    });
  }

  private async startAngularServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const angularEnv = {
        ...process.env,
        PORT: this.angularPort.toString(),
        NG_CLI_ANALYTICS: 'false',
        NODE_OPTIONS: '--max-old-space-size=4096'  // Increase memory for faster compilation
      };

      // Angular needs to proxy to the correct API port for this worker
      const proxyConfig = `--proxy-config=proxy.worker${this.workerIndex}.conf.json`;
      const portConfig = `--port=${this.angularPort}`;
      // Add optimization flags for faster startup in test environment
      const optimizationFlags = '--poll=2000 --live-reload=false --hmr=false';
      
      const angularPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular');
      console.log(`🔧 Starting Angular from: ${angularPath}`);
      
      const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';
      this.angularProcess = spawn(npmCommand, ['start', '--', portConfig, proxyConfig, optimizationFlags], {
        cwd: angularPath,
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe'],
        shell: false
      });

      let angularStarted = false;
      const timeout = setTimeout(() => {
        if (!angularStarted) {
          console.error(`❌ Angular server for worker ${this.workerIndex} timeout after 5 minutes`);
          reject(new Error(`Angular server for worker ${this.workerIndex} failed to start within timeout`));
        }
      }, 300000); // 5 minutes - increased timeout

      this.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        // Log Angular output for debugging
        if (output.trim()) {
          console.log(`Angular Worker ${this.workerIndex} output:`, output.trim());
        }
        // Check for various Angular startup messages - improved detection
        if (output.includes('webpack compiled') || 
            output.includes('Local:') || 
            output.includes('Compiled successfully') ||
            output.includes('Angular Live Development Server') ||
            output.includes('Application bundle generation complete') ||
            output.includes('Watch mode enabled') ||
            output.includes('Build completed')) {
          if (!angularStarted) {
            angularStarted = true;
            clearTimeout(timeout);
            console.log(`✅ Angular compilation detected for worker ${this.workerIndex}`);
            // Give Angular a bit more time to fully initialize before health check
            setTimeout(() => {
              this.waitForAngularHealth().then(resolve).catch(reject);
            }, 2000);
          }
        }
      });

      this.angularProcess.stderr?.on('data', (data) => {
        const errorOutput = data.toString();
        // Log all stderr for better debugging but don't fail on warnings
        console.log(`Angular Worker ${this.workerIndex} stderr:`, errorOutput.trim());
        // Check for actual errors
        if (errorOutput.includes('Error:') || errorOutput.includes('ERROR')) {
          console.error(`⚠️ Angular Worker ${this.workerIndex} error detected`);
        }
      });

      this.angularProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(new Error(`Angular server for worker ${this.workerIndex} failed: ${error.message}`));
      });
    });
  }

  private async waitForApiHealth(): Promise<void> {
    const maxAttempts = 30;
    const delayMs = 2000;

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${this.apiPort}/health`);
        if (response.ok) {
          console.log(`✅ API health check passed for worker ${this.workerIndex}`);
          return;
        }
      } catch (error) {
        // API not ready yet
      }

      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }

    throw new Error(`API health check failed for worker ${this.workerIndex} after ${maxAttempts} attempts`);
  }

  private async waitForAngularHealth(): Promise<void> {
    const maxAttempts = 60;  // Increased attempts
    const delayMs = 2000;

    console.log(`🔍 Starting Angular health check for worker ${this.workerIndex} on port ${this.angularPort}`);

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${this.angularPort}`);
        if (response.ok) {
          // Double-check Angular is actually serving the app
          const text = await response.text();
          if (text.includes('<app-root>') || text.includes('angular') || text.includes('<!doctype html>')) {
            console.log(`✅ Angular health check passed for worker ${this.workerIndex} on attempt ${attempt}`);
            return;
          } else {
            console.log(`⚠️ Angular response received but doesn't look like Angular app for worker ${this.workerIndex}`);
          }
        } else {
          console.log(`⚠️ Angular health check got status ${response.status} for worker ${this.workerIndex}`);
        }
      } catch (error) {
        // Angular not ready yet - this is expected during startup
        if (attempt % 10 === 0) {
          console.log(`⏳ Still waiting for Angular to start for worker ${this.workerIndex} (attempt ${attempt}/${maxAttempts})...`);
        }
      }

      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }

    throw new Error(`Angular health check failed for worker ${this.workerIndex} after ${maxAttempts} attempts (${maxAttempts * delayMs / 1000} seconds)`);
  }

  async stopServers(): Promise<void> {
    console.log(`🛑 Stopping servers for worker ${this.workerIndex}...`);

    if (this.apiProcess) {
      this.apiProcess.kill('SIGTERM');
      this.apiProcess = null;
    }

    if (this.angularProcess) {
      this.angularProcess.kill('SIGTERM');
      this.angularProcess = null;
    }
    
    // Clean up proxy config file
    try {
      const proxyPath = path.resolve(process.cwd(), '..', '..', 'src', 'Angular', `proxy.worker${this.workerIndex}.conf.json`);
      if (fs.existsSync(proxyPath)) {
        fs.unlinkSync(proxyPath);
        console.log(`🧹 Removed proxy config for worker ${this.workerIndex}`);
      }
    } catch (error) {
      // Ignore cleanup errors
    }

    console.log(`✅ Worker ${this.workerIndex} servers stopped`);
  }

  getApiUrl(): string {
    return `http://localhost:${this.apiPort}`;
  }

  getAngularUrl(): string {
    return `http://localhost:${this.angularPort}`;
  }

  getWorkerDatabase(): string {
    return this.workerDatabase;
  }
}