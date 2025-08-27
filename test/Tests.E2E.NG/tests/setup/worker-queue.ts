import { EventEmitter } from 'events';

/**
 * Queue system to serialize worker startup and prevent port conflicts
 */
export class WorkerStartupQueue {
  private static instance: WorkerStartupQueue;
  private queue: Array<{ workerIndex: number; resolve: () => void }> = [];
  private isProcessing = false;
  private activeWorkers = new Set<number>();
  private eventEmitter = new EventEmitter();

  private constructor() {}

  static getInstance(): WorkerStartupQueue {
    if (!WorkerStartupQueue.instance) {
      WorkerStartupQueue.instance = new WorkerStartupQueue();
    }
    return WorkerStartupQueue.instance;
  }

  /**
   * Add a worker to the startup queue
   */
  async enqueue(workerIndex: number): Promise<void> {
    return new Promise<void>((resolve) => {
      console.log(`🔄 Worker ${workerIndex} queued for startup`);
      
      this.queue.push({ workerIndex, resolve });
      this.processQueue();
    });
  }

  /**
   * Process the startup queue
   */
  private async processQueue(): Promise<void> {
    if (this.isProcessing || this.queue.length === 0) {
      return;
    }

    this.isProcessing = true;

    while (this.queue.length > 0) {
      const { workerIndex, resolve } = this.queue.shift()!;
      
      // Check if worker is already active
      if (this.activeWorkers.has(workerIndex)) {
        console.log(`⚠️ Worker ${workerIndex} already active, skipping`);
        resolve();
        continue;
      }

      console.log(`🚀 Starting worker ${workerIndex} (${this.queue.length} in queue)`);
      
      // Mark worker as active
      this.activeWorkers.add(workerIndex);
      
      // Signal that worker can start
      resolve();
      
      // Wait a bit before starting the next worker to avoid race conditions
      await new Promise(r => setTimeout(r, 2000));
    }

    this.isProcessing = false;
  }

  /**
   * Mark a worker as completed
   */
  complete(workerIndex: number): void {
    this.activeWorkers.delete(workerIndex);
    console.log(`✅ Worker ${workerIndex} startup complete`);
    this.eventEmitter.emit('workerComplete', workerIndex);
  }

  /**
   * Reset the queue (for testing)
   */
  reset(): void {
    this.queue = [];
    this.activeWorkers.clear();
    this.isProcessing = false;
  }

  /**
   * Get active workers count
   */
  getActiveWorkersCount(): number {
    return this.activeWorkers.size;
  }

  /**
   * Wait for all workers to complete
   */
  async waitForAll(): Promise<void> {
    return new Promise((resolve) => {
      if (this.activeWorkers.size === 0 && this.queue.length === 0) {
        resolve();
        return;
      }

      const checkComplete = () => {
        if (this.activeWorkers.size === 0 && this.queue.length === 0) {
          this.eventEmitter.off('workerComplete', checkComplete);
          resolve();
        }
      };

      this.eventEmitter.on('workerComplete', checkComplete);
    });
  }
}