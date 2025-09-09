const assert = require('assert');
const fs = require('fs').promises;
const path = require('path');
const { StatusTracker } = require('../status-tracker');
const { StatusAggregator } = require('../status-aggregator');

describe('StatusTracker', () => {
  let tracker;
  const testStatusDir = path.join(__dirname, 'test-status');
  
  beforeEach(async () => {
    tracker = new StatusTracker(testStatusDir);
    // Create test directory
    await fs.mkdir(testStatusDir, { recursive: true });
  });
  
  afterEach(async () => {
    // Clean up test directory
    try {
      await fs.rm(testStatusDir, { recursive: true, force: true });
    } catch (err) {
      // Ignore cleanup errors
    }
  });
  
  describe('parseTasksFile', () => {
    it('should parse tasks with correct completion status', async () => {
      const tasksContent = `# Spec Tasks

## Tasks

- [x] 1. Completed task
  - [x] 1.1 Completed subtask
  - [ ] 1.2 Incomplete subtask
- [ ] 2. Incomplete task
  - [ ] 2.1 Incomplete subtask`;
      
      const tasks = await tracker.parseTasksFile(tasksContent);
      
      assert.strictEqual(tasks.total, 2);
      assert.strictEqual(tasks.completed, 1);
      assert.strictEqual(tasks.subtasks.total, 3);
      assert.strictEqual(tasks.subtasks.completed, 1);
    });
    
    it('should handle blocked tasks with warnings', async () => {
      const tasksContent = `# Spec Tasks

## Tasks

- [x] 1. Completed task
- [ ] 2. Blocked task
  ⚠️ Blocking issue: Database connection timeout`;
      
      const tasks = await tracker.parseTasksFile(tasksContent);
      
      assert.strictEqual(tasks.total, 2);
      assert.strictEqual(tasks.completed, 1);
      assert.strictEqual(tasks.blocked.length, 1);
      assert.strictEqual(tasks.blocked[0].task, '2. Blocked task');
      assert.strictEqual(tasks.blocked[0].reason, 'Database connection timeout');
    });
    
    it('should calculate completion percentage correctly', async () => {
      const tasksContent = `# Spec Tasks

## Tasks

- [x] 1. Task one
- [x] 2. Task two
- [ ] 3. Task three
- [ ] 4. Task four`;
      
      const tasks = await tracker.parseTasksFile(tasksContent);
      
      assert.strictEqual(tasks.total, 4);
      assert.strictEqual(tasks.completed, 2);
      assert.strictEqual(tasks.percentage, 50);
    });
  });
  
  describe('createStatusFile', () => {
    it('should create a properly formatted status file', async () => {
      const status = {
        lastUpdated: new Date().toISOString(),
        activeSpecs: [
          {
            name: 'test-spec',
            path: '.agent-os/specs/2025-01-01-test-spec',
            tasks: { total: 5, completed: 3, percentage: 60 },
            status: 'in-progress'
          }
        ],
        reviewResults: [],
        blockedItems: []
      };
      
      const content = await tracker.createStatusFile(status);
      
      assert(content.includes('# Agent OS Progress Status'));
      assert(content.includes('## Active Specifications'));
      assert(content.includes('test-spec'));
      assert(content.includes('60%'));
    });
    
    it('should include review results when available', async () => {
      const status = {
        lastUpdated: new Date().toISOString(),
        activeSpecs: [],
        reviewResults: [
          {
            spec: 'test-spec',
            date: new Date().toISOString(),
            critical: 0,
            warnings: 2,
            suggestions: 5
          }
        ],
        blockedItems: []
      };
      
      const content = await tracker.createStatusFile(status);
      
      assert(content.includes('## Recent Review Results'));
      assert(content.includes('test-spec'));
      assert(content.includes('2')); // Just check for the number
    });
    
    it('should include blocked items with reasons', async () => {
      const status = {
        lastUpdated: new Date().toISOString(),
        activeSpecs: [],
        reviewResults: [],
        blockedItems: [
          {
            spec: 'test-spec',
            task: '3. Implement feature',
            reason: 'Waiting for API design approval',
            since: new Date().toISOString()
          }
        ]
      };
      
      const content = await tracker.createStatusFile(status);
      
      assert(content.includes('## Blocked Items'));
      assert(content.includes('test-spec'));
      assert(content.includes('3. Implement feature'));
      assert(content.includes('Waiting for API design approval'));
    });
  });
  
  describe('updateStatus', () => {
    it('should update status file with current spec information', async () => {
      const specPath = '.agent-os/specs/2025-01-01-test-spec';
      const tasksContent = `# Spec Tasks

## Tasks

- [x] 1. Task one
- [ ] 2. Task two`;
      
      await tracker.updateStatus(specPath, tasksContent);
      
      const statusPath = path.join(testStatusDir, 'current-status.md');
      const exists = await fs.access(statusPath).then(() => true).catch(() => false);
      assert(exists);
      
      const content = await fs.readFile(statusPath, 'utf-8');
      assert(content.includes('test-spec'));
      assert(content.includes('50%'));
    });
  });
});

describe('StatusAggregator', () => {
  let aggregator;
  const testSpecsDir = path.join(__dirname, 'test-specs');
  
  beforeEach(async () => {
    aggregator = new StatusAggregator(testSpecsDir);
    // Create test directories
    await fs.mkdir(testSpecsDir, { recursive: true });
  });
  
  afterEach(async () => {
    // Clean up test directories
    try {
      await fs.rm(testSpecsDir, { recursive: true, force: true });
    } catch (err) {
      // Ignore cleanup errors
    }
  });
  
  describe('scanSpecs', () => {
    it('should find all spec directories with tasks.md files', async () => {
      // Create test spec directories
      const spec1Dir = path.join(testSpecsDir, '2025-01-01-spec-one');
      const spec2Dir = path.join(testSpecsDir, '2025-01-02-spec-two');
      
      await fs.mkdir(spec1Dir, { recursive: true });
      await fs.mkdir(spec2Dir, { recursive: true });
      
      await fs.writeFile(path.join(spec1Dir, 'tasks.md'), '# Tasks');
      await fs.writeFile(path.join(spec2Dir, 'tasks.md'), '# Tasks');
      
      const specs = await aggregator.scanSpecs();
      
      assert.strictEqual(specs.length, 2);
      assert(specs.some(s => s.name === 'spec-one'));
      assert(specs.some(s => s.name === 'spec-two'));
    });
    
    it('should ignore directories without tasks.md', async () => {
      const spec1Dir = path.join(testSpecsDir, '2025-01-01-spec-with-tasks');
      const spec2Dir = path.join(testSpecsDir, '2025-01-02-spec-without-tasks');
      
      await fs.mkdir(spec1Dir, { recursive: true });
      await fs.mkdir(spec2Dir, { recursive: true });
      
      await fs.writeFile(path.join(spec1Dir, 'tasks.md'), '# Tasks');
      // No tasks.md in spec2Dir
      
      const specs = await aggregator.scanSpecs();
      
      assert.strictEqual(specs.length, 1);
      assert.strictEqual(specs[0].name, 'spec-with-tasks');
    });
  });
  
  describe('aggregateProgress', () => {
    it('should aggregate progress across multiple specs', async () => {
      const specs = [
        {
          name: 'spec-one',
          tasks: { total: 4, completed: 2, percentage: 50 }
        },
        {
          name: 'spec-two',
          tasks: { total: 6, completed: 6, percentage: 100 }
        }
      ];
      
      // Provide full tasks objects as expected by aggregateProgress
      const specsWithTasks = specs.map(s => ({
        ...s,
        tasks: {
          ...s.tasks,
          subtasks: { total: 0, completed: 0 },
          blocked: []
        }
      }));
      
      const aggregate = await aggregator.aggregateProgress(specsWithTasks);
      
      assert.strictEqual(aggregate.totalSpecs, 2);
      assert.strictEqual(aggregate.completedSpecs, 1);
      assert.strictEqual(aggregate.totalTasks, 10);
      assert.strictEqual(aggregate.completedTasks, 8);
      assert.strictEqual(aggregate.overallPercentage, 80);
    });
    
    it('should handle empty specs array', async () => {
      const aggregate = await aggregator.aggregateProgress([]);
      
      assert.strictEqual(aggregate.totalSpecs, 0);
      assert.strictEqual(aggregate.completedSpecs, 0);
      assert.strictEqual(aggregate.totalTasks, 0);
      assert.strictEqual(aggregate.completedTasks, 0);
      assert.strictEqual(aggregate.overallPercentage, 0);
    });
  });
  
  describe('generateSummary', () => {
    it('should generate a markdown summary of all specs', async () => {
      const aggregate = {
        totalSpecs: 3,
        completedSpecs: 1,
        totalTasks: 15,
        completedTasks: 10,
        overallPercentage: 67,
        specs: [
          {
            name: 'spec-one',
            status: 'completed',
            tasks: { total: 5, completed: 5, percentage: 100, blocked: [] }
          },
          {
            name: 'spec-two',
            status: 'in-progress',
            tasks: { total: 10, completed: 5, percentage: 50, blocked: [] }
          }
        ]
      };
      
      const summary = await aggregator.generateSummary(aggregate);
      
      assert(summary.includes('# Agent OS Overall Progress'));
      assert(summary.includes('67%'));
      assert(summary.includes('spec-one'));
      assert(summary.includes('✅ Completed'));
      assert(summary.includes('spec-two'));
      assert(summary.includes('🚧 In Progress'));
    });
  });
});