/**
 * Task Generator for Agent OS
 * Generates tasks from templates
 */

const TemplateEngine = require('./template-engine');
const path = require('path');

class TaskGenerator {
  constructor() {
    this.engine = new TemplateEngine();
  }

  /**
   * Generate tasks from a template
   */
  fromTemplate(templatePath, variables) {
    const template = this.engine.loadTemplate(templatePath);
    const processed = this.engine.process(template, variables);
    
    // Parse tasks from processed template
    return this.parseTasks(processed);
  }

  /**
   * Parse tasks from template content
   */
  parseTasks(content) {
    const tasks = [];
    const lines = content.split('\n');
    let currentTask = null;
    let currentSubtask = null;
    
    for (const line of lines) {
      // Parent task (e.g., "- [ ] 1. Task description")
      const taskMatch = line.match(/^-\s*\[\s*\]\s*(\d+)\.\s*(.+)/);
      if (taskMatch) {
        if (currentTask) {
          tasks.push(currentTask);
        }
        currentTask = {
          number: parseInt(taskMatch[1]),
          description: taskMatch[2].trim(),
          subtasks: []
        };
        currentSubtask = null;
        continue;
      }
      
      // Subtask (e.g., "  - [ ] 1.1 Subtask description")
      const subtaskMatch = line.match(/^\s+-\s*\[\s*\]\s*(\d+\.\d+)\s*(.+)/);
      if (subtaskMatch && currentTask) {
        currentSubtask = {
          number: subtaskMatch[1],
          description: subtaskMatch[2].trim()
        };
        currentTask.subtasks.push(currentSubtask);
      }
    }
    
    if (currentTask) {
      tasks.push(currentTask);
    }
    
    return tasks;
  }

  /**
   * Generate CRUD tasks for an entity
   */
  generateCrudTasks(entityName, properties) {
    return this.fromTemplate('backend/crud-feature.md', {
      EntityName: entityName,
      Properties: properties,
      EntityNameLower: entityName.toLowerCase(),
      EntityNamePlural: this.pluralize(entityName)
    });
  }

  /**
   * Generate component tasks
   */
  generateComponentTasks(componentName, type = 'list') {
    const templateMap = {
      'list': 'frontend/angular-list.md',
      'form': 'frontend/angular-form.md',
      'detail': 'frontend/angular-detail.md'
    };
    
    return this.fromTemplate(templateMap[type] || 'frontend/angular-component.md', {
      ComponentName: componentName,
      ComponentNameLower: componentName.toLowerCase()
    });
  }

  /**
   * Simple pluralization
   */
  pluralize(word) {
    if (word.endsWith('y')) {
      return word.slice(0, -1) + 'ies';
    }
    if (word.endsWith('s') || word.endsWith('x') || word.endsWith('ch') || word.endsWith('sh')) {
      return word + 'es';
    }
    return word + 's';
  }
}

module.exports = TaskGenerator;