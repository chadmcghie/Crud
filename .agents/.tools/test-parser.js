const line = '- [x] 1. Create Command Dashboard and Shell Integration (Issue: #153)';
const pattern = /^- \[([ x])\] (\d+\. .+?)(?:\s*\(Issue: #\d+\))?$/;

console.log('Testing line:', line);
console.log('Pattern:', pattern);

const match = line.match(pattern);
console.log('Match result:', match);

if (match) {
  console.log('Checkbox:', match[1]);
  console.log('Task text:', match[2]);
}

// Test without lazy quantifier
const pattern2 = /^- \[([ x])\] (\d+\. .+)$/;
const match2 = line.match(pattern2);
console.log('\nWithout optional Issue part:', match2);