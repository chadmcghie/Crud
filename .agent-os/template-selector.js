/**
 * Template Selector for Agent OS
 * Helps select appropriate templates based on feature type and context
 */

const templateMappings = {
  // Backend templates
  'crud': 'backend/crud-feature.md',
  'crud-feature': 'backend/crud-feature.md',
  'api-endpoint': 'backend/api-endpoint.md',
  'api': 'backend/api-endpoint.md',
  'domain-aggregate': 'backend/domain-aggregate.md',
  'aggregate': 'backend/domain-aggregate.md',
  'entity': 'backend/domain-aggregate.md',
  'command': 'backend/command-handler.md',
  'query': 'backend/query-handler.md',
  'repository': 'backend/repository.md',
  'service': 'backend/service.md',
  
  // Frontend templates
  'component': 'frontend/angular-component.md',
  'angular-component': 'frontend/angular-component.md',
  'angular-service': 'frontend/angular-service.md',
  'angular-state': 'frontend/angular-state.md',
  'form': 'frontend/angular-form.md',
  'list': 'frontend/angular-list.md',
  'detail': 'frontend/angular-detail.md',
  
  // Testing templates
  'unit-test': 'testing/unit-test.md',
  'integration-test': 'testing/integration-test.md',
  'e2e-test': 'testing/e2e-test.md',
  'test': 'testing/unit-test.md',
  
  // Common templates
  'feature': 'common/full-feature.md',
  'bug-fix': 'common/bug-fix.md',
  'refactoring': 'common/refactoring.md',
  'documentation': 'common/documentation.md'
};

class TemplateSelector {
  /**
   * Select a template based on type
   */
  select(type, entityName) {
    const normalizedType = type.toLowerCase().replace(/[_\s]+/g, '-');
    const template = templateMappings[normalizedType];
    
    if (!template) {
      throw new Error(`No template found for type: ${type}`);
    }
    
    return template;
  }

  /**
   * Suggest templates based on context
   */
  suggest(context) {
    const suggestions = [];
    const { type, stack, entity, operation, layer } = context;
    
    // Feature-based suggestions
    if (type === 'feature') {
      if (stack === 'fullstack' || stack === 'full-stack') {
        suggestions.push('backend/crud-feature.md');
        suggestions.push('frontend/angular-component.md');
        suggestions.push('frontend/angular-service.md');
        suggestions.push('testing/integration-test.md');
        suggestions.push('testing/e2e-test.md');
      } else if (stack === 'backend') {
        suggestions.push('backend/crud-feature.md');
        suggestions.push('backend/api-endpoint.md');
        suggestions.push('backend/domain-aggregate.md');
        suggestions.push('testing/integration-test.md');
      } else if (stack === 'frontend') {
        suggestions.push('frontend/angular-component.md');
        suggestions.push('frontend/angular-service.md');
        suggestions.push('frontend/angular-state.md');
        suggestions.push('testing/e2e-test.md');
      }
    }
    
    // Operation-based suggestions
    if (operation) {
      switch (operation.toLowerCase()) {
        case 'create':
        case 'add':
        case 'insert':
          suggestions.push('backend/command-handler.md');
          suggestions.push('frontend/angular-form.md');
          break;
        case 'read':
        case 'get':
        case 'list':
        case 'fetch':
          suggestions.push('backend/query-handler.md');
          suggestions.push('frontend/angular-list.md');
          break;
        case 'update':
        case 'edit':
        case 'modify':
          suggestions.push('backend/command-handler.md');
          suggestions.push('frontend/angular-form.md');
          break;
        case 'delete':
        case 'remove':
          suggestions.push('backend/command-handler.md');
          break;
      }
    }
    
    // Layer-based suggestions
    if (layer) {
      switch (layer.toLowerCase()) {
        case 'domain':
          suggestions.push('backend/domain-aggregate.md');
          break;
        case 'application':
        case 'app':
          suggestions.push('backend/command-handler.md');
          suggestions.push('backend/query-handler.md');
          break;
        case 'infrastructure':
          suggestions.push('backend/repository.md');
          break;
        case 'api':
        case 'web':
          suggestions.push('backend/api-endpoint.md');
          break;
        case 'ui':
        case 'frontend':
          suggestions.push('frontend/angular-component.md');
          break;
      }
    }
    
    // Remove duplicates
    return [...new Set(suggestions)];
  }

  /**
   * Get all available templates
   */
  getAllTemplates() {
    return Object.values(templateMappings);
  }

  /**
   * Get templates by category
   */
  getTemplatesByCategory(category) {
    const templates = [];
    const prefix = category.toLowerCase() + '/';
    
    for (const [key, value] of Object.entries(templateMappings)) {
      if (value.startsWith(prefix)) {
        templates.push({ key, template: value });
      }
    }
    
    return templates;
  }

  /**
   * Analyze requirements and suggest templates
   */
  analyzeRequirements(requirements) {
    const suggestions = [];
    const text = requirements.toLowerCase();
    
    // Check for CRUD operations
    if (text.includes('crud') || 
        (text.includes('create') && text.includes('read') && text.includes('update') && text.includes('delete'))) {
      suggestions.push('backend/crud-feature.md');
      suggestions.push('frontend/angular-component.md');
    }
    
    // Check for API mentions
    if (text.includes('api') || text.includes('endpoint') || text.includes('rest')) {
      suggestions.push('backend/api-endpoint.md');
    }
    
    // Check for UI/Frontend mentions
    if (text.includes('ui') || text.includes('frontend') || text.includes('angular') || text.includes('component')) {
      suggestions.push('frontend/angular-component.md');
      if (text.includes('form')) {
        suggestions.push('frontend/angular-form.md');
      }
      if (text.includes('list') || text.includes('table') || text.includes('grid')) {
        suggestions.push('frontend/angular-list.md');
      }
    }
    
    // Check for testing mentions
    if (text.includes('test')) {
      if (text.includes('unit')) {
        suggestions.push('testing/unit-test.md');
      }
      if (text.includes('integration')) {
        suggestions.push('testing/integration-test.md');
      }
      if (text.includes('e2e') || text.includes('end-to-end')) {
        suggestions.push('testing/e2e-test.md');
      }
    }
    
    // Check for domain/business logic
    if (text.includes('domain') || text.includes('business') || text.includes('entity') || text.includes('aggregate')) {
      suggestions.push('backend/domain-aggregate.md');
    }
    
    return [...new Set(suggestions)];
  }
}

module.exports = new TemplateSelector();