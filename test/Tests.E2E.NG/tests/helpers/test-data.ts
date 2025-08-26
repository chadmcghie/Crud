// Test data generators and utilities

export interface TestRole {
  name: string;
  description?: string;
}

export interface TestPerson {
  fullName: string;
  phone?: string;
  roleIds?: string[];
}

export interface TestWall {
  name: string;
  description?: string;
  length: number;
  height: number;
  thickness: number;
  assemblyType: string;
  assemblyDetails?: string;
  rValue?: number;
  uValue?: number;
  materialLayers?: string;
  orientation?: string;
  location?: string;
}

export const testRoles: TestRole[] = [
  {
    name: 'Test Administrator',
    description: 'Full system access and management capabilities'
  },
  {
    name: 'Test Manager',
    description: 'Team management and oversight responsibilities'
  },
  {
    name: 'Test Developer',
    description: 'Software development and technical implementation'
  },
  {
    name: 'Test Analyst',
    description: 'Data analysis and reporting functions'
  }
];

export const testPeople: TestPerson[] = [
  {
    fullName: 'Test John Smith',
    phone: '+1-555-0101'
  },
  {
    fullName: 'Test Jane Doe',
    phone: '+1-555-0102'
  },
  {
    fullName: 'Test Bob Johnson',
    phone: '+1-555-0103'
  },
  {
    fullName: 'Test Alice Brown'
  }
];

export const testWalls: TestWall[] = [
  {
    name: 'North Wall',
    description: 'Main exterior wall facing north',
    length: 12.5,
    height: 3.0,
    thickness: 0.25,
    assemblyType: 'Exterior',
    assemblyDetails: 'Brick veneer with insulation',
    rValue: 15.2,
    uValue: 0.066,
    materialLayers: 'Brick, Air Gap, Insulation, Drywall',
    orientation: 'North',
    location: 'Building A'
  },
  {
    name: 'Interior Partition',
    description: 'Dividing wall between offices',
    length: 8.0,
    height: 2.7,
    thickness: 0.15,
    assemblyType: 'Interior',
    assemblyDetails: 'Standard drywall partition',
    rValue: 2.5,
    uValue: 0.4,
    materialLayers: 'Drywall, Studs, Drywall',
    orientation: 'East-West',
    location: 'Office Floor 2'
  }
];

// Utility functions
export function generateRandomString(length: number = 8): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}

// Get worker-specific prefix for test isolation
export function getWorkerPrefix(workerIndex?: number): string {
  // Use provided workerIndex or fallback to environment variable or default
  const workerId = workerIndex !== undefined ? workerIndex.toString() : (process.env.TEST_WORKER_INDEX || '0');
  const timestamp = Date.now().toString().slice(-6); // Last 6 digits of timestamp
  const randomSuffix = generateRandomString(4); // Add extra randomness
  return `W${workerId}_T${timestamp}_${randomSuffix}`;
}

export function generateTestRole(overrides: Partial<TestRole> = {}, workerIndex?: number): TestRole {
  // If name is explicitly provided in overrides, use it exactly as-is
  if (overrides.name) {
    return {
      name: overrides.name,
      description: overrides.description || `Test role for ${overrides.name}`,
      // Apply any other overrides
      ...Object.fromEntries(Object.entries(overrides).filter(([key]) => key !== 'name' && key !== 'description'))
    };
  }
  
  // Otherwise generate a unique name with prefix
  const prefix = getWorkerPrefix(workerIndex);
  const workerId = workerIndex !== undefined ? workerIndex : 0;
  
  return {
    name: `${prefix}_Role_${generateRandomString(4)}`,
    description: overrides.description || `Worker${workerId}: Auto-generated test role - ${generateRandomString(6)}`,
    // Apply any other overrides
    ...Object.fromEntries(Object.entries(overrides).filter(([key]) => key !== 'name' && key !== 'description'))
  };
}

export function generateTestPerson(overrides: Partial<TestPerson> = {}, workerIndex?: number): TestPerson {
  // If fullName is explicitly provided in overrides, use it exactly as-is
  if (overrides.fullName) {
    return {
      fullName: overrides.fullName,
      phone: overrides.phone || `+1-555-${Math.floor(Math.random() * 9000) + 1000}`,
      // Apply any other overrides
      ...Object.fromEntries(Object.entries(overrides).filter(([key]) => key !== 'fullName' && key !== 'phone'))
    };
  }
  
  // Otherwise generate a unique name with prefix
  const prefix = getWorkerPrefix(workerIndex);
  
  return {
    fullName: `${prefix}_Person_${generateRandomString(4)}`,
    phone: overrides.phone || `+1-555-${Math.floor(Math.random() * 9000) + 1000}`,
    // Apply any other overrides
    ...Object.fromEntries(Object.entries(overrides).filter(([key]) => key !== 'fullName' && key !== 'phone'))
  };
}

export function generateTestWall(overrides: Partial<TestWall> = {}): TestWall {
  return {
    name: `Test Wall ${generateRandomString(4)}`,
    description: `Auto-generated test wall - ${generateRandomString(6)}`,
    length: Math.round((Math.random() * 20 + 5) * 10) / 10, // 5-25 meters
    height: Math.round((Math.random() * 2 + 2.5) * 10) / 10, // 2.5-4.5 meters
    thickness: Math.round((Math.random() * 0.3 + 0.1) * 100) / 100, // 0.1-0.4 meters
    assemblyType: Math.random() > 0.5 ? 'Exterior' : 'Interior',
    assemblyDetails: `Test assembly details ${generateRandomString(4)}`,
    rValue: Math.round((Math.random() * 20 + 5) * 10) / 10,
    uValue: Math.round((Math.random() * 0.5 + 0.05) * 1000) / 1000,
    materialLayers: `Layer1, Layer2, Layer3 ${generateRandomString(3)}`,
    orientation: ['North', 'South', 'East', 'West'][Math.floor(Math.random() * 4)],
    location: `Test Location ${generateRandomString(3)}`,
    ...overrides
  };
}