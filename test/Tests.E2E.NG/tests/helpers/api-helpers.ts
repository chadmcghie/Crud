// API helper functions for E2E tests
import { APIRequestContext } from '@playwright/test';
import { TestRole, TestPerson, TestWall } from './test-data';

export class ApiHelpers {
  constructor(private request: APIRequestContext) {}

  // Helper method for retrying operations
  private async retryOperation<T>(
    operation: () => Promise<T>,
    maxRetries: number = 3,
    delayMs: number = 500
  ): Promise<T> {
    let lastError: Error;
    
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        lastError = error as Error;
        console.warn(`Operation failed (attempt ${attempt}/${maxRetries}):`, error);
        
        if (attempt < maxRetries) {
          await new Promise(resolve => setTimeout(resolve, delayMs));
        }
      }
    }
    
    throw lastError!;
  }

  // Role API helpers
  async createRole(role: TestRole): Promise<any> {
    return this.retryOperation(async () => {
      const response = await this.request.post('http://localhost:5172/api/roles', {
        data: role
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create role: ${response.status()} ${errorText}`);
      }
      return await response.json();
    });
  }

  async getRoles(): Promise<any[]> {
    return this.retryOperation(async () => {
      const response = await this.request.get('http://localhost:5172/api/roles');
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to get roles: ${response.status()} ${errorText}`);
      }
      return await response.json();
    });
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
    return this.retryOperation(async () => {
      const response = await this.request.post('http://localhost:5172/api/people', {
        data: person
      });
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to create person: ${response.status()} ${errorText}`);
      }
      return await response.json();
    });
  }

  async getPeople(): Promise<any[]> {
    return this.retryOperation(async () => {
      const response = await this.request.get('http://localhost:5172/api/people');
      if (!response.ok()) {
        const errorText = await response.text();
        throw new Error(`Failed to get people: ${response.status()} ${errorText}`);
      }
      return await response.json();
    });
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
    const response = await this.request.post('http://localhost:5172/api/walls', {
      data: wall
    });
    if (!response.ok()) {
      throw new Error(`Failed to create wall: ${response.status()} ${await response.text()}`);
    }
    return await response.json();
  }

  async getWalls(): Promise<any[]> {
    const response = await this.request.get('http://localhost:5172/api/walls');
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

  // Improved cleanup helpers with retry logic
  async cleanupRoles(): Promise<void> {
    let retries = 3;
    while (retries > 0) {
      try {
        const roles = await this.getRoles();
        for (const role of roles) {
          try {
            await this.deleteRole(role.id);
          } catch (error) {
            console.warn(`Failed to cleanup role ${role.id}:`, error);
          }
        }
        break;
      } catch (error) {
        retries--;
        if (retries === 0) {
          console.error('Failed to cleanup roles after retries:', error);
          throw error;
        }
        await new Promise(resolve => setTimeout(resolve, 500));

      }
    } catch (error) {
      console.warn('Failed to get roles for cleanup:', error);
    }
  }

  async cleanupPeople(): Promise<void> {
    let retries = 3;
    while (retries > 0) {
      try {
        const people = await this.getPeople();
        for (const person of people) {
          try {
            await this.deletePerson(person.id);
          } catch (error) {
            console.warn(`Failed to cleanup person ${person.id}:`, error);
          }
        }
        break;
      } catch (error) {
        retries--;
        if (retries === 0) {
          console.error('Failed to cleanup people after retries:', error);
          throw error;
        }
        await new Promise(resolve => setTimeout(resolve, 500));

      }
    } catch (error) {
      console.warn('Failed to get people for cleanup:', error);
    }
  }

  async cleanupWalls(): Promise<void> {
    let retries = 3;
    while (retries > 0) {
      try {
        const walls = await this.getWalls();
        for (const wall of walls) {
          try {
            await this.deleteWall(wall.id);
          } catch (error) {
            console.warn(`Failed to cleanup wall ${wall.id}:`, error);
          }
        }
        break;
      } catch (error) {
        retries--;
        if (retries === 0) {
          console.error('Failed to cleanup walls after retries:', error);
          throw error;
        }
        await new Promise(resolve => setTimeout(resolve, 500));
      }
    }
  }

  async cleanupAll(): Promise<void> {
    await this.cleanupPeople();
    await this.cleanupRoles();
    await this.cleanupWalls();
  }
}