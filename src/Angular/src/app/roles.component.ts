import { Component, OnInit, OnChanges, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ApiService, RoleDto, CreateRoleRequest, UpdateRoleRequest } from './api.service';

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
            Role name is required
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
          ></textarea>
          <div class="help-text">
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
  `]
})
export class RolesComponent implements OnInit, OnChanges {
  @Input() editingRole: RoleDto | null = null;
  @Output() roleSaved = new EventEmitter<RoleDto>();
  @Output() cancelled = new EventEmitter<void>();

  form: FormGroup;
  isSubmitting = false;

  constructor(private api: ApiService, private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      description: ['']
    });
  }

  ngOnInit() {
    if (this.editingRole) {
      this.populateFormForEdit();
    }
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
    console.log('Role form submitted!');
    console.log('Form valid:', this.form.valid);
    console.log('Form value:', this.form.value);
    console.log('Form errors:', this.form.errors);
    console.log('Is submitting:', this.isSubmitting);
    
    if (this.form.valid && !this.isSubmitting) {
      console.log('Starting role submission...');
      this.isSubmitting = true;
      const formValue = this.form.value;
      
      const payload: CreateRoleRequest | UpdateRoleRequest = {
        name: formValue.name,
        description: formValue.description || null
      };

      console.log('Payload:', payload);
      console.log('Editing role:', this.editingRole);

      if (this.editingRole) {
        // Update existing role
        console.log('Updating role with ID:', this.editingRole.id);
        this.api.updateRole(this.editingRole.id, payload).subscribe({
          next: () => {
            console.log('Role updated successfully');
            this.isSubmitting = false;
            this.roleSaved.emit(this.editingRole!);
          },
          error: (error: any) => {
            console.error('Error updating role:', error);
            this.isSubmitting = false;
          }
        });
      } else {
        // Create new role
        console.log('Creating new role...');
        this.api.createRole(payload).subscribe({
          next: (role: RoleDto) => {
            console.log('Role created successfully:', role);
            this.isSubmitting = false;
            this.roleSaved.emit(role);
            this.resetForm();
          },
          error: (error: any) => {
            console.error('Error creating role:', error);
            this.isSubmitting = false;
          }
        });
      }
    } else {
      console.log('Form submission blocked - invalid or already submitting');
      if (!this.form.valid) {
        console.log('Individual field errors:');
        Object.keys(this.form.controls).forEach(key => {
          const control = this.form.get(key);
          if (control && control.errors) {
            console.log(`${key}:`, control.errors);
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
  }
}
