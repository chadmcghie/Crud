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
    name: 'Administrator',
    description: 'Full system access and management capabilities'
  },
  {
    name: 'Manager',
    description: 'Team management and oversight responsibilities'
  },
  {
    name: 'Developer',
    description: 'Software development and technical implementation'
  },
  {
    name: 'Analyst',
    description: 'Data analysis and reporting functions'
  }
];

export const testPeople: TestPerson[] = [
  {
    fullName: 'John Smith',
    phone: '+1-555-0101'
  },
  {
    fullName: 'Jane Doe',
    phone: '+1-555-0102'
  },
  {
    fullName: 'Bob Johnson',
    phone: '+1-555-0103'
  },
  {
    fullName: 'Alice Brown'
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

export function generateTestRole(overrides: Partial<TestRole> = {}): TestRole {
  return {
    name: `Test Role ${generateRandomString(4)}`,
    description: `Auto-generated test role - ${generateRandomString(6)}`,
    ...overrides
  };
}

export function generateTestPerson(overrides: Partial<TestPerson> = {}): TestPerson {
  return {
    fullName: `Test Person ${generateRandomString(4)}`,
    phone: `+1-555-${Math.floor(Math.random() * 9000) + 1000}`,
    ...overrides
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