class PatternMatcher {
  constructor() {
    this.patterns = [];
  }
  
  /**
   * Load patterns for matching
   */
  loadPatterns(patterns) {
    this.patterns = patterns;
  }
  
  /**
   * Find the best matching pattern for an error message
   */
  findMatch(errorMessage) {
    const matches = this.findAllMatches(errorMessage);
    return matches.length > 0 ? matches[0] : null;
  }
  
  /**
   * Find all matching patterns ranked by confidence
   */
  findAllMatches(errorMessage) {
    const matches = [];
    const lowerError = errorMessage.toLowerCase();
    
    for (const pattern of this.patterns) {
      let confidence = 0;
      let matchCount = 0;
      
      // Check examples for matches
      if (pattern.examples) {
        for (const example of pattern.examples) {
          if (lowerError.includes(example.toLowerCase())) {
            matchCount++;
          }
        }
        
        if (matchCount > 0) {
          // Higher confidence for more matching examples
          // Boost confidence based on both match count and ratio
          confidence = (matchCount / pattern.examples.length) * 0.8 + (matchCount * 0.1);
        }
      }
      
      // Check problem description
      if (pattern.problem && lowerError.includes(pattern.problem.toLowerCase())) {
        confidence = Math.max(confidence, 0.5);
      }
      
      // Check symptoms
      if (pattern.symptoms) {
        for (const symptom of pattern.symptoms) {
          if (lowerError.includes(symptom.toLowerCase())) {
            confidence = Math.max(confidence, 0.7);
          }
        }
      }
      
      // Boost confidence for exact matches
      if (pattern.examples) {
        for (const example of pattern.examples) {
          if (errorMessage === example) {
            confidence = 1.0;
            break;
          }
        }
      }
      
      if (confidence > 0) {
        matches.push({
          ...pattern,
          confidence: confidence
        });
      }
    }
    
    // Sort by confidence (highest first)
    matches.sort((a, b) => b.confidence - a.confidence);
    
    return matches;
  }
  
  /**
   * Suggest solutions for an error message
   */
  suggestSolutions(errorMessage) {
    const matches = this.findAllMatches(errorMessage);
    
    return matches
      .filter(m => m.solution)
      .map(m => ({
        pattern: m.id,
        solution: m.solution,
        confidence: m.confidence,
        category: m.category
      }));
  }
  
  /**
   * Check if error matches known patterns
   */
  hasKnownSolution(errorMessage) {
    const match = this.findMatch(errorMessage);
    return match && match.solution && match.confidence > 0.5;
  }
  
  /**
   * Get patterns by category
   */
  getPatternsByCategory(category) {
    return this.patterns.filter(p => p.category === category);
  }
  
  /**
   * Get patterns by tag
   */
  getPatternsByTag(tag) {
    return this.patterns.filter(p => 
      p.tags && p.tags.includes(tag)
    );
  }
  
  /**
   * Search patterns by keyword
   */
  searchPatterns(keyword) {
    const lowerKeyword = keyword.toLowerCase();
    
    return this.patterns.filter(p => {
      // Check ID
      if (p.id && p.id.includes(lowerKeyword)) {
        return true;
      }
      
      // Check problem
      if (p.problem && p.problem.toLowerCase().includes(lowerKeyword)) {
        return true;
      }
      
      // Check solution
      if (p.solution && p.solution.toLowerCase().includes(lowerKeyword)) {
        return true;
      }
      
      // Check tags
      if (p.tags) {
        for (const tag of p.tags) {
          if (tag.toLowerCase().includes(lowerKeyword)) {
            return true;
          }
        }
      }
      
      return false;
    });
  }
}

module.exports = { PatternMatcher };