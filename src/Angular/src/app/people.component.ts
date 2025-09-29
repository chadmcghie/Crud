import { Component, OnInit, OnChanges, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService, PersonResponse, RoleDto, CreatePersonRequest, UpdatePersonRequest } from './api.service';
import { CustomValidators } from './validators/custom-validators';

@Component({
  selector: 'app-people',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule, RouterModule],
  template: `
    <div class="people-form-container">
      <h3>{{ editingPerson ? 'Edit Person' : 'Add New Person' }}</h3>

      <!-- Success Message -->
      <div class="alert alert-success" *ngIf="successMessage">
        <svg class="alert-icon" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
        </svg>
        {{ successMessage }}
      </div>

      <!-- Error Message -->
      <div class="alert alert-error" *ngIf="error">
        <svg class="alert-icon" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
        </svg>
        {{ error }}
      </div>

      <!-- Roles Error Message -->
      <div class="alert alert-warning" *ngIf="rolesError">
        <svg class="alert-icon" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
        </svg>
        {{ rolesError }}
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit($event)" class="person-form">
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
            <span *ngIf="form.get('fullName')?.errors?.['required']">Full name is required</span>
            <span *ngIf="form.get('fullName')?.errors?.['invalidFullName']">{{ form.get('fullName')?.errors?.['invalidFullName'] }}</span>
            <span *ngIf="form.get('fullName')?.errors?.['maxLength']">{{ form.get('fullName')?.errors?.['maxLength'] }}</span>
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
            [class.error]="isFieldInvalid('phone')"
          />
          <div class="error-message" *ngIf="isFieldInvalid('phone')">
            <span *ngIf="form.get('phone')?.errors?.['invalidPhone']">{{ form.get('phone')?.errors?.['invalidPhone'] }}</span>
          </div>
        </div>

        <div class="form-group">
          <label for="roles">Roles</label>
          <div class="roles-grid" id="roles" *ngIf="roles.length > 0">
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
            <div class="no-roles-content">
              <p><strong>No roles available.</strong></p>
              <p>You need to create roles before you can assign them to people.</p>
              <a routerLink="/roles-list" class="create-roles-link">
                <svg class="link-icon" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z" clip-rule="evenodd"/>
                </svg>
                Create Roles Now
              </a>
            </div>
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

    .no-roles-content p {
      margin: 0 0 8px 0;
      color: #856404;
    }

    .create-roles-link {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
      padding: 8px 16px;
      background-color: rgba(0, 123, 255, 0.1);
      border-radius: 4px;
      transition: all 0.2s ease;
      margin-top: 8px;
    }

    .create-roles-link:hover {
      background-color: rgba(0, 123, 255, 0.2);
      text-decoration: none;
    }

    .link-icon {
      width: 16px;
      height: 16px;
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

    .alert {
      padding: 12px 16px;
      border-radius: 6px;
      margin-bottom: 20px;
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 14px;
      font-weight: 500;
    }

    .alert-icon {
      width: 20px;
      height: 20px;
      flex-shrink: 0;
    }

    .alert-success {
      background-color: #d1edff;
      border: 1px solid #0084ff;
      color: #0066cc;
    }

    .alert-error {
      background-color: #ffe6e6;
      border: 1px solid #ff4757;
      color: #c44569;
    }

    .alert-warning {
      background-color: #fff3cd;
      border: 1px solid #ffc107;
      color: #856404;
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
  error: string | null = null;
  rolesError: string | null = null;
  successMessage: string | null = null;
  
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  constructor() {
    this.form = this.fb.group({
      fullName: ['', [Validators.required, CustomValidators.fullName()]],
      phone: ['', [CustomValidators.phoneNumber()]]
    });
  }

  ngOnInit() {
    this.loadRoles();
    
    // Check for edit query parameter
    this.route.queryParams.subscribe(params => {
      if (params['edit']) {
        const personId = params['edit']; // ID is already a string (GUID)
        this.loadPersonForEdit(personId);
      } else {
        this.editingPerson = null;
        this.resetForm();
      }
    });
  }

  ngOnChanges() {
    if (this.editingPerson) {
      this.populateFormForEdit();
    } else {
      this.resetForm();
    }
  }

  private loadRoles() {
    this.rolesError = null;
    
    this.api.listRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
      },
      error: (error) => {
        console.error('Error loading roles:', error);
        this.rolesError = 'Failed to load roles. Role assignment may not work properly.';
        this.roles = []; // Clear roles on error
      }
    });
  }

  private loadPersonForEdit(personId: string) {
    this.api.getPerson(personId).subscribe({
      next: (person: PersonResponse) => {
        this.editingPerson = person;
        this.populateFormForEdit();
      },
      error: (error: unknown) => {
        console.error('Error loading person for edit:', error);
        this.error = 'Failed to load person data';
        // Navigate back to the list if person not found
        this.router.navigate(['/people-list']);
      }
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

  onSubmit(event?: Event) {
    if (event) {
      event.preventDefault();
    }
    
    if (this.form.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      this.error = null;
      this.successMessage = null;
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
            this.error = null;
            this.successMessage = `Person "${payload.fullName}" has been updated successfully!`;
            // Show success message for a few seconds, then navigate
            setTimeout(() => {
              this.router.navigate(['/people-list']);
            }, 2000);
          },
          error: (error: unknown) => {
            console.error('Error updating person:', error);
            this.handleApiError(error, 'update');
            this.isSubmitting = false;
          }
        });
      } else {
        // Create new person
        this.api.createPerson(payload).subscribe({
          next: (person: PersonResponse) => {
            this.isSubmitting = false;
            this.error = null;
            this.successMessage = `Person "${person.fullName}" has been created successfully!`;
            this.resetForm();
            // Show success message for a few seconds, then navigate
            setTimeout(() => {
              this.router.navigate(['/people-list']);
            }, 2000);
          },
          error: (error: unknown) => {
            console.error('Error creating person:', error);
            this.handleApiError(error, 'create');
            this.isSubmitting = false;
          }
        });
      }
    }
  }

  onCancel() {
    // Navigate back to the list
    this.router.navigate(['/people-list']);
  }

  onReset() {
    this.resetForm();
  }

  private resetForm() {
    this.form.reset();
    this.selectedRoleIds.clear();
    this.error = null;
    this.successMessage = null;
  }

  private handleApiError(error: unknown, operation?: 'create' | 'update') {
    console.error('API Error:', error);

    const httpError = error as {
      status?: number;
      error?: { errors?: Record<string, string[]>; detail?: string; title?: string; message?: string }
    };

    // Handle specific HTTP status codes first
    if (httpError.status === 403) {
      this.error = 'Permission denied. You do not have the required role to create people. Please contact an administrator.';
      return;
    }

    if (httpError.status === 401) {
      this.error = 'Authentication failed. Please log in again.';
      return;
    }

    if (httpError.status === 404) {
      this.error = 'API endpoint not found. Please contact support.';
      return;
    }

    if (httpError.status === 500) {
      this.error = 'Server error occurred. Please try again later or contact support.';
      return;
    }

    // Handle validation errors
    if (httpError.error?.errors) {
      const errors = httpError.error.errors;
      const errorMessages = Object.keys(errors).map(key =>
        `${key}: ${errors[key].join(', ')}`
      ).join('; ');
      this.error = errorMessages;
    } else if (httpError.error?.detail) {
      this.error = httpError.error.detail;
    } else if (httpError.error?.title) {
      this.error = httpError.error.title;
    } else if (httpError.error?.message) {
      this.error = httpError.error.message;
    } else {
      // Provide specific error messages based on operation
      const statusText = httpError.status ? ` (Status: ${httpError.status})` : '';
      if (operation === 'create') {
        this.error = `Failed to create person${statusText}. Please check your input and try again.`;
      } else if (operation === 'update') {
        this.error = `Failed to update person${statusText}. Please check your input and try again.`;
      } else {
        this.error = `An error occurred${statusText}. Please check your input and try again.`;
      }
    }
  }
}
