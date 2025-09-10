const assert = require('assert');
const fs = require('fs').promises;
const path = require('path');
const { LearningCache } = require('../learning-cache');
const { PatternMatcher } = require('../pattern-matcher');

describe('LearningCache', () => {
  let cache;
  const testLearningDir = path.join(__dirname, 'test-learning');
  
  beforeEach(async () => {
    cache = new LearningCache(testLearningDir);
    await fs.mkdir(testLearningDir, { recursive: true });
  });
  
  afterEach(async () => {
    try {
      await fs.rm(testLearningDir, { recursive: true, force: true });
    } catch (err) {
      // Ignore cleanup errors
    }
  });
  
  describe('Pattern Storage', () => {
    it('should store and retrieve patterns', async () => {
      const pattern = {
        id: 'sqlite-lock-error',
        category: 'testing',
        problem: 'SQLite database locked error during tests',
        solution: 'Use unique database names per test run or implement serial execution',
        tags: ['sqlite', 'testing', 'database', 'lock'],
        examples: [
          'Error: SQLITE_BUSY: database is locked',
          'database table is locked'
        ]
      };
      
      await cache.addPattern(pattern);
      const retrieved = await cache.getPattern('sqlite-lock-error');
      
      assert.deepStrictEqual(retrieved, pattern);
    });
    
    it('should search patterns by category', async () => {
      await cache.addPattern({
        id: 'test-pattern-1',
        category: 'testing',
        problem: 'Test isolation issue',
        solution: 'Use beforeEach/afterEach hooks'
      });
      
      await cache.addPattern({
        id: 'build-pattern-1',
        category: 'build',
        problem: 'Slow build times',
        solution: 'Enable incremental compilation'
      });
      
      const testingPatterns = await cache.searchByCategory('testing');
      assert.strictEqual(testingPatterns.length, 1);
      assert.strictEqual(testingPatterns[0].id, 'test-pattern-1');
    });
    
    it('should search patterns by tags', async () => {
      await cache.addPattern({
        id: 'pattern-1',
        category: 'testing',
        problem: 'Issue 1',
        solution: 'Solution 1',
        tags: ['angular', 'testing']
      });
      
      await cache.addPattern({
        id: 'pattern-2',
        category: 'build',
        problem: 'Issue 2',
        solution: 'Solution 2',
        tags: ['angular', 'build']
      });
      
      const angularPatterns = await cache.searchByTag('angular');
      assert.strictEqual(angularPatterns.length, 2);
      
      const testingPatterns = await cache.searchByTag('testing');
      assert.strictEqual(testingPatterns.length, 1);
    });
  });
  
  describe('Memory References', () => {
    it('should store memory references with metadata', async () => {
      const memoryRef = {
        id: 'auth-implementation',
        description: 'JWT authentication implementation discussion',
        memoryId: 'mem_xyz123',
        context: 'Implementation of JWT auth in .NET/Angular stack',
        tags: ['authentication', 'jwt', 'security'],
        dateAdded: new Date().toISOString()
      };
      
      await cache.addMemoryReference(memoryRef);
      const retrieved = await cache.getMemoryReference('auth-implementation');
      
      assert.deepStrictEqual(retrieved, memoryRef);
    });
    
    it('should list all memory references', async () => {
      await cache.addMemoryReference({
        id: 'ref-1',
        description: 'Reference 1',
        memoryId: 'mem_1'
      });
      
      await cache.addMemoryReference({
        id: 'ref-2',
        description: 'Reference 2',
        memoryId: 'mem_2'
      });
      
      const references = await cache.getAllMemoryReferences();
      assert.strictEqual(references.length, 2);
    });
  });
  
  describe('Pattern File Generation', () => {
    it('should generate patterns.md file', async () => {
      await cache.addPattern({
        id: 'sqlite-lock',
        category: 'testing',
        problem: 'SQLite database locked',
        solution: 'Use serial execution',
        tags: ['sqlite', 'testing'],
        examples: ['SQLITE_BUSY error']
      });
      
      await cache.addPattern({
        id: 'angular-build',
        category: 'build',
        problem: 'Slow Angular builds',
        solution: 'Skip Angular build with environment variable',
        tags: ['angular', 'build'],
        examples: ['Build taking > 5 minutes']
      });
      
      const content = await cache.generatePatternsFile();
      
      assert(content.includes('# Agent OS Learning Patterns'));
      assert(content.includes('## Testing Patterns'));
      assert(content.includes('## Build Patterns'));
      assert(content.includes('SQLite database locked'));
      assert(content.includes('Slow Angular builds'));
    });
    
    it('should generate memory-references.md file', async () => {
      await cache.addMemoryReference({
        id: 'auth-ref',
        description: 'Authentication implementation',
        memoryId: 'mem_123',
        context: 'JWT auth setup',
        tags: ['auth']
      });
      
      const content = await cache.generateMemoryReferencesFile();
      
      assert(content.includes('# Agent OS Memory References'));
      assert(content.includes('Authentication implementation'));
      assert(content.includes('mem_123'));
    });
  });
  
  describe('Auto-learning from Issues', () => {
    it('should extract patterns from resolved issues', async () => {
      const issue = {
        title: 'Database locked during E2E tests',
        body: 'Getting SQLITE_BUSY errors when running tests in parallel',
        resolution: 'Changed to serial execution with workers: 1',
        labels: ['bug', 'testing']
      };
      
      const pattern = await cache.learnFromIssue(issue);
      
      assert(pattern.problem.includes('Database locked'));
      assert(pattern.solution.includes('serial execution'));
      assert(pattern.category === 'testing');
    });
  });
});

describe('PatternMatcher', () => {
  let matcher;
  
  beforeEach(() => {
    matcher = new PatternMatcher();
  });
  
  describe('Pattern Matching', () => {
    it('should match error messages to patterns', async () => {
      const patterns = [
        {
          id: 'sqlite-lock',
          problem: 'SQLite database locked',
          solution: 'Use serial execution',
          examples: ['SQLITE_BUSY', 'database is locked']
        },
        {
          id: 'null-ref',
          problem: 'Null reference exception',
          solution: 'Add null checks',
          examples: ['NullReferenceException', 'Cannot read property of null']
        }
      ];
      
      matcher.loadPatterns(patterns);
      
      const error = 'Error: SQLITE_BUSY: database is locked';
      const match = matcher.findMatch(error);
      
      assert.strictEqual(match.id, 'sqlite-lock');
      assert(match.confidence > 0.8);
    });
    
    it('should rank multiple matches by confidence', async () => {
      const patterns = [
        {
          id: 'generic-db',
          problem: 'Database error',
          examples: ['database']
        },
        {
          id: 'specific-sqlite',
          problem: 'SQLite specific error',
          examples: ['SQLITE_BUSY', 'SQLite', 'database']
        }
      ];
      
      matcher.loadPatterns(patterns);
      
      const error = 'SQLITE_BUSY: database is locked';
      const matches = matcher.findAllMatches(error);
      
      assert(matches[0].id === 'specific-sqlite');
      assert(matches[0].confidence > matches[1].confidence);
    });
  });
  
  describe('Solution Suggestions', () => {
    it('should suggest solutions for known patterns', async () => {
      const patterns = [
        {
          id: 'test-1',
          problem: 'Problem 1',
          solution: 'Solution 1',
          examples: ['error pattern 1']
        }
      ];
      
      matcher.loadPatterns(patterns);
      const suggestions = matcher.suggestSolutions('error pattern 1');
      
      assert.strictEqual(suggestions.length, 1);
      assert.strictEqual(suggestions[0].solution, 'Solution 1');
    });
  });
});