const fs = require('fs');
const path = require('path');

const tasksFile = path.join(__dirname, 'specs/2025-09-08-agent-os-productivity-improvements/tasks.md');
const content = fs.readFileSync(tasksFile, 'utf-8');
const lines = content.split('\n');

console.log('Parsing tasks from:', tasksFile);
console.log('Total lines:', lines.length);

let parentTasks = 0;
let completedTasks = 0;

lines.forEach((line, index) => {
  // Check for parent tasks
  const parentMatch = line.match(/^- \[([ x])\] (\d+\. .+?)(?:\s*\(Issue: #\d+\))?$/);
  if (parentMatch) {
    parentTasks++;
    if (parentMatch[1] === 'x') {
      completedTasks++;
    }
    console.log(`Line ${index + 1}: Found task [${parentMatch[1]}] ${parentMatch[2].substring(0, 40)}...`);
  }
});

console.log(`\nSummary: ${completedTasks}/${parentTasks} tasks completed`);

// Also test the StatusAggregator directly
const { StatusAggregator } = require('./status-aggregator');
const aggregator = new StatusAggregator();

aggregator.parseTasksFile(tasksFile).then(result => {
  console.log('\nStatusAggregator result:', result);
});