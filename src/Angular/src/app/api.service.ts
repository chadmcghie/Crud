import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RoleDto {
  id: string;
  name: string;
  description?: string | null;
}

export interface CreateRoleRequest { name: string; description?: string | null; }
export interface UpdateRoleRequest { name: string; description?: string | null; }

export interface PersonResponse {
  id: string;
  fullName: string;
  phone?: string | null;
  roles: RoleDto[];
}

export interface CreatePersonRequest {
  fullName: string;
  phone?: string | null;
  roleIds?: string[];
}

export interface UpdatePersonRequest {
  fullName: string;
  phone?: string | null;
  roleIds?: string[];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  // Use relative base URL so dev server proxy can forward to API
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  // Roles
  listRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(`${this.baseUrl}/roles`);
  }
  createRole(req: CreateRoleRequest): Observable<RoleDto> {
    return this.http.post<RoleDto>(`${this.baseUrl}/roles`, req);
  }
  updateRole(id: string, req: UpdateRoleRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/roles/${id}`, req);
  }
  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/roles/${id}`);
  }

  // People
  listPeople(): Observable<PersonResponse[]> {
    return this.http.get<PersonResponse[]>(`${this.baseUrl}/people`);
  }
  createPerson(req: CreatePersonRequest): Observable<PersonResponse> {
    return this.http.post<PersonResponse>(`${this.baseUrl}/people`, req);
  }
  updatePerson(id: string, req: UpdatePersonRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/people/${id}`, req);
  }
  deletePerson(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/people/${id}`);
  }
}
