/**
 * Agent OS Template Engine
 * Provides variable substitution, composition, and inheritance for templates
 */

const fs = require('fs');
const path = require('path');

class TemplateEngine {
  constructor(templatesDir) {
    this.templatesDir = templatesDir || path.join(__dirname, 'templates');
    this.cache = new Map();
    this.typeMapping = {
      'string': { typescript: 'string' },
      'int': { typescript: 'number' },
      'long': { typescript: 'number' },
      'decimal': { typescript: 'number' },
      'float': { typescript: 'number' },
      'double': { typescript: 'number' },
      'bool': { typescript: 'boolean' },
      'boolean': { typescript: 'boolean' },
      'DateTime': { typescript: 'Date' },
      'DateTimeOffset': { typescript: 'Date' },
      'Guid': { typescript: 'string' },
      'byte[]': { typescript: 'Uint8Array' },
      'object': { typescript: 'any' },
      'dynamic': { typescript: 'any' }
    };
  }

  /**
   * Load a template from file
   */
  loadTemplate(templatePath) {
    const fullPath = path.isAbsolute(templatePath) 
      ? templatePath 
      : path.join(this.templatesDir, templatePath);
    
    if (this.cache.has(fullPath)) {
      return this.cache.get(fullPath);
    }
    
    if (!fs.existsSync(fullPath)) {
      throw new Error(`Template not found: ${templatePath}`);
    }
    
    const content = fs.readFileSync(fullPath, 'utf8');
    this.cache.set(fullPath, content);
    return content;
  }

  /**
   * Substitute variables in template
   */
  substitute(template, variables = {}) {
    let result = template;
    
    // Simple variable substitution ${var}
    result = result.replace(/\$\{([^}]+)\}/g, (match, expr) => {
      const value = this.evaluateExpression(expr.trim(), variables);
      return value !== undefined ? value : match;
    });
    
    // Conditional sections ${condition ? true : false}
    result = this.processConditionals(result, variables);
    
    // Array/loop processing ${array.map(...)}
    result = this.processArrays(result, variables);
    
    return result;
  }

  /**
   * Evaluate expressions including nested properties
   */
  evaluateExpression(expr, context) {
    // Handle ternary operator
    if (expr.includes('?')) {
      const [condition, branches] = expr.split('?').map(s => s.trim());
      const [trueBranch, falseBranch] = branches.split(':').map(s => s.trim());
      const conditionValue = this.evaluateExpression(condition, context);
      return conditionValue ? 
        this.evaluateExpression(trueBranch, context) : 
        this.evaluateExpression(falseBranch, context);
    }
    
    // Handle array methods
    if (expr.includes('.map(')) {
      return this.evaluateArrayExpression(expr, context);
    }
    
    // Handle nested properties (e.g., Project.Namespace)
    const parts = expr.split('.');
    let value = context;
    
    for (const part of parts) {
      if (value === undefined || value === null) return undefined;
      value = value[part];
    }
    
    // Handle string literals
    if (expr.startsWith("'") && expr.endsWith("'")) {
      return expr.slice(1, -1);
    }
    
    return value;
  }

  /**
   * Process conditional sections
   */
  processConditionals(template, variables) {
    const conditionalRegex = /\$\{([^?]+)\s*\?\s*'([^']*)'\s*:\s*'([^']*)'\}/g;
    
    return template.replace(conditionalRegex, (match, condition, trueBranch, falseBranch) => {
      const conditionValue = this.evaluateExpression(condition.trim(), variables);
      return conditionValue ? trueBranch : falseBranch;
    });
  }

  /**
   * Process array expressions
   */
  processArrays(template, variables) {
    const arrayRegex = /\$\{([^.]+)\.map\(([^)]+)\s*=>\s*([^)]+)\)\.join\(['"]([^'"]+)['"]\)\}/g;
    
    return template.replace(arrayRegex, (match, arrayName, itemVar, expression, separator) => {
      const array = variables[arrayName];
      if (!Array.isArray(array)) return match;
      
      const processedSeparator = separator.replace(/\\n/g, '\n').replace(/\\t/g, '\t');
      
      return array.map(item => {
        const itemContext = { ...variables, [itemVar]: item };
        return this.evaluateArrayItemExpression(expression, itemContext, itemVar);
      }).join(processedSeparator);
    });
  }

  /**
   * Evaluate array item expressions
   */
  evaluateArrayItemExpression(expr, context, itemVar) {
    // Handle string concatenation
    const parts = expr.split('+').map(part => part.trim());
    return parts.map(part => {
      if (part.startsWith("'") && part.endsWith("'")) {
        return part.slice(1, -1);
      }
      return this.evaluateExpression(part, context);
    }).join('');
  }

  /**
   * Evaluate array expressions for direct evaluation
   */
  evaluateArrayExpression(expr, context) {
    const match = expr.match(/([^.]+)\.map\(([^)]+)\s*=>\s*([^)]+)\)\.join\(['"]([^'"]+)['"]\)/);
    if (!match) return undefined;
    
    const [, arrayName, itemVar, expression, separator] = match;
    const array = context[arrayName];
    if (!Array.isArray(array)) return undefined;
    
    const processedSeparator = separator.replace(/\\n/g, '\n').replace(/\\t/g, '\t');
    
    return array.map(item => {
      const itemContext = { ...context, [itemVar]: item };
      return this.evaluateArrayItemExpression(expression, itemContext, itemVar);
    }).join(processedSeparator);
  }

  /**
   * Compose template with includes
   */
  compose(template, variables = {}) {
    let result = template;
    const includeRegex = /\{\{include:([^}]+)\}\}/g;
    
    result = result.replace(includeRegex, (match, includePath) => {
      try {
        const includeContent = this.loadTemplate(includePath.trim());
        return this.compose(includeContent, variables); // Recursive for nested includes
      } catch (error) {
        console.error(`Failed to include template: ${includePath}`, error);
        return match;
      }
    });
    
    return this.substitute(result, variables);
  }

  /**
   * Process template with inheritance
   */
  process(template, variables = {}) {
    let result = template;
    
    // Handle extends
    const extendsMatch = template.match(/\{\{extends:([^}]+)\}\}/);
    if (extendsMatch) {
      const parentPath = extendsMatch[1].trim();
      const parentTemplate = this.loadTemplate(parentPath);
      result = this.mergeTemplates(parentTemplate, template);
    }
    
    // Handle blocks
    result = this.processBlocks(result, variables);
    
    // Compose and substitute
    return this.compose(result, variables);
  }

  /**
   * Merge parent and child templates
   */
  mergeTemplates(parent, child) {
    let result = parent;
    
    // Extract blocks from child
    const blockRegex = /\{\{block:([^}]+)\}\}([\s\S]*?)\{\{\/block\}\}/g;
    const childBlocks = {};
    
    let match;
    while ((match = blockRegex.exec(child)) !== null) {
      childBlocks[match[1]] = match[2];
    }
    
    // Replace parent blocks with child blocks
    result = result.replace(blockRegex, (match, blockName, blockContent) => {
      return childBlocks[blockName] ? 
        `{{block:${blockName}}}${childBlocks[blockName]}{{/block}}` : 
        match;
    });
    
    return result;
  }

  /**
   * Process block sections
   */
  processBlocks(template, variables) {
    const blockRegex = /\{\{block:([^}]+)\}\}([\s\S]*?)\{\{\/block\}\}/g;
    
    return template.replace(blockRegex, (match, blockName, blockContent) => {
      // Check for override in variables
      if (variables.Override && variables.Override[blockName]) {
        return variables.Override[blockName];
      }
      return blockContent;
    });
  }

  /**
   * Process a template file
   */
  processTemplate(templatePath, variables = {}) {
    const template = this.loadTemplate(templatePath);
    return this.process(template, variables);
  }

  /**
   * Map types between languages
   */
  mapType(csharpType, targetLang = 'typescript') {
    if (targetLang !== 'typescript') {
      throw new Error(`Unsupported target language: ${targetLang}`);
    }
    
    // Handle nullable types
    const isNullable = csharpType.endsWith('?');
    let baseType = isNullable ? csharpType.slice(0, -1) : csharpType;
    
    // Handle generic collections
    const listMatch = baseType.match(/^(?:List|IList|IEnumerable|ICollection)<(.+)>$/);
    if (listMatch) {
      const innerType = this.mapType(listMatch[1], targetLang);
      return `${innerType}[]`;
    }
    
    const dictMatch = baseType.match(/^(?:Dictionary|IDictionary)<(.+),\s*(.+)>$/);
    if (dictMatch) {
      const keyType = this.mapType(dictMatch[1], targetLang);
      const valueType = this.mapType(dictMatch[2], targetLang);
      return `Record<${keyType}, ${valueType}>`;
    }
    
    // Map base type
    const mapping = this.typeMapping[baseType];
    let mappedType = mapping ? mapping[targetLang] : baseType;
    
    // Add nullable suffix if needed
    if (isNullable) {
      mappedType = `${mappedType} | null`;
    }
    
    return mappedType;
  }

  /**
   * Validate required variables
   */
  validate(template, variables) {
    const requiredMatch = template.match(/\{\{required:([^}]+)\}\}/);
    if (!requiredMatch) return true;
    
    const required = requiredMatch[1].split(',').map(v => v.trim());
    const missing = required.filter(v => !(v in variables));
    
    if (missing.length > 0) {
      throw new Error(`Missing required variable: ${missing[0]}`);
    }
    
    return true;
  }
}

module.exports = TemplateEngine;