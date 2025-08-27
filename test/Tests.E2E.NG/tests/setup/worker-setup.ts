import { spawn, ChildProcess } from 'child_process';
import { WorkerInfo } from '@playwright/test';
import fetch from 'node-fetch';

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
    this.workerDatabase = `/tmp/CrudTest_Worker${this.workerIndex}_${timestamp}.db`;
    
    console.log(`🔧 Worker ${this.workerIndex}: API=${this.apiPort}, Angular=${this.angularPort}, DB=${this.workerDatabase}`);
  }

  async startServers(): Promise<void> {
    console.log(`🚀 Starting servers for worker ${this.workerIndex}...`);
    
    // Start API server
    await this.startApiServer();
    
    // Start Angular dev server
    await this.startAngularServer();
    
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

      this.apiProcess = spawn('dotnet', ['run'], {
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

  private async startAngularServer(): Promise<void> {
    return new Promise((resolve, reject) => {
      const angularEnv = {
        ...process.env,
        PORT: this.angularPort.toString(),
        NG_CLI_ANALYTICS: 'false'
      };

      this.angularProcess = spawn('npm', ['start', '--', `--port=${this.angularPort}`], {
        cwd: '../../src/Angular',
        env: angularEnv,
        stdio: ['pipe', 'pipe', 'pipe']
      });

      let angularStarted = false;
      const timeout = setTimeout(() => {
        if (!angularStarted) {
          reject(new Error(`Angular server for worker ${this.workerIndex} failed to start within timeout`));
        }
      }, 180000); // 3 minutes

      this.angularProcess.stdout?.on('data', (data) => {
        const output = data.toString();
        if (output.includes('webpack compiled') || output.includes('Local:')) {
          if (!angularStarted) {
            angularStarted = true;
            clearTimeout(timeout);
            this.waitForAngularHealth().then(resolve).catch(reject);
          }
        }
      });

      this.angularProcess.stderr?.on('data', (data) => {
        const errorOutput = data.toString();
        // Ignore common Angular warnings
        if (!errorOutput.includes('Warning:') && !errorOutput.includes('WARNING:')) {
          console.error(`Angular Worker ${this.workerIndex} stderr:`, errorOutput);
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
    const maxAttempts = 30;
    const delayMs = 2000;

    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const response = await fetch(`http://localhost:${this.angularPort}`);
        if (response.ok) {
          console.log(`✅ Angular health check passed for worker ${this.workerIndex}`);
          return;
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

    if (this.angularProcess) {
      this.angularProcess.kill('SIGTERM');
      this.angularProcess = null;
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