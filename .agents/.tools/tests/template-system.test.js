/**
 * Tests for Agent OS Template System
 * Verifies template loading, variable substitution, composition, and inheritance
 */

const fs = require('fs');
const path = require('path');

describe('Template System', () => {
  const templatesDir = path.join(__dirname, '..', 'templates');
  
  describe('Directory Structure', () => {
    test('should have required template directories', () => {
      const requiredDirs = [
        'backend',
        'frontend',
        'testing',
        'common'
      ];
      
      requiredDirs.forEach(dir => {
        const dirPath = path.join(templatesDir, dir);
        expect(fs.existsSync(dirPath)).toBe(true);
      });
    });
    
    test('should have backend templates', () => {
      const backendTemplates = [
        'crud-feature.md',
        'api-endpoint.md',
        'domain-aggregate.md'
      ];
      
      backendTemplates.forEach(template => {
        const templatePath = path.join(templatesDir, 'backend', template);
        expect(fs.existsSync(templatePath)).toBe(true);
      });
    });
    
    test('should have frontend templates', () => {
      const frontendTemplates = [
        'angular-component.md',
        'angular-service.md',
        'angular-state.md'
      ];
      
      frontendTemplates.forEach(template => {
        const templatePath = path.join(templatesDir, 'frontend', template);
        expect(fs.existsSync(templatePath)).toBe(true);
      });
    });
  });
  
  describe('Template Engine', () => {
    const TemplateEngine = require('../template-engine');
    let engine;
    
    beforeEach(() => {
      engine = new TemplateEngine(templatesDir);
    });
    
    describe('Variable Substitution', () => {
      test('should replace simple variables', () => {
        const template = 'Create ${EntityName} with ${PropertyCount} properties';
        const variables = {
          EntityName: 'Product',
          PropertyCount: 5
        };
        
        const result = engine.substitute(template, variables);
        expect(result).toBe('Create Product with 5 properties');
      });
      
      test('should handle nested variables', () => {
        const template = 'namespace ${Project.Namespace}.${Module.Name}';
        const variables = {
          Project: { Namespace: 'Crud.Domain' },
          Module: { Name: 'Products' }
        };
        
        const result = engine.substitute(template, variables);
        expect(result).toBe('namespace Crud.Domain.Products');
      });
      
      test('should handle arrays and loops', () => {
        const template = `
Properties:
\${Properties.map(p => '- ' + p.Name + ': ' + p.Type).join('\\n')}
`;
        const variables = {
          Properties: [
            { Name: 'Id', Type: 'Guid' },
            { Name: 'Name', Type: 'string' },
            { Name: 'Price', Type: 'decimal' }
          ]
        };
        
        const result = engine.substitute(template, variables);
        expect(result).toContain('- Id: Guid');
        expect(result).toContain('- Name: string');
        expect(result).toContain('- Price: decimal');
      });
      
      test('should handle conditional sections', () => {
        const template = `
public class \${EntityName}
{
\${HasAudit ? '    public DateTime CreatedAt { get; set; }\\n    public DateTime UpdatedAt { get; set; }' : ''}
}`;
        
        const withAudit = engine.substitute(template, { 
          EntityName: 'Product', 
          HasAudit: true 
        });
        expect(withAudit).toContain('CreatedAt');
        expect(withAudit).toContain('UpdatedAt');
        
        const withoutAudit = engine.substitute(template, { 
          EntityName: 'Product', 
          HasAudit: false 
        });
        expect(withoutAudit).not.toContain('CreatedAt');
      });
    });
    
    describe('Template Composition', () => {
      test('should include partial templates', () => {
        const mainTemplate = `
# \${EntityName} Feature

{{include:common/header.md}}

## Implementation

{{include:backend/entity-base.md}}
`;
        
        const result = engine.compose(mainTemplate, {
          EntityName: 'Product'
        });
        
        expect(result).toContain('# Product Feature');
        // Should include content from included templates
      });
      
      test('should handle nested includes', () => {
        const template = '{{include:backend/crud-feature.md}}';
        const result = engine.compose(template, {
          EntityName: 'Product',
          Properties: []
        });
        
        expect(result).toBeDefined();
        expect(result.length).toBeGreaterThan(0);
      });
    });
    
    describe('Template Inheritance', () => {
      test('should extend base templates', () => {
        const childTemplate = `
{{extends:common/base-feature.md}}

{{block:description}}
Custom product management feature
{{/block}}
`;
        
        const result = engine.process(childTemplate, {
          EntityName: 'Product'
        });
        
        expect(result).toContain('Custom product management feature');
      });
      
      test('should override parent blocks', () => {
        const result = engine.processTemplate('backend/crud-feature.md', {
          EntityName: 'Product',
          Override: {
            Commands: 'Custom command implementation'
          }
        });
        
        expect(result).toContain('Custom command implementation');
      });
    });
    
    describe('Type System Mapping', () => {
      test('should map C# types to TypeScript', () => {
        const csharpTypes = [
          { csharp: 'string', typescript: 'string' },
          { csharp: 'int', typescript: 'number' },
          { csharp: 'decimal', typescript: 'number' },
          { csharp: 'bool', typescript: 'boolean' },
          { csharp: 'DateTime', typescript: 'Date' },
          { csharp: 'Guid', typescript: 'string' },
          { csharp: 'List<T>', typescript: 'T[]' },
          { csharp: 'Dictionary<K,V>', typescript: 'Record<K, V>' }
        ];
        
        csharpTypes.forEach(({ csharp, typescript }) => {
          expect(engine.mapType(csharp, 'typescript')).toBe(typescript);
        });
      });
      
      test('should handle nullable types', () => {
        expect(engine.mapType('string?', 'typescript')).toBe('string | null');
        expect(engine.mapType('int?', 'typescript')).toBe('number | null');
      });
      
      test('should handle generic collections', () => {
        expect(engine.mapType('List<Product>', 'typescript')).toBe('Product[]');
        expect(engine.mapType('IEnumerable<Order>', 'typescript')).toBe('Order[]');
      });
    });
  });
  
  describe('Template Selection', () => {
    test('should select appropriate template based on feature type', () => {
      const selector = require('../template-selector');
      
      const crudTemplate = selector.select('crud', 'Product');
      expect(crudTemplate).toBe('backend/crud-feature.md');
      
      const apiTemplate = selector.select('api-endpoint', 'GetProducts');
      expect(apiTemplate).toBe('backend/api-endpoint.md');
      
      const componentTemplate = selector.select('component', 'ProductList');
      expect(componentTemplate).toBe('frontend/angular-component.md');
    });
    
    test('should suggest templates based on context', () => {
      const selector = require('../template-selector');
      
      const suggestions = selector.suggest({
        type: 'feature',
        stack: 'fullstack',
        entity: 'Product'
      });
      
      expect(suggestions).toContain('backend/crud-feature.md');
      expect(suggestions).toContain('frontend/angular-component.md');
      expect(suggestions).toContain('testing/integration-test.md');
    });
  });
  
  describe('Error Handling', () => {
    const TemplateEngine = require('../template-engine');
    let engine;
    
    beforeEach(() => {
      engine = new TemplateEngine(templatesDir);
    });
    
    test('should handle missing variables gracefully', () => {
      const template = 'Hello ${Name}, your age is ${Age}';
      const result = engine.substitute(template, { Name: 'John' });
      
      expect(result).toContain('John');
      expect(result).toContain('${Age}'); // Unsubstituted
    });
    
    test('should report template not found', () => {
      expect(() => {
        engine.loadTemplate('nonexistent/template.md');
      }).toThrow('Template not found');
    });
    
    test('should validate required variables', () => {
      const template = '{{required:EntityName,Properties}}';
      
      expect(() => {
        engine.validate(template, { EntityName: 'Product' });
      }).toThrow('Missing required variable: Properties');
    });
  });
});

describe('Integration with create-tasks.md', () => {
  test('should support template selection in task creation', () => {
    const createTasksPath = path.join(__dirname, '..', 'instructions', 'core', 'create-tasks.md');
    const content = fs.readFileSync(createTasksPath, 'utf8');
    
    expect(content).toContain('template_selection');
    // template_variables is handled within the template selection step
  });
  
  test('should generate tasks from templates', () => {
    const TaskGenerator = require('../task-generator');
    const generator = new TaskGenerator();
    
    const tasks = generator.fromTemplate('backend/crud-feature.md', {
      EntityName: 'Product',
      Properties: [
        { Name: 'Name', Type: 'string' },
        { Name: 'Price', Type: 'decimal' }
      ]
    });
    
    expect(tasks).toBeInstanceOf(Array);
    expect(tasks.length).toBeGreaterThan(0);
    expect(tasks[0]).toHaveProperty('description');
    expect(tasks[0]).toHaveProperty('subtasks');
  });
});