const fs = require('fs').promises;
const path = require('path');

class StatusTracker {
  constructor(statusDir = '.agents/.agent-os/status') {
    this.statusDir = statusDir;
    this.statusFile = path.join(statusDir, 'current-status.md');
  }
  
  /**
   * Parse a tasks.md file to extract completion status
   */
  async parseTasksFile(content) {
    const lines = content.split('\n');
    const result = {
      total: 0,
      completed: 0,
      percentage: 0,
      subtasks: {
        total: 0,
        completed: 0
      },
      blocked: []
    };
    
    let currentTask = null;
    let blockingReason = null;
    
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      
      // Parse parent tasks (- [ ] or - [x])
      const parentMatch = line.match(/^- \[([ x])\] (\d+\. .+)/);
      if (parentMatch) {
        result.total++;
        // Remove the Issue part from the task name if present
        currentTask = parentMatch[2].replace(/\s*\(Issue: #\d+\)$/, '');
        
        if (parentMatch[1] === 'x') {
          result.completed++;
        }
        
        // Check for blocking indicator on next line
        if (i + 1 < lines.length && lines[i + 1].includes('⚠️ Blocking issue:')) {
          blockingReason = lines[i + 1].replace(/.*⚠️ Blocking issue:\s*/, '').trim();
          result.blocked.push({
            task: currentTask,
            reason: blockingReason
          });
        }
      }
      
      // Parse subtasks (  - [ ] or   - [x])
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
  }
  
  /**
   * Create the status markdown file content
   */
  async createStatusFile(status) {
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
    
    let content = `# Agent OS Progress Status

> Last Updated: ${readableDate}
> Generated: ${timestamp}

## Overview

`;
    
    // Calculate overall statistics
    const totalSpecs = status.activeSpecs.length;
    const completedSpecs = status.activeSpecs.filter(s => s.status === 'completed').length;
    const totalTasks = status.activeSpecs.reduce((sum, s) => sum + s.tasks.total, 0);
    const completedTasks = status.activeSpecs.reduce((sum, s) => sum + s.tasks.completed, 0);
    const overallPercentage = totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;
    
    content += `- **Active Specifications**: ${totalSpecs} (${completedSpecs} completed)
- **Total Tasks**: ${completedTasks}/${totalTasks} (${overallPercentage}% complete)
- **Blocked Items**: ${status.blockedItems.length}

## Active Specifications

| Specification | Status | Progress | Tasks |
|--------------|--------|----------|-------|
`;
    
    // Add active specs
    for (const spec of status.activeSpecs) {
      const statusIcon = spec.status === 'completed' ? '✅' : 
                        spec.status === 'blocked' ? '⚠️' : '🚧';
      const progressBar = this.createProgressBar(spec.tasks.percentage);
      
      content += `| ${spec.name} | ${statusIcon} ${this.capitalize(spec.status)} | ${progressBar} ${spec.tasks.percentage}% | ${spec.tasks.completed}/${spec.tasks.total} |\n`;
    }
    
    // Add review results if any
    if (status.reviewResults && status.reviewResults.length > 0) {
      content += `
## Recent Review Results

| Specification | Date | Critical | Warnings | Suggestions |
|--------------|------|----------|----------|-------------|
`;
      
      for (const review of status.reviewResults) {
        const reviewDate = new Date(review.date).toLocaleDateString();
        content += `| ${review.spec} | ${reviewDate} | ${review.critical} | ${review.warnings} | ${review.suggestions} |\n`;
      }
    }
    
    // Add blocked items if any
    if (status.blockedItems && status.blockedItems.length > 0) {
      content += `
## Blocked Items

`;
      
      for (const blocked of status.blockedItems) {
        const blockedSince = new Date(blocked.since).toLocaleDateString();
        content += `### ${blocked.spec}

- **Task**: ${blocked.task}
- **Reason**: ${blocked.reason}
- **Since**: ${blockedSince}

`;
      }
    }
    
    // Add quick links
    content += `
## Quick Links

- [Specifications Directory](./../specs/)
- [Standards & Best Practices](./../standards/)
- [Instructions](./../instructions/)
- [Templates](./../templates/)

---

*This status is automatically updated when tasks are executed or reviewed.*`;
    
    return content;
  }
  
  /**
   * Update the status file with current information
   */
  async updateStatus(specPath, tasksContent) {
    // Parse the spec name from path
    const specName = path.basename(specPath).replace(/^\d{4}-\d{2}-\d{2}-/, '');
    
    // Parse tasks
    const tasks = await this.parseTasksFile(tasksContent);
    
    // Determine status
    let status = 'in-progress';
    if (tasks.percentage === 100) {
      status = 'completed';
    } else if (tasks.blocked.length > 0) {
      status = 'blocked';
    }
    
    // Read existing status or create new
    let currentStatus = {
      lastUpdated: new Date().toISOString(),
      activeSpecs: [],
      reviewResults: [],
      blockedItems: []
    };
    
    try {
      const existingContent = await fs.readFile(this.statusFile, 'utf-8');
      // Parse existing status if needed (simplified for now)
    } catch (err) {
      // File doesn't exist yet
    }
    
    // Update or add spec
    const specIndex = currentStatus.activeSpecs.findIndex(s => s.name === specName);
    const specData = {
      name: specName,
      path: specPath,
      tasks: tasks,
      status: status
    };
    
    if (specIndex >= 0) {
      currentStatus.activeSpecs[specIndex] = specData;
    } else {
      currentStatus.activeSpecs.push(specData);
    }
    
    // Update blocked items
    currentStatus.blockedItems = currentStatus.blockedItems.filter(b => b.spec !== specName);
    for (const blocked of tasks.blocked) {
      currentStatus.blockedItems.push({
        spec: specName,
        task: blocked.task,
        reason: blocked.reason,
        since: new Date().toISOString()
      });
    }
    
    // Write updated status
    const content = await this.createStatusFile(currentStatus);
    await fs.mkdir(this.statusDir, { recursive: true });
    await fs.writeFile(this.statusFile, content, 'utf-8');
    
    return currentStatus;
  }
  
  /**
   * Create a simple text progress bar
   */
  createProgressBar(percentage) {
    const filled = Math.round(percentage / 10);
    const empty = 10 - filled;
    return '█'.repeat(filled) + '░'.repeat(empty);
  }
  
  /**
   * Capitalize first letter of string
   */
  capitalize(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }
}

module.exports = { StatusTracker };