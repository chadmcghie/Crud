const fs = require('fs').promises;
const path = require('path');

class StatusAggregator {
  constructor(specsDir = '.agent-os/specs') {
    this.specsDir = specsDir;
    this.statusDir = '.agent-os/status';
    this.statusFile = path.join(this.statusDir, 'current-status.md');
  }
  
  /**
   * Scan all spec directories for tasks.md files
   */
  async scanSpecs() {
    const specs = [];
    
    try {
      const entries = await fs.readdir(this.specsDir, { withFileTypes: true });
      
      for (const entry of entries) {
        if (entry.isDirectory() && /^\d{4}-\d{2}-\d{2}-/.test(entry.name)) {
          const specPath = path.join(this.specsDir, entry.name);
          const tasksPath = path.join(specPath, 'tasks.md');
          
          // Check if tasks.md exists
          try {
            await fs.access(tasksPath);
            const specName = entry.name.replace(/^\d{4}-\d{2}-\d{2}-/, '');
            specs.push({
              name: specName,
              path: specPath,
              tasksPath: tasksPath,
              folderName: entry.name
            });
          } catch (err) {
            // tasks.md doesn't exist, skip this spec
          }
        }
      }
    } catch (err) {
      console.error('Error scanning specs directory:', err);
    }
    
    return specs;
  }
  
  /**
   * Parse a tasks.md file to extract completion status
   */
  async parseTasksFile(filePath) {
    try {
      const content = await fs.readFile(filePath, 'utf-8');
      const lines = content.split('\n');
      
      const result = {
        total: 0,
        completed: 0,
        percentage: 0,
        subtasks: {
          total: 0,
          completed: 0
        },
        blocked: [],
        issueNumber: null
      };
      
      let currentTask = null;
      
      for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        
        // Extract parent issue number
        if (line.includes('Parent Issue: #')) {
          const match = line.match(/Parent Issue: #(\d+)/);
          if (match) {
            result.issueNumber = match[1];
          }
        }
        
        // Parse parent tasks - handle both with and without Issue numbers
        const parentMatch = line.match(/^- \[([ x])\] (\d+\. .+)/);
        if (parentMatch) {
          result.total++;
          // Remove the Issue part from the task name if present
          currentTask = parentMatch[2].replace(/\s*\(Issue: #\d+\)$/, '');
          
          if (parentMatch[1] === 'x') {
            result.completed++;
          }
          
          // Check for blocking indicator
          if (i + 1 < lines.length && lines[i + 1].includes('⚠️ Blocking issue:')) {
            const blockingReason = lines[i + 1].replace(/.*⚠️ Blocking issue:\s*/, '').trim();
            result.blocked.push({
              task: currentTask,
              reason: blockingReason
            });
          }
        }
        
        // Parse subtasks
        const subtaskMatch = line.match(/^\s+- \[([ x])\] \d+\.\d+ .+/);
        if (subtaskMatch) {
          result.subtasks.total++;
          if (subtaskMatch[1] === 'x') {
            result.subtasks.completed++;
          }
        }
      }
      
      // Calculate percentage
      if (result.total > 0) {
        result.percentage = Math.round((result.completed / result.total) * 100);
      }
      
      return result;
    } catch (err) {
      console.error(`Error parsing ${filePath}:`, err);
      return {
        total: 0,
        completed: 0,
        percentage: 0,
        subtasks: { total: 0, completed: 0 },
        blocked: []
      };
    }
  }
  
  /**
   * Aggregate progress across all specs
   */
  async aggregateProgress(specs) {
    const result = {
      totalSpecs: specs.length,
      completedSpecs: 0,
      blockedSpecs: 0,
      totalTasks: 0,
      completedTasks: 0,
      totalSubtasks: 0,
      completedSubtasks: 0,
      overallPercentage: 0,
      specs: []
    };
    
    for (const spec of specs) {
      const tasks = spec.tasks || await this.parseTasksFile(spec.tasksPath);
      
      // Determine spec status
      let status = 'in-progress';
      if (tasks.percentage === 100) {
        status = 'completed';
        result.completedSpecs++;
      } else if (tasks.blocked.length > 0) {
        status = 'blocked';
        result.blockedSpecs++;
      }
      
      result.totalTasks += tasks.total;
      result.completedTasks += tasks.completed;
      result.totalSubtasks += tasks.subtasks.total;
      result.completedSubtasks += tasks.subtasks.completed;
      
      result.specs.push({
        name: spec.name,
        path: spec.path,
        status: status,
        tasks: tasks,
        issueNumber: tasks.issueNumber
      });
    }
    
    // Calculate overall percentage
    if (result.totalTasks > 0) {
      result.overallPercentage = Math.round((result.completedTasks / result.totalTasks) * 100);
    }
    
    return result;
  }
  
  /**
   * Generate markdown summary
   */
  async generateSummary(aggregate) {
    const now = new Date();
    const timestamp = now.toISOString();
    const readableDate = now.toLocaleString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZoneName: 'short'
    });
    
    let content = `# Agent OS Overall Progress

> Last Updated: ${readableDate}
> Generated: ${timestamp}

## Summary Statistics

`;
    
    // Overall stats
    const progressBar = this.createProgressBar(aggregate.overallPercentage);
    
    content += `### Overall Progress: ${progressBar} ${aggregate.overallPercentage}%

- **Total Specifications**: ${aggregate.totalSpecs}
  - ✅ Completed: ${aggregate.completedSpecs}
  - 🚧 In Progress: ${aggregate.totalSpecs - aggregate.completedSpecs - aggregate.blockedSpecs}
  - ⚠️ Blocked: ${aggregate.blockedSpecs}
- **Total Tasks**: ${aggregate.completedTasks}/${aggregate.totalTasks}
- **Total Subtasks**: ${aggregate.completedSubtasks}/${aggregate.totalSubtasks}

## Specifications Progress

| Specification | Status | Progress | Tasks | Issue |
|--------------|--------|----------|-------|-------|
`;
    
    // Sort specs: blocked first, then in-progress, then completed
    const sortedSpecs = [...aggregate.specs].sort((a, b) => {
      const statusOrder = { 'blocked': 0, 'in-progress': 1, 'completed': 2 };
      return statusOrder[a.status] - statusOrder[b.status];
    });
    
    for (const spec of sortedSpecs) {
      const statusIcon = spec.status === 'completed' ? '✅ Completed' : 
                        spec.status === 'blocked' ? '⚠️ Blocked' : '🚧 In Progress';
      const progressBar = this.createProgressBar(spec.tasks.percentage);
      const issueLink = spec.issueNumber ? `[#${spec.issueNumber}](../../issues/${spec.issueNumber})` : '-';
      
      content += `| ${spec.name} | ${statusIcon} | ${progressBar} ${spec.tasks.percentage}% | ${spec.tasks.completed}/${spec.tasks.total} | ${issueLink} |\n`;
    }
    
    // Add blocked items section if any
    const blockedSpecs = aggregate.specs.filter(s => s.tasks.blocked.length > 0);
    if (blockedSpecs.length > 0) {
      content += `
## Blocked Items

`;
      for (const spec of blockedSpecs) {
        for (const blocked of spec.tasks.blocked) {
          content += `### ${spec.name}

- **Task**: ${blocked.task}
- **Reason**: ${blocked.reason}

`;
        }
      }
    }
    
    // Add quick actions
    content += `
## Quick Actions

- **Update Status**: Run \`node .agent-os/status-aggregator.js\`
- **View Specs**: Browse [.agent-os/specs/](./../specs/)
- **Check Roadmap**: View [.agent-os/roadmap.md](./../roadmap.md)

---

*This report is automatically generated by the Agent OS Status Aggregator.*`;
    
    return content;
  }
  
  /**
   * Create a text progress bar
   */
  createProgressBar(percentage) {
    const filled = Math.round(percentage / 10);
    const empty = 10 - filled;
    return '█'.repeat(filled) + '░'.repeat(empty);
  }
  
  /**
   * Main aggregation process
   */
  async run() {
    console.log('Scanning specifications...');
    const specs = await this.scanSpecs();
    console.log(`Found ${specs.length} specifications with tasks.md files`);
    
    console.log('Parsing task files...');
    const aggregate = await this.aggregateProgress(specs);
    
    console.log('Generating status report...');
    const summary = await this.generateSummary(aggregate);
    
    console.log('Writing status file...');
    await fs.mkdir(this.statusDir, { recursive: true });
    await fs.writeFile(this.statusFile, summary, 'utf-8');
    
    console.log(`\n✅ Status updated successfully!`);
    console.log(`📊 Overall Progress: ${aggregate.overallPercentage}%`);
    console.log(`📁 Status file: ${this.statusFile}\n`);
    
    return aggregate;
  }
}

// Run if called directly
if (require.main === module) {
  const aggregator = new StatusAggregator();
  aggregator.run().catch(err => {
    console.error('Error running status aggregator:', err);
    process.exit(1);
  });
}

module.exports = { StatusAggregator };