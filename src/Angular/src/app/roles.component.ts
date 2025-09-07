import { Component, OnInit, OnChanges, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, RoleDto, CreateRoleRequest, UpdateRoleRequest } from './api.service';
import { CustomValidators } from './validators/custom-validators';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  template: `
    <div class="roles-form-container">
      <h3>{{ editingRole ? 'Edit Role' : 'Add New Role' }}</h3>
      
      <form [formGroup]="form" (ngSubmit)="onSubmit($event)" class="role-form">
        <div class="form-group">
          <label for="name">Role Name *</label>
          <input 
            id="name"
            type="text"
            placeholder="Enter role name" 
            formControlName="name" 
            class="form-control"
            [class.error]="isFieldInvalid('name')"
          />
          <div class="error-message" *ngIf="isFieldInvalid('name')">
            <span *ngIf="form.get('name')?.errors?.['required']">Role name is required</span>
            <span *ngIf="form.get('name')?.errors?.['invalidRoleName']">{{ form.get('name')?.errors?.['invalidRoleName'] }}</span>
            <span *ngIf="form.get('name')?.errors?.['maxLength']">{{ form.get('name')?.errors?.['maxLength'] }}</span>
          </div>
        </div>

        <div class="form-group">
          <label for="description">Description</label>
          <textarea 
            id="description"
            placeholder="Enter role description (optional)" 
            formControlName="description" 
            class="form-control textarea"
            rows="3"
            [class.error]="isFieldInvalid('description')"
          ></textarea>
          <div class="error-message" *ngIf="isFieldInvalid('description')">
            <span *ngIf="form.get('description')?.errors?.['maxlength']">Description cannot exceed 500 characters</span>
          </div>
          <div class="help-text" *ngIf="!isFieldInvalid('description')">
            Provide a brief description of this role's responsibilities and permissions.
          </div>
        </div>

        <div class="form-actions">
          <button type="submit" class="btn btn-primary" [disabled]="!form.valid || isSubmitting">
            {{ isSubmitting ? 'Saving...' : (editingRole ? 'Update Role' : 'Create Role') }}
          </button>
          <button type="button" class="btn btn-secondary" (click)="onCancel()" [disabled]="isSubmitting">
            Cancel
          </button>
          <button type="button" class="btn btn-outline" (click)="onReset()" [disabled]="isSubmitting">
            Reset
          </button>
        </div>
        <div class="error-alert" *ngIf="error">
          {{ error }}
        </div>
      </form>
    </div>
  `,
  styles: [`
    .roles-form-container {
      background: white;
      border-radius: 8px;
      padding: 24px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      max-width: 500px;
    }
    
    h3 {
      margin: 0 0 24px 0;
      color: #333;
      font-weight: 600;
      border-bottom: 2px solid #28a745;
      padding-bottom: 8px;
    }
    
    .role-form {
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
      font-family: inherit;
    }
    
    .form-control:focus {
      outline: none;
      border-color: #28a745;
      box-shadow: 0 0 0 3px rgba(40, 167, 69, 0.1);
    }
    
    .form-control.error {
      border-color: #dc3545;
    }
    
    .textarea {
      resize: vertical;
      min-height: 80px;
    }
    
    .error-message {
      color: #dc3545;
      font-size: 12px;
      margin-top: -4px;
    }
    
    .help-text {
      color: #666;
      font-size: 12px;
      margin-top: -4px;
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
      background: #28a745;
      color: white;
    }
    
    .btn-primary:hover:not(:disabled) {
      background: #1e7e34;
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
      color: #28a745;
      border: 1px solid #28a745;
    }
    
    .btn-outline:hover:not(:disabled) {
      background: #28a745;
      color: white;
    }
    
    .error-alert {
      margin-top: 16px;
      padding: 12px;
      background-color: #f8d7da;
      border: 1px solid #f5c6cb;
      border-radius: 4px;
      color: #721c24;
      font-size: 14px;
    }
  `]
})
export class RolesComponent implements OnInit, OnChanges {
  @Input() editingRole: RoleDto | null = null;
  @Output() roleSaved = new EventEmitter<RoleDto>();
  @Output() cancelled = new EventEmitter<void>();

  form: FormGroup;
  isSubmitting = false;
  error: string | null = null;
  
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  constructor() {
    this.form = this.fb.group({
      name: ['', [Validators.required, CustomValidators.roleName()]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  ngOnInit() {
    // Check for edit query parameter
    this.route.queryParams.subscribe(params => {
      if (params['edit']) {
        const roleId = params['edit'];
        this.loadRoleForEdit(roleId);
      } else {
        this.editingRole = null;
        this.resetForm();
      }
    });
  }

  private loadRoleForEdit(roleId: string) {
    this.api.getRole(roleId).subscribe({
      next: (role: RoleDto) => {
        this.editingRole = role;
        this.populateFormForEdit();
      },
      error: (error: unknown) => {
        console.error('Error loading role for edit:', error);
        this.error = 'Failed to load role data';
        // Navigate back to the list if role not found
        this.router.navigate(['/roles-list']);
      }
    });
  }

  ngOnChanges() {
    if (this.editingRole) {
      this.populateFormForEdit();
    } else {
      this.resetForm();
    }
  }

  private populateFormForEdit() {
    if (!this.editingRole) return;
    
    this.form.patchValue({
      name: this.editingRole.name,
      description: this.editingRole.description
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(event?: Event) {
    if (event) {
      event.preventDefault();
    }
    
    if (this.form.valid && !this.isSubmitting) {
      this.isSubmitting = true;
      const formValue = this.form.value;
      
      const payload: CreateRoleRequest | UpdateRoleRequest = {
        name: formValue.name,
        description: formValue.description || null
      };

      if (this.editingRole) {
        // Update existing role
        this.api.updateRole(this.editingRole.id, payload).subscribe({
          next: () => {
            this.isSubmitting = false;
            this.error = null;
            // Navigate back to the list after successful update
            this.router.navigate(['/roles-list']);
          },
          error: (error: unknown) => {
            console.error('Error updating role:', error);
            this.handleApiError(error);
            this.isSubmitting = false;
          }
        });
      } else {
        // Create new role
        this.api.createRole(payload).subscribe({
          next: (_role: RoleDto) => {
            this.isSubmitting = false;
            this.error = null;
            this.resetForm();
            // Navigate back to the list after successful creation
            this.router.navigate(['/roles-list']);
          },
          error: (error: unknown) => {
            console.error('Error creating role:', error);
            this.handleApiError(error);
            this.isSubmitting = false;
          }
        });
      }
    }
  }

  onCancel() {
    // Navigate back to the list
    this.router.navigate(['/roles-list']);
  }

  onReset() {
    this.resetForm();
  }

  private resetForm() {
    this.form.reset();
    this.error = null;
  }

  private handleApiError(error: unknown) {
    const httpError = error as { error?: { errors?: Record<string, string[]>; detail?: string; title?: string } };
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
    } else {
      this.error = 'An error occurred. Please check your input and try again.';
    }
  }
}
