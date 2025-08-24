import { Component, OnInit, OnChanges, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ApiService, PersonResponse, RoleDto, CreatePersonRequest, UpdatePersonRequest } from './api.service';

@Component({
  selector: 'app-people',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  template: `
    <div class="people-form-container">
      <h3>{{ editingPerson ? 'Edit Person' : 'Add New Person' }}</h3>
      
      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="person-form">
        <div class="form-group">
          <label for="fullName">Full Name *</label>
          <input 
            id="fullName"
            type="text"
            placeholder="Enter full name" 
            formControlName="fullName" 
            class="form-control"
            [class.error]="isFieldInvalid('fullName')"
          />
          <div class="error-message" *ngIf="isFieldInvalid('fullName')">
            Full name is required
          </div>
        </div>

        <div class="form-group">
          <label for="phone">Phone Number</label>
          <input 
            id="phone"
            type="tel"
            placeholder="Enter phone number" 
            formControlName="phone" 
            class="form-control"
          />
        </div>

        <div class="form-group">
          <label>Roles</label>
          <div class="roles-grid" *ngIf="roles.length > 0">
            <div *ngFor="let role of roles" class="role-checkbox">
              <input 
                type="checkbox" 
                [id]="'role-' + role.id"
                [value]="role.id" 
                (change)="toggleRole(role.id, $any($event.target).checked)" 
                [checked]="selectedRoleIds.has(role.id)"
              />
              <label [for]="'role-' + role.id" class="checkbox-label">
                <strong>{{ role.name }}</strong>
                <span class="role-description" *ngIf="role.description">{{ role.description }}</span>
              </label>
            </div>
          </div>
          <div *ngIf="roles.length === 0" class="no-roles-message">
            No roles available. Please create roles first.
          </div>
        </div>

        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="!form.valid || isSubmitting">
            {{ isSubmitting ? 'Saving...' : (editingPerson ? 'Update Person' : 'Create Person') }}
          </button>
          <button type="button" class="btn btn-secondary" (click)="onCancel()" [disabled]="isSubmitting">
            Cancel
          </button>
          <button type="button" class="btn btn-outline" (click)="onReset()" [disabled]="isSubmitting">
            Reset
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .people-form-container {
      background: white;
      border-radius: 8px;
      padding: 24px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      max-width: 600px;
    }
    
    h3 {
      margin: 0 0 24px 0;
      color: #333;
      font-weight: 600;
      border-bottom: 2px solid #007bff;
      padding-bottom: 8px;
    }
    
    .person-form {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    
    label {
      font-weight: 500;
      color: #333;
      font-size: 14px;
    }
    
    .form-control {
      padding: 10px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      transition: border-color 0.2s, box-shadow 0.2s;
    }
    
    .form-control:focus {
      outline: none;
      border-color: #007bff;
      box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
    }
    
    .form-control.error {
      border-color: #dc3545;
    }
    
    .error-message {
      color: #dc3545;
      font-size: 12px;
      margin-top: -4px;
    }
    
    .roles-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
      gap: 12px;
      margin-top: 8px;
    }
    
    .role-checkbox {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      padding: 8px;
      border: 1px solid #e9ecef;
      border-radius: 4px;
      transition: background-color 0.2s;
    }
    
    .role-checkbox:hover {
      background-color: #f8f9fa;
    }
    
    .role-checkbox input[type="checkbox"] {
      margin-top: 2px;
    }
    
    .checkbox-label {
      display: flex;
      flex-direction: column;
      gap: 2px;
      cursor: pointer;
      flex: 1;
    }
    
    .role-description {
      font-size: 12px;
      color: #666;
      font-weight: normal;
    }
    
    .no-roles-message {
      padding: 16px;
      background-color: #fff3cd;
      border: 1px solid #ffeaa7;
      border-radius: 4px;
      color: #856404;
      text-align: center;
      font-size: 14px;
    }
    
    .form-actions {
      display: flex;
      gap: 12px;
      margin-top: 24px;
      padding-top: 20px;
      border-top: 1px solid #e9ecef;
    }
    
    .btn {
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
      font-size: 14px;
      transition: all 0.2s;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 120px;
    }
    
    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
    
    .btn-primary {
      background: #007bff;
      color: white;
    }
    
    .btn-primary:hover:not(:disabled) {
      background: #0056b3;
    }
    
    .btn-secondary {
      background: #6c757d;
      color: white;
    }
    
    .btn-secondary:hover:not(:disabled) {
      background: #545b62;
    }
    
    .btn-outline {
      background: white;
      color: #007bff;
      border: 1px solid #007bff;
    }
    
    .btn-outline:hover:not(:disabled) {
      background: #007bff;
      color: white;
    }
  `]
})
export class PeopleComponent implements OnInit, OnChanges {
  @Input() editingPerson: PersonResponse | null = null;
  @Output() personSaved = new EventEmitter<PersonResponse>();
  @Output() cancelled = new EventEmitter<void>();

  roles: RoleDto[] = [];
  form: FormGroup;
  selectedRoleIds = new Set<string>();
  isSubmitting = false;

  constructor(private api: ApiService, private fb: FormBuilder) {
    this.form = this.fb.group({
      fullName: ['', Validators.required],
      phone: ['']
    });
  }

  ngOnInit() {
    this.loadRoles();
    if (this.editingPerson) {
      this.populateFormForEdit();
    }
  }

  ngOnChanges() {
    if (this.editingPerson) {
      this.populateFormForEdit();
    } else {
      this.resetForm();
    }
  }

  private loadRoles() {
    this.api.listRoles().subscribe(roles => {
      this.roles = roles;
    });
  }

  private populateFormForEdit() {
    if (!this.editingPerson) return;
    
    this.form.patchValue({
      fullName: this.editingPerson.fullName,
      phone: this.editingPerson.phone
    });
    
    this.selectedRoleIds = new Set(this.editingPerson.roles.map(r => r.id));
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  toggleRole(roleId: string, checked: boolean) {
    if (!roleId) return;
    if (checked) {
      this.selectedRoleIds.add(roleId);
    } else {
      this.selectedRoleIds.delete(roleId);
    }
  }

  onSubmit() {
    if (this.form.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      const formValue = this.form.value;
      
      const payload: CreatePersonRequest | UpdatePersonRequest = {
        fullName: formValue.fullName,
        phone: formValue.phone || null,
        roleIds: Array.from(this.selectedRoleIds)
      };

      if (this.editingPerson) {
        // Update existing person
        this.api.updatePerson(this.editingPerson.id, payload).subscribe({
          next: () => {
            this.isSubmitting = false;
            this.personSaved.emit(this.editingPerson!);
          },
          error: (error: any) => {
            this.isSubmitting = false;
            console.error('Error updating person:', error);
          }
        });
      } else {
        // Create new person
        this.api.createPerson(payload).subscribe({
          next: (person: PersonResponse) => {
            this.isSubmitting = false;
            this.personSaved.emit(person);
            this.resetForm();
          },
          error: (error: any) => {
            this.isSubmitting = false;
            console.error('Error creating person:', error);
          }
        });
      }
    }
  }

  onCancel() {
    this.cancelled.emit();
  }

  onReset() {
    this.resetForm();
  }

  private resetForm() {
    this.form.reset();
    this.selectedRoleIds.clear();
  }
}
