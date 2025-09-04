"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.isPortAvailable = isPortAvailable;
exports.checkPortsAvailable = checkPortsAvailable;
exports.findAvailablePort = findAvailablePort;
exports.killProcessOnPort = killProcessOnPort;
exports.killAllTestServers = killAllTestServers;
const net = require("net");
const child_process_1 = require("child_process");
const util_1 = require("util");
const execAsync = (0, util_1.promisify)(child_process_1.exec);
async function isPortAvailable(port) {
    return new Promise((resolve) => {
        const server = net.createServer();
        server.once('error', (err) => {
            if (err.code === 'EADDRINUSE') {
                resolve(false);
            }
            else {
                resolve(false);
            }
        });
        server.once('listening', () => {
            server.close(() => {
                resolve(true);
            });
        });
        server.listen(port, '127.0.0.1');
    });
}
async function checkPortsAvailable(ports) {
    const conflicts = [];
    for (const port of ports) {
        const available = await isPortAvailable(port);
        if (!available) {
            conflicts.push(port);
        }
    }
    return {
        available: conflicts.length === 0,
        conflicts
    };
}
async function findAvailablePort(startPort, maxAttempts = 100) {
    for (let i = 0; i < maxAttempts; i++) {
        const port = startPort + i;
        if (await isPortAvailable(port)) {
            return port;
        }
    }
    throw new Error(`Could not find available port starting from ${startPort} after ${maxAttempts} attempts`);
}
async function killProcessOnPort(port) {
    try {
        if (process.platform === 'win32') {
            // Windows: Find and kill process using the port
            try {
                // First, find the PID using the port
                const { stdout: netstatOutput } = await execAsync(`netstat -ano | findstr :${port}`);
                const lines = netstatOutput.trim().split('\n');
                const pids = new Set();
                for (const line of lines) {
                    const parts = line.trim().split(/\s+/);
                    const pid = parts[parts.length - 1];
                    if (pid && pid !== '0') {
                        pids.add(pid);
                    }
                }
                // Kill each PID
                for (const pid of pids) {
                    console.log(`🔪 Killing process ${pid} on port ${port}`);
                    try {
                        await execAsync(`taskkill /F /PID ${pid}`);
                    }
                    catch (killError) {
                        console.warn(`⚠️ Could not kill process ${pid}: ${killError}`);
                    }
                }
            }
            catch (error) {
                console.warn(`⚠️ Could not find process on port ${port}: ${error}`);
            }
        }
        else {
            // Unix/Linux/Mac: Use lsof to find and kill process
            try {
                const { stdout } = await execAsync(`lsof -t -i:${port}`);
                const pids = stdout.trim().split('\n').filter(pid => pid);
                for (const pid of pids) {
                    console.log(`🔪 Killing process ${pid} on port ${port}`);
                    await execAsync(`kill -9 ${pid}`);
                }
            }
            catch (error) {
                console.warn(`⚠️ Could not find or kill process on port ${port}: ${error}`);
            }
        }
        // Wait a bit for the port to be released
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
    catch (error) {
        console.error(`❌ Error killing process on port ${port}:`, error);
    }
}
async function killAllTestServers() {
    console.log('🧹 Killing all test servers...');
    // Common ports used by test servers
    const testPorts = [
        // API ports
        5172, 5173, 5174, 5175, 5176, 5177, 5178, 5179, 5180,
        // Angular ports
        4200, 4210, 4220, 4230, 4240, 4250, 4260, 4270, 4280
    ];
    for (const port of testPorts) {
        const isInUse = !(await isPortAvailable(port));
        if (isInUse) {
            console.log(`📍 Found server on port ${port}, killing...`);
            await killProcessOnPort(port);
        }
    }
    console.log('✅ All test servers killed');
}
