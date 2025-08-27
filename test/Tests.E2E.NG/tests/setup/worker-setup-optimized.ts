import { spawn, ChildProcess } from 'child_process';
import { WorkerInfo } from '@playwright/test';
import fetch from 'node-fetch';
import * as fs from 'fs';
import * as path from 'path';
import { createServer } from 'http';
import { createProxyMiddleware } from 'http-proxy-middleware';
import * as express from 'express';

export class OptimizedWorkerServerManager {
  private apiProcess: ChildProcess | null = null;
  private angularServer: any = null;  // Express server for Angular
  private apiPort: number;
  private angularPort: number;
  private workerDatabase: string;
  private workerIndex: number;

  constructor(workerInfo: WorkerInfo) {
    this.workerIndex = workerInfo.workerIndex;
    this.apiPort = 5172 + this.workerIndex;
    this.angularPort = 4200 + (this.workerIndex * 10);
    
    const timestamp = Date.now();
    this.workerDatabase = `/tmp/CrudTest_Worker${this.workerIndex}_${timestamp}.db`;
    
    console.log(`🔧 Worker ${this.workerIndex}: API=${this.apiPort}, Angular=${this.angularPort}, DB=${this.workerDatabase}`);
  }

  async startServers(): Promise<void> {
    console.log(`🚀 Starting optimized servers for worker ${this.workerIndex}...`);
    
    // Start API server
    await this.startApiServer();
    
    // Serve pre-built Angular with proxy
    await this.startAngularStaticServer();
    
    console.log(`✅ Worker ${this.workerIndex} servers ready`);
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

      this.apiProcess = spawn('dotnet', ['run', '--no-launch-profile'], {
        cwd: '../../src/Api',
        env: apiEnv,
        stdio: ['pipe', 'pipe', 'pipe']
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

  private async startAngularStaticServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const app = express();

      // Proxy API calls to the worker's API server
      app.use('/api', createProxyMiddleware({
        target: `http://localhost:${this.apiPort}`,
        changeOrigin: true,
        logLevel: 'warn'
      }));

      // Check if Angular is pre-built
      const angularDistPath = path.join(__dirname, '..', '..', '..', '..', 'src', 'Angular', 'dist', 'angular', 'browser');
      
      if (!fs.existsSync(angularDistPath)) {
        console.log(`⚠️ Angular not pre-built. Building now for worker ${this.workerIndex}...`);
        // Fall back to development server
        this.startAngularDevServer().then(resolve).catch(reject);
        return;
      }

      // Serve pre-built Angular files
      app.use(express.static(angularDistPath));

      // Fallback to index.html for Angular routing
      app.get('*', (req, res) => {
        res.sendFile(path.join(angularDistPath, 'index.html'));
      });

      this.angularServer = app.listen(this.angularPort, () => {
        console.log(`✅ Angular static server started for worker ${this.workerIndex} on port ${this.angularPort}`);
        // Quick health check
        setTimeout(() => {
          this.waitForAngularHealth().then(resolve).catch(reject);
        }, 500);
      });

      this.angularServer.on('error', (error: any) => {
        reject(new Error(`Angular server for worker ${this.workerIndex} failed: ${error.message}`));
      });
    });
  }

  private async startAngularDevServer(): Promise<void> {
    // Fallback to original dev server implementation
    return new Promise((resolve, reject) => {
      const angularEnv = {
        ...process.env,
        PORT: this.angularPort.toString(),
        NG_CLI_ANALYTICS: 'false',
        NODE_OPTIONS: '--max-old-space-size=4096'
      };

      const proxyConfig = {
        "/api": {
          "target": `http://localhost:${this.apiPort}`,
          "secure": false,
          "changeOrigin": true
        }
      };
      
      const proxyPath = path.join(__dirname, '..', '..', '..', '..', 'src', 'Angular', `proxy.worker${this.workerIndex}.conf.json`);
      fs.writeFileSync(proxyPath, JSON.stringify(proxyConfig, null, 2));

      const proxyConfigArg = `--proxy-config=proxy.worker${this.workerIndex}.conf.json`;
      const portConfig = `--port=${this.angularPort}`;
      
      const angularProcess = spawn('npm', ['start', '--', portConfig, proxyConfigArg], {
        cwd: '../../src/Angular',
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe']
      });

      let angularStarted = false;
      const timeout = setTimeout(() => {
        if (!angularStarted) {
          reject(new Error(`Angular dev server for worker ${this.workerIndex} failed to start within timeout`));
        }
      }, 300000);

      angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if (output.includes('compiled') || output.includes('Local:')) {
          if (!angularStarted) {
            angularStarted = true;
            clearTimeout(timeout);
            this.waitForAngularHealth().then(resolve).catch(reject);
          }
        }
      });

      angularProcess.on('error', (error) => {
        clearTimeout(timeout);
        reject(new Error(`Angular dev server for worker ${this.workerIndex} failed: ${error.message}`));
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
    const maxAttempts = 30;
    const delayMs = 1000;

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${this.angularPort}`);
        if (response.ok) {
          const text = await response.text();
          if (text.includes('<app-root>') || text.includes('angular') || text.includes('<!doctype html>')) {
            console.log(`✅ Angular health check passed for worker ${this.workerIndex}`);
            return;
          }
        }
      } catch (error) {
        // Angular not ready yet
      }

      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }

    throw new Error(`Angular health check failed for worker ${this.workerIndex} after ${maxAttempts} attempts`);
  }

  async stopServers(): Promise<void> {
    console.log(`🛑 Stopping servers for worker ${this.workerIndex}...`);

    if (this.apiProcess) {
      this.apiProcess.kill('SIGTERM');
      this.apiProcess = null;
    }

    if (this.angularServer) {
      this.angularServer.close();
      this.angularServer = null;
    }

    // Clean up proxy config file if exists
    try {
      const proxyPath = path.join(__dirname, '..', '..', '..', '..', 'src', 'Angular', `proxy.worker${this.workerIndex}.conf.json`);
      if (fs.existsSync(proxyPath)) {
        fs.unlinkSync(proxyPath);
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