import { exec } from 'child_process';
import { promisify } from 'util';
import * as net from 'net';

const execAsync = promisify(exec);

export async function checkPortAvailable(port: number): Promise<boolean> {
  return new Promise((resolve) => {
    const server = net.createServer();
    
    server.once('error', () => {
      resolve(false);
    });
    
    server.once('listening', () => {
      server.close();
      resolve(true);
    });
    
    server.listen(port);
  });
}

export async function checkPortsAvailable(ports: number[]): Promise<{ available: boolean; conflicts: number[] }> {
  const conflicts: number[] = [];
  
  for (const port of ports) {
    const available = await checkPortAvailable(port);
    if (!available) {
      conflicts.push(port);
    }
  }
  
  return {
    available: conflicts.length === 0,
    conflicts
  };
}

export async function killProcessOnPort(port: number): Promise<void> {
  try {
    if (process.platform === 'win32') {
      // Windows: Find and kill process using the port
      try {
        const { stdout } = await execAsync(`netstat -ano | findstr :${port}`);
        const lines = stdout.trim().split('\n');
        const pids = new Set<string>();
        
        for (const line of lines) {
          const parts = line.trim().split(/\s+/);
          const pid = parts[parts.length - 1];
          if (pid && pid !== '0') {
            pids.add(pid);
          }
        }
        
        for (const pid of pids) {
          try {
            await execAsync(`taskkill /F /PID ${pid}`);
            console.log(`✅ Killed process ${pid} on port ${port}`);
          } catch (error) {
            console.warn(`⚠️ Failed to kill process ${pid}:`, error);
          }
        }
      } catch (error) {
        // Port might not be in use
        console.log(`ℹ️ No process found on port ${port}`);
      }
    } else {
      // Unix/Linux/Mac: Use lsof and kill
      try {
        const { stdout } = await execAsync(`lsof -ti:${port}`);
        const pids = stdout.trim().split('\n').filter(pid => pid);
        
        for (const pid of pids) {
          await execAsync(`kill -9 ${pid}`);
          console.log(`✅ Killed process ${pid} on port ${port}`);
        }
      } catch (error) {
        // Port might not be in use
        console.log(`ℹ️ No process found on port ${port}`);
      }
    }
  } catch (error) {
    console.error(`❌ Error killing process on port ${port}:`, error);
  }
}

export async function killAllTestServers(): Promise<void> {
  // Kill processes on common test ports
  const testPorts = [
    5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009,
    5010, 5011, 5012, 5013, 5014, 5015, 5016, 5017, 5018, 5019,
    5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109,
    5110, 5111, 5112, 5113, 5114, 5115, 5116, 5117, 5118, 5119,
    5170, 5171, 5172, 5173, 5174, 5175, 5176, 5177, 5178, 5179,
    5180, 5181, 5182, 5183, 5184, 5185, 5186, 5187, 5188, 5189,
    4200, 4201, 4202, 4203, 4204, 4205, 4206, 4207, 4208, 4209,
    4210, 4211, 4212, 4213, 4214, 4215, 4216, 4217, 4218, 4219
  ];
  
  console.log('🔪 Killing processes on test ports...');
  
  for (const port of testPorts) {
    await killProcessOnPort(port);
  }
  
  console.log('✅ Test server cleanup complete');
}

export async function findAvailablePort(startPort: number, maxPort: number = startPort + 100): Promise<number> {
  for (let port = startPort; port <= maxPort; port++) {
    if (await checkPortAvailable(port)) {
      return port;
    }
  }
  throw new Error(`No available ports found between ${startPort} and ${maxPort}`);
}

export async function waitForServer(url: string, timeout: number = 30000): Promise<boolean> {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    try {
      const response = await fetch(url, {
        method: 'GET',
        signal: AbortSignal.timeout(5000),
      });
      
      if (response.ok || response.status === 404) {
        // Server is responding
        return true;
      }
    } catch (error) {
      // Server not ready yet, wait and retry
    }
    
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  
  return false;
}