// Script to run only the 5 failing tests
const { execSync } = require('child_process');

const failingTests = [
  'tests/api/roles-api-parallel.spec.ts:9',
  'tests/integration/full-workflow.spec.ts:114',
  'tests/integration/full-workflow.spec.ts:291'
];

const testPattern = failingTests.join('|');
const command = `npx playwright test --grep "${testPattern}"`;

console.log('Running failing tests:', command);
execSync(command, { stdio: 'inherit' });
