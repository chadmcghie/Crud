import fetch from 'node-fetch';
import * as net from 'net';
import { ChildProcess } from 'child_process';

/**
 * Check if a server is running and responding at the given URL
 * @param url The URL to check (e.g., http://localhost:5172/health)
 * @param timeout Timeout in milliseconds (default: 2000)
 * @returns true if server is responding, false otherwise
 */
export async function isServerRunning(url: string, timeout: number = 2000): Promise<boolean> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeout);
    
    const response = await fetch(url, {
      method: 'GET',
      headers: { 'Accept': 'text/html,application/json' },
      signal: controller.signal as any,
    });
    
    clearTimeout(timeoutId);
    // Accept any 2xx status code or 304 Not Modified
    return response.ok || response.status === 304;
  } catch (error) {
    // Server not responding or error occurred
    return false;
  }
}

/**
 * Check if a port is available (not in use)
 * @param port The port number to check
 * @param host The host to check (default: '127.0.0.1')
 * @returns true if port is available, false if in use
 */
export async function checkPortAvailability(port: number, host: string = '127.0.0.1'): Promise<boolean> {
  return new Promise((resolve) => {
    const server = net.createServer();
    
    server.once('error', (err: any) => {
      if (err.code === 'EADDRINUSE') {
        resolve(false); // Port is in use
      } else {
        resolve(true); // Other error, assume port is available
      }
    });
    
    server.once('listening', () => {
      server.close();
      resolve(true); // Port is available
    });
    
    server.listen(port, host);
  });
}

/**
 * Server status information
 */
export interface ServerInfo {
  running: boolean;
  processId?: number;
  startedByUs: boolean;
  message: string;
}

/**
 * Manages server status and lifecycle decisions
 */
export class ServerStatus {
  private servers: Map<string, ServerInfo> = new Map();
  
  /**
   * Check API server status
   */
  async checkApi(baseUrl: string): Promise<ServerInfo> {
    const healthUrl = `${baseUrl}/health`;
    const running = await isServerRunning(healthUrl);
    
    const info: ServerInfo = {
      running,
      startedByUs: false,
      message: running ? '✅ Already running' : '🔴 Not running'
    };
    
    this.servers.set('api', info);
    return info;
  }
  
  /**
   * Check Angular server status
   */
  async checkAngular(baseUrl: string): Promise<ServerInfo> {
    const running = await isServerRunning(baseUrl);
    
    const info: ServerInfo = {
      running,
      startedByUs: false,
      message: running ? '✅ Already running' : '🔴 Not running'
    };
    
    this.servers.set('angular', info);
    return info;
  }
  
  /**
   * Check all servers
   */
  async checkAll(apiUrl: string, angularUrl: string): Promise<void> {
    await this.checkApi(apiUrl);
    await this.checkAngular(angularUrl);
  }
  
  /**
   * Get a formatted status report
   */
  getStatusReport(): string {
    const lines: string[] = ['📦 Server Status:'];
    
    const apiInfo = this.servers.get('api');
    const angularInfo = this.servers.get('angular');
    
    if (apiInfo) {
      lines.push(`   API: ${apiInfo.message}`);
    }
    
    if (angularInfo) {
      lines.push(`   Angular: ${angularInfo.message}`);
    }
    
    return lines.join('\n');
  }
  
  /**
   * Mark a server as pre-existing (not started by us)
   */
  markAsPreExisting(serverName: string): void {
    const info = this.servers.get(serverName);
    if (info) {
      info.startedByUs = false;
    }
  }
  
  /**
   * Mark a server as started by our tests
   */
  markAsStartedByUs(serverName: string, processId: number): void {
    const info = this.servers.get(serverName) || { 
      running: true, 
      startedByUs: true, 
      message: '✅ Started by tests' 
    };
    
    info.startedByUs = true;
    info.processId = processId;
    this.servers.set(serverName, info);
  }
  
  /**
   * Determine if we should kill a server on teardown
   */
  shouldKillOnTeardown(serverName: string): boolean {
    const info = this.servers.get(serverName);
    return info ? info.startedByUs : false;
  }
  
  /**
   * Get server info
   */
  getServerInfo(serverName: string): ServerInfo | undefined {
    return this.servers.get(serverName);
  }
}

/**
 * Helper to determine if a server should be started
 */
export function shouldStartServer(info: ServerInfo): boolean {
  return !info.running;
}

/**
 * Helper to extend ServerInfo with shouldStart method
 */
export function extendServerInfo(info: ServerInfo): ServerInfo & { shouldStart(): boolean } {
  return Object.assign(info, {
    shouldStart(): boolean {
      return !info.running;
    }
  });
}