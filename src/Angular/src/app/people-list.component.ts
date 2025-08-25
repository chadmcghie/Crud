import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, PersonResponse } from './api.service';

@Component({
  selector: 'app-people-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="people-list-container">
      <h3>People Directory</h3>
      <div class="actions">
        <button class="btn btn-primary" (click)="onAddPerson()">Add New Person</button>
        <button class="btn btn-secondary" (click)="refresh()">Refresh</button>
      </div>
      
      <div class="table-container" *ngIf="people.length > 0">
        <table class="people-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Phone</th>
              <th>Roles</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let person of people" class="person-row">
              <td class="name-cell">{{ person.fullName }}</td>
              <td class="phone-cell">{{ person.phone || 'N/A' }}</td>
              <td class="roles-cell">
                <span *ngFor="let role of person.roles; let last = last" class="role-badge">
                  {{ role.name }}<span *ngIf="!last">, </span>
                </span>
                <span *ngIf="person.roles.length === 0" class="no-roles">No roles assigned</span>
              </td>
              <td class="actions-cell">
                <button class="btn btn-edit" (click)="onEditPerson(person)">Edit</button>
                <button class="btn btn-delete" (click)="onDeletePerson(person)">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      
      <div class="empty-state" *ngIf="people.length === 0">
        <p>No people found. <a href="#" (click)="onAddPerson()">Add the first person</a></p>
      </div>
    </div>
  `,
  styles: [`
    .people-list-container {
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
    
    .people-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 10px;
    }
    
    .people-table th,
    .people-table td {
      text-align: left;
      padding: 12px;
      border-bottom: 1px solid #dee2e6;
    }
    
    .people-table th {
      background-color: #f8f9fa;
      font-weight: 600;
      color: #495057;
    }
    
    .person-row:hover {
      background-color: #f8f9fa;
    }
    
    .name-cell {
      font-weight: 500;
      color: #333;
    }
    
    .phone-cell {
      color: #666;
    }
    
    .roles-cell {
      max-width: 200px;
    }
    
    .role-badge {
      background: #e9ecef;
      padding: 2px 6px;
      border-radius: 3px;
      font-size: 11px;
      color: #495057;
    }
    
    .no-roles {
      color: #999;
      font-style: italic;
      font-size: 12px;
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
export class PeopleListComponent implements OnInit {
  people: PersonResponse[] = [];
  isLoading = false;
  error: string | null = null;
  @Output() editPerson = new EventEmitter<PersonResponse>();
  @Output() addPerson = new EventEmitter<void>();

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.loadPeople();
  }

  loadPeople() {
    this.isLoading = true;
    this.error = null;
    
    this.api.listPeople().subscribe({
      next: (people) => {
        this.people = people;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading people:', error);
        this.error = 'Failed to load people. Please try again.';
        this.isLoading = false;
        this.people = []; // Clear the list on error
      }
    });
  }

  refresh() {
    this.loadPeople();
  }

  onAddPerson() {
    this.addPerson.emit();
  }

  onEditPerson(person: PersonResponse) {
    this.editPerson.emit(person);
  }

  onDeletePerson(person: PersonResponse) {
    if (confirm(`Are you sure you want to delete ${person.fullName}?`)) {
      this.api.deletePerson(person.id).subscribe({
        next: () => {
          this.loadPeople(); // Refresh the list
        },
        error: (error) => {
          console.error('Error deleting person:', error);
          this.error = `Failed to delete ${person.fullName}. Please try again.`;
          // Don't refresh the list on error, keep current state
        }
      });
    }
  }
}