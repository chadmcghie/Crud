const fs = require('fs').promises;
const path = require('path');

class LearningCache {
  constructor(learningDir = '.agents/.agent-os/learning') {
    this.learningDir = learningDir;
    this.patternsFile = path.join(learningDir, 'patterns.md');
    this.memoryRefsFile = path.join(learningDir, 'memory-references.md');
    this.patterns = [];
    this.memoryRefs = [];
  }
  
  /**
   * Add a new pattern to the cache
   */
  async addPattern(pattern) {
    await this.loadPatterns();
    
    // Check if pattern already exists
    const existingIndex = this.patterns.findIndex(p => p.id === pattern.id);
    if (existingIndex >= 0) {
      this.patterns[existingIndex] = pattern;
    } else {
      this.patterns.push(pattern);
    }
    
    await this.savePatterns();
    return pattern;
  }
  
  /**
   * Get a specific pattern by ID
   */
  async getPattern(id) {
    await this.loadPatterns();
    return this.patterns.find(p => p.id === id);
  }
  
  /**
   * Search patterns by category
   */
  async searchByCategory(category) {
    await this.loadPatterns();
    return this.patterns.filter(p => p.category === category);
  }
  
  /**
   * Search patterns by tag
   */
  async searchByTag(tag) {
    await this.loadPatterns();
    return this.patterns.filter(p => 
      p.tags && p.tags.includes(tag)
    );
  }
  
  /**
   * Add a memory reference
   */
  async addMemoryReference(ref) {
    await this.loadMemoryRefs();
    
    const existingIndex = this.memoryRefs.findIndex(r => r.id === ref.id);
    if (existingIndex >= 0) {
      this.memoryRefs[existingIndex] = ref;
    } else {
      this.memoryRefs.push(ref);
    }
    
    await this.saveMemoryRefs();
    return ref;
  }
  
  /**
   * Get a memory reference by ID
   */
  async getMemoryReference(id) {
    await this.loadMemoryRefs();
    return this.memoryRefs.find(r => r.id === id);
  }
  
  /**
   * Get all memory references
   */
  async getAllMemoryReferences() {
    await this.loadMemoryRefs();
    return this.memoryRefs;
  }
  
  /**
   * Learn from a resolved issue
   */
  async learnFromIssue(issue) {
    const pattern = {
      id: this.generateId(issue.title),
      category: this.categorizeIssue(issue),
      problem: issue.title,
      solution: issue.resolution,
      tags: issue.labels || [],
      dateAdded: new Date().toISOString()
    };
    
    if (issue.body) {
      pattern.symptoms = this.extractSymptoms(issue.body);
    }
    
    await this.addPattern(pattern);
    return pattern;
  }
  
  /**
   * Generate patterns.md file content
   */
  async generatePatternsFile() {
    await this.loadPatterns();
    
    let content = '# Agent OS Learning Patterns\n\n';
    content += `> Last Updated: ${new Date().toISOString()}\n`;
    content += '> Auto-generated from resolved issues and development experience\n\n';
    
    // Group patterns by category
    const categories = {};
    this.patterns.forEach(pattern => {
      if (!categories[pattern.category]) {
        categories[pattern.category] = [];
      }
      categories[pattern.category].push(pattern);
    });
    
    // Generate content for each category
    for (const [category, patterns] of Object.entries(categories)) {
      content += `## ${this.formatCategoryName(category)}\n\n`;
      
      for (const pattern of patterns) {
        content += `### ${pattern.problem}\n`;
        content += `**ID**: \`${pattern.id}\`  \n`;
        content += `**Problem**: ${pattern.problem}  \n`;
        
        if (pattern.symptoms) {
          content += `**Symptoms**:\n`;
          pattern.symptoms.forEach(symptom => {
            content += `- ${symptom}\n`;
          });
        }
        
        if (pattern.solution) {
          content += `\n**Solution**:\n${pattern.solution}\n`;
        }
        
        if (pattern.tags && pattern.tags.length > 0) {
          content += `\n**Tags**: ${pattern.tags.map(t => `\`${t}\``).join(', ')}  \n`;
        }
        
        if (pattern.examples && pattern.examples.length > 0) {
          content += `**Examples**: ${pattern.examples.join(', ')}  \n`;
        }
        
        content += '\n---\n\n';
      }
    }
    
    return content;
  }
  
  /**
   * Generate memory-references.md file content
   */
  async generateMemoryReferencesFile() {
    await this.loadMemoryRefs();
    
    let content = '# Agent OS Memory References\n\n';
    content += '> Important conversation and decision references for Agent OS context\n';
    content += `> Last Updated: ${new Date().toISOString()}\n\n`;
    
    for (const ref of this.memoryRefs) {
      content += `## ${ref.description}\n`;
      content += `**Memory ID**: \`${ref.memoryId}\`  \n`;
      
      if (ref.context) {
        content += `**Context**: ${ref.context}  \n`;
      }
      
      if (ref.tags && ref.tags.length > 0) {
        content += `**Tags**: ${ref.tags.map(t => `\`${t}\``).join(', ')}  \n`;
      }
      
      if (ref.dateAdded) {
        content += `**Date Added**: ${ref.dateAdded}  \n`;
      }
      
      content += '\n';
    }
    
    return content;
  }
  
  // Helper methods
  
  async loadPatterns() {
    try {
      // For now, patterns are stored in memory
      // In future, could parse from markdown file
      return this.patterns;
    } catch (err) {
      this.patterns = [];
      return this.patterns;
    }
  }
  
  async savePatterns() {
    const content = await this.generatePatternsFile();
    await fs.mkdir(this.learningDir, { recursive: true });
    await fs.writeFile(this.patternsFile, content, 'utf-8');
  }
  
  async loadMemoryRefs() {
    try {
      // For now, refs are stored in memory
      // In future, could parse from markdown file
      return this.memoryRefs;
    } catch (err) {
      this.memoryRefs = [];
      return this.memoryRefs;
    }
  }
  
  async saveMemoryRefs() {
    const content = await this.generateMemoryReferencesFile();
    await fs.mkdir(this.learningDir, { recursive: true });
    await fs.writeFile(this.memoryRefsFile, content, 'utf-8');
  }
  
  generateId(title) {
    return title
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
      .substring(0, 50);
  }
  
  categorizeIssue(issue) {
    const labels = issue.labels || [];
    
    if (labels.includes('testing') || labels.includes('test')) {
      return 'testing';
    }
    if (labels.includes('build') || labels.includes('ci')) {
      return 'build';
    }
    if (labels.includes('bug') || labels.includes('error')) {
      return 'errors';
    }
    if (labels.includes('performance')) {
      return 'performance';
    }
    if (labels.includes('security')) {
      return 'security';
    }
    
    return 'general';
  }
  
  extractSymptoms(body) {
    const symptoms = [];
    const lines = body.split('\n');
    
    for (const line of lines) {
      // Look for error messages or symptoms
      if (line.includes('Error:') || line.includes('error:')) {
        symptoms.push(line.trim());
      }
      if (line.includes('Failed') || line.includes('failed')) {
        symptoms.push(line.trim());
      }
    }
    
    return symptoms;
  }
  
  formatCategoryName(category) {
    return category
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ') + ' Patterns';
  }
}

module.exports = { LearningCache };