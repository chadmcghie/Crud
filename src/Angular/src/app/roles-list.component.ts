import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, RoleDto } from './api.service';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="roles-list-container">
      <h3>Roles Management</h3>
      <div class="actions">
        <button class="btn btn-primary" (click)="onAddRole()">Add New Role</button>
        <button class="btn btn-secondary" (click)="refresh()">Refresh</button>
      </div>
      
      <div class="table-container" *ngIf="roles.length > 0">
        <table class="roles-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Description</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let role of roles" class="role-row">
              <td class="name-cell">{{ role.name }}</td>
              <td class="description-cell">{{ role.description || 'N/A' }}</td>
              <td class="actions-cell">
                <button class="btn btn-edit" (click)="onEditRole(role)">Edit</button>
                <button class="btn btn-delete" (click)="onDeleteRole(role)">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <div class="empty-state" *ngIf="roles.length === 0">
        <p>No roles found. <a href="#" (click)="onAddRole()">Add the first role</a></p>
      </div>
    </div>
  `,
  styles: [`
    .roles-list-container {
      background: white;
      border-radius: 8px;
      padding: 20px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    
    h3 {
      margin: 0 0 20px 0;
      color: #333;
      font-weight: 600;
    }
    
    .actions {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
    }
    
    .btn {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
      transition: background-color 0.2s;
    }
    
    .btn-primary {
      background: #007bff;
      color: white;
    }
    
    .btn-primary:hover {
      background: #0056b3;
    }
    
    .btn-secondary {
      background: #6c757d;
      color: white;
    }
    
    .btn-secondary:hover {
      background: #545b62;
    }
    
    .btn-edit {
      background: #28a745;
      color: white;
      padding: 4px 8px;
      font-size: 12px;
      margin-right: 5px;
    }
    
    .btn-edit:hover {
      background: #1e7e34;
    }
    
    .btn-delete {
      background: #dc3545;
      color: white;
      padding: 4px 8px;
      font-size: 12px;
    }
    
    .btn-delete:hover {
      background: #bd2130;
    }
    
    .table-container {
      overflow-x: auto;
    }
    
    .roles-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 10px;
    }
    
    .roles-table th,
    .roles-table td {
      text-align: left;
      padding: 12px;
      border-bottom: 1px solid #dee2e6;
    }
    
    .roles-table th {
      background-color: #f8f9fa;
      font-weight: 600;
      color: #495057;
    }
    
    .role-row:hover {
      background-color: #f8f9fa;
    }
    
    .name-cell {
      font-weight: 500;
      color: #333;
    }
    
    .description-cell {
      color: #666;
      max-width: 300px;
      word-wrap: break-word;
    }
    
    .actions-cell {
      white-space: nowrap;
    }
    
    .empty-state {
      text-align: center;
      padding: 40px;
      color: #666;
    }
    
    .empty-state a {
      color: #007bff;
      text-decoration: none;
    }
    
    .empty-state a:hover {
      text-decoration: underline;
    }
  `]
})
export class RolesListComponent implements OnInit {
  roles: RoleDto[] = [];
  @Output() editRole = new EventEmitter<RoleDto>();
  @Output() addRole = new EventEmitter<void>();

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadRoles();
  }

  loadRoles() {
    this.api.listRoles().subscribe(roles => {
      this.roles = roles;
    });
  }

  refresh() {
    this.loadRoles();
  }

  onAddRole() {
    console.log('Roles List: Add role button clicked');
    this.addRole.emit();
  }

  onEditRole(role: RoleDto) {
    this.editRole.emit(role);
  }

  onDeleteRole(role: RoleDto) {
    if (confirm(`Are you sure you want to delete the role "${role.name}"?`)) {
      this.api.deleteRole(role.id).subscribe(() => {
        this.loadRoles(); // Refresh the list
      });
    }
  }
}