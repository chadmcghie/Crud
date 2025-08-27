import { spawn, ChildProcess } from 'child_process';
import { WorkerInfo } from '@playwright/test';
import fetch from 'node-fetch';
import * as fs from 'fs';
import * as path from 'path';
import * as http from 'http';
import * as url from 'url';

/**
 * Optimized worker setup that serves pre-built Angular files
 * Falls back to dev server if no build is available
 */
export class StaticWorkerServerManager {
  private apiProcess: ChildProcess | null = null;
  private angularServer: http.Server | null = null;
  private angularProcess: ChildProcess | null = null;
  private apiPort: number;
  private angularPort: number;
  private workerDatabase: string;
  private workerIndex: number;
  private useDevServer: boolean = false;

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
    
    // Try to use pre-built Angular, fall back to dev server
    const angularDistPath = path.join(__dirname, '..', '..', '..', '..', 'src', 'Angular', 'dist', 'angular', 'browser');
    
    if (fs.existsSync(angularDistPath)) {
      console.log(`📦 Using pre-built Angular from ${angularDistPath}`);
      await this.startStaticAngularServer(angularDistPath);
    } else {
      console.log(`⚠️ No pre-built Angular found, falling back to dev server`);
      this.useDevServer = true;
      await this.startAngularDevServer();
    }
    
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

  private async startStaticAngularServer(distPath: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const server = http.createServer((req, res) => {
        // Proxy API calls to the worker's API server
        if (req.url?.startsWith('/api')) {
          const apiUrl = `http://localhost:${this.apiPort}${req.url}`;
          
          const proxyReq = http.request(apiUrl, {
            method: req.method,
            headers: req.headers
          }, (proxyRes) => {
            res.writeHead(proxyRes.statusCode || 200, proxyRes.headers);
            proxyRes.pipe(res);
          });
          
          proxyReq.on('error', (err) => {
            console.error(`Proxy error for worker ${this.workerIndex}:`, err);
            res.writeHead(502);
            res.end('Bad Gateway');
          });
          
          req.pipe(proxyReq);
          return;
        }

        // Serve static files
        let filePath = path.join(distPath, req.url === '/' ? 'index.html' : req.url!);
        
        // Handle Angular routes - serve index.html for client-side routing
        if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
          filePath = path.join(distPath, 'index.html');
        }

        const ext = path.extname(filePath);
        const contentType = this.getContentType(ext);

        fs.readFile(filePath, (err, content) => {
          if (err) {
            res.writeHead(404);
            res.end('File not found');
            return;
          }

          res.writeHead(200, { 'Content-Type': contentType });
          res.end(content);
        });
      });

      server.listen(this.angularPort, () => {
        console.log(`✅ Static Angular server started for worker ${this.workerIndex} on port ${this.angularPort}`);
        this.angularServer = server;
        
        // Quick health check
        setTimeout(() => {
          this.waitForAngularHealth().then(resolve).catch(reject);
        }, 100);
      });

      server.on('error', (error) => {
        reject(new Error(`Static Angular server for worker ${this.workerIndex} failed: ${error.message}`));
      });
    });
  }

  private getContentType(ext: string): string {
    const types: Record<string, string> = {
      '.html': 'text/html',
      '.js': 'application/javascript',
      '.css': 'text/css',
      '.json': 'application/json',
      '.png': 'image/png',
      '.jpg': 'image/jpeg',
      '.gif': 'image/gif',
      '.svg': 'image/svg+xml',
      '.ico': 'image/x-icon'
    };
    return types[ext] || 'application/octet-stream';
  }

  private async startAngularDevServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const angularEnv = {
        ...process.env,
        PORT: this.angularPort.toString(),
        NG_CLI_ANALYTICS: 'false',
        NODE_OPTIONS: '--max-old-space-size=4096'
      };

      // Create proxy config
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
      const optimizationFlags = '--poll=2000 --live-reload=false --hmr=false';
      
      this.angularProcess = spawn('npm', ['start', '--', portConfig, proxyConfigArg, optimizationFlags], {
        cwd: '../../src/Angular',
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe']
      });

      let angularStarted = false;
      const timeout = setTimeout(() => {
        if (!angularStarted) {
          reject(new Error(`Angular dev server for worker ${this.workerIndex} failed to start within timeout`));
        }
      }, 300000); // 5 minutes

      this.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if (output.trim()) {
          console.log(`Angular Worker ${this.workerIndex}:`, output.trim());
        }
        
        if (output.includes('compiled') || 
            output.includes('Local:') || 
            output.includes('Application bundle generation complete')) {
          if (!angularStarted) {
            angularStarted = true;
            clearTimeout(timeout);
            setTimeout(() => {
              this.waitForAngularHealth().then(resolve).catch(reject);
            }, 2000);
          }
        }
      });

      this.angularProcess.stderr?.on('data', (data) => {
        console.log(`Angular Worker ${this.workerIndex} stderr:`, data.toString());
      });

      this.angularProcess.on('error', (error) => {
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
        if (attempt % 10 === 0) {
          console.log(`⏳ Waiting for Angular (worker ${this.workerIndex})...`);
        }
      }

      if (attempt < maxAttempts) {
        await new Promise(resolve => setTimeout(resolve, delayMs));
      }
    }

    throw new Error(`Angular health check failed for worker ${this.workerIndex}`);
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

    if (this.angularProcess) {
      this.angularProcess.kill('SIGTERM');
      this.angularProcess = null;
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