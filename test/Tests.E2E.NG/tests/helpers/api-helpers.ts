// API helper functions for E2E tests
import { APIRequestContext } from '@playwright/test';
import { TestRole, TestPerson, TestWall } from './test-data';

export class ApiHelpers {
  constructor(private request: APIRequestContext) {}

  // Role API helpers
  async createRole(role: TestRole): Promise<any> {
    const response = await this.request.post('/api/roles', {
      data: role
    });
    if (!response.ok()) {
      throw new Error(`Failed to create role: ${response.status()} ${await response.text()}`);
    }
    return await response.json();
  }

  async getRoles(): Promise<any[]> {
    const response = await this.request.get('/api/roles');
    if (!response.ok()) {
      throw new Error(`Failed to get roles: ${response.status()}`);
    }
    return await response.json();
  }

  async getRole(id: string): Promise<any> {
    const response = await this.request.get(`/api/roles/${id}`);
    if (!response.ok()) {
      throw new Error(`Failed to get role: ${response.status()}`);
    }
    return await response.json();
  }

  async updateRole(id: string, role: TestRole): Promise<void> {
    const response = await this.request.put(`/api/roles/${id}`, {
      data: role
    });
    if (!response.ok()) {
      throw new Error(`Failed to update role: ${response.status()}`);
    }
  }

  async deleteRole(id: string): Promise<void> {
    const response = await this.request.delete(`/api/roles/${id}`);
    if (!response.ok()) {
      throw new Error(`Failed to delete role: ${response.status()}`);
    }
  }

  // Person API helpers
  async createPerson(person: TestPerson): Promise<any> {
    const response = await this.request.post('/api/people', {
      data: person
    });
    if (!response.ok()) {
      throw new Error(`Failed to create person: ${response.status()} ${await response.text()}`);
    }
    return await response.json();
  }

  async getPeople(): Promise<any[]> {
    const response = await this.request.get('/api/people');
    if (!response.ok()) {
      throw new Error(`Failed to get people: ${response.status()}`);
    }
    return await response.json();
  }

  async getPerson(id: string): Promise<any> {
    const response = await this.request.get(`/api/people/${id}`);
    if (!response.ok()) {
      throw new Error(`Failed to get person: ${response.status()}`);
    }
    return await response.json();
  }

  async updatePerson(id: string, person: TestPerson): Promise<void> {
    const response = await this.request.put(`/api/people/${id}`, {
      data: person
    });
    if (!response.ok()) {
      throw new Error(`Failed to update person: ${response.status()}`);
    }
  }

  async deletePerson(id: string): Promise<void> {
    const response = await this.request.delete(`/api/people/${id}`);
    if (!response.ok()) {
      throw new Error(`Failed to delete person: ${response.status()}`);
    }
  }

  // Wall API helpers
  async createWall(wall: TestWall): Promise<any> {
    const response = await this.request.post('/api/walls', {
      data: wall
    });
    if (!response.ok()) {
      throw new Error(`Failed to create wall: ${response.status()} ${await response.text()}`);
    }
    return await response.json();
  }

  async getWalls(): Promise<any[]> {
    const response = await this.request.get('/api/walls');
    if (!response.ok()) {
      throw new Error(`Failed to get walls: ${response.status()}`);
    }
    return await response.json();
  }

  async getWall(id: string): Promise<any> {
    const response = await this.request.get(`/api/walls/${id}`);
    if (!response.ok()) {
      throw new Error(`Failed to get wall: ${response.status()}`);
    }
    return await response.json();
  }

  async updateWall(id: string, wall: TestWall): Promise<void> {
    const response = await this.request.put(`/api/walls/${id}`, {
      data: wall
    });
    if (!response.ok()) {
      throw new Error(`Failed to update wall: ${response.status()}`);
    }
  }

  async deleteWall(id: string): Promise<void> {
    const response = await this.request.delete(`/api/walls/${id}`);
    if (!response.ok()) {
      throw new Error(`Failed to delete wall: ${response.status()}`);
    }
  }

  // Cleanup helpers
  async cleanupRoles(): Promise<void> {
    try {
      const roles = await this.getRoles();
      for (const role of roles) {
        try {
          await this.deleteRole(role.id);
          // Small delay between deletions to avoid race conditions
          await new Promise(resolve => setTimeout(resolve, 50));
        } catch (error) {
          console.warn(`Failed to cleanup role ${role.id}:`, error);
        }
      }
    } catch (error) {
      console.warn('Failed to get roles for cleanup:', error);
    }
  }

  async cleanupPeople(): Promise<void> {
    try {
      const people = await this.getPeople();
      for (const person of people) {
        try {
          await this.deletePerson(person.id);
          // Small delay between deletions to avoid race conditions
          await new Promise(resolve => setTimeout(resolve, 50));
        } catch (error) {
          console.warn(`Failed to cleanup person ${person.id}:`, error);
        }
      }
    } catch (error) {
      console.warn('Failed to get people for cleanup:', error);
    }
  }

  async cleanupWalls(): Promise<void> {
    const walls = await this.getWalls();
    for (const wall of walls) {
      try {
        await this.deleteWall(wall.id);
      } catch (error) {
        console.warn(`Failed to cleanup wall ${wall.id}:`, error);
      }
    }
  }

  async cleanupAll(): Promise<void> {
    // Clean up in order to avoid dependency issues
    // People first (they might reference roles)
    await this.cleanupPeople();
    await this.cleanupRoles();
    await this.cleanupWalls();
    
    // Add a small delay to ensure cleanup is complete
    await new Promise(resolve => setTimeout(resolve, 200));
  }
}