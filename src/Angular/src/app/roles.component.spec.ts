import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { RolesComponent } from './roles.component';
import { ApiService, RoleDto } from './api.service';

describe('RolesComponent', () => {
  let component: RolesComponent;
  let fixture: ComponentFixture<RolesComponent>;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockRole: RoleDto = {
    id: '1',
    name: 'Admin',
    description: 'Administrator role'
  };

  beforeEach(async () => {
    const apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'createRole',
      'updateRole'
    ]);

    await TestBed.configureTestingModule({
      imports: [RolesComponent, ReactiveFormsModule, HttpClientTestingModule],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RolesComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty values', () => {
    fixture.detectChanges();
    
    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.get('description')?.value).toBe('');
  });

  it('should populate form when editing role', () => {
    component.editingRole = mockRole;
    
    fixture.detectChanges();
    
    expect(component.form.get('name')?.value).toBe(mockRole.name);
    expect(component.form.get('description')?.value).toBe(mockRole.description);
  });

  it('should reset form when editingRole changes to null', () => {
    component.editingRole = mockRole;
    fixture.detectChanges();
    
    // Verify form is populated
    expect(component.form.get('name')?.value).toBe(mockRole.name);
    
    // Change to null (simulating cancel/new role)
    component.editingRole = null;
    component.ngOnChanges();
    
    expect(component.form.get('name')?.value).toBe(null);
    expect(component.form.get('description')?.value).toBe(null);
  });

  it('should validate required fields', () => {
    fixture.detectChanges();
    
    const nameControl = component.form.get('name');
    nameControl?.markAsTouched();
    
    expect(component.isFieldInvalid('name')).toBe(true);
    
    nameControl?.setValue('Test Role');
    expect(component.isFieldInvalid('name')).toBe(false);
  });

  it('should create role successfully', () => {
    const newRole: RoleDto = {
      id: '2',
      name: 'Manager',
      description: 'Manager role'
    };
    
    apiService.createRole.and.returnValue(of(newRole));
    spyOn(component.roleSaved, 'emit');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      name: 'Manager',
      description: 'Manager role'
    });
    
    component.onSubmit();
    
    expect(apiService.createRole).toHaveBeenCalledWith({
      name: 'Manager',
      description: 'Manager role'
    });
    expect(component.roleSaved.emit).toHaveBeenCalledWith(newRole);
    expect(component.isSubmitting).toBe(false);
  });

  it('should update role successfully', () => {
    component.editingRole = mockRole;
    apiService.updateRole.and.returnValue(of(undefined));
    spyOn(component.roleSaved, 'emit');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      name: 'Updated Admin',
      description: 'Updated administrator role'
    });
    
    component.onSubmit();
    
    expect(apiService.updateRole).toHaveBeenCalledWith('1', {
      name: 'Updated Admin',
      description: 'Updated administrator role'
    });
    expect(component.roleSaved.emit).toHaveBeenCalledWith(mockRole);
    expect(component.isSubmitting).toBe(false);
  });

  it('should handle create role error', () => {
    apiService.createRole.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      name: 'Test Role',
      description: 'Test description'
    });
    
    component.onSubmit();
    
    expect(console.error).toHaveBeenCalledWith('Error creating role:', jasmine.any(Error));
    expect(component.isSubmitting).toBe(false);
  });

  it('should handle update role error', () => {
    component.editingRole = mockRole;
    apiService.updateRole.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      name: 'Updated Admin'
    });
    
    component.onSubmit();
    
    expect(console.error).toHaveBeenCalledWith('Error updating role:', jasmine.any(Error));
    expect(component.isSubmitting).toBe(false);
  });

  it('should not submit invalid form', () => {
    fixture.detectChanges();
    
    // Form is invalid (name is required)
    component.onSubmit();
    
    expect(apiService.createRole).not.toHaveBeenCalled();
    expect(apiService.updateRole).not.toHaveBeenCalled();
  });

  it('should not submit when already submitting', () => {
    fixture.detectChanges();
    
    component.form.patchValue({ name: 'Test' });
    component.isSubmitting = true;
    
    component.onSubmit();
    
    expect(apiService.createRole).not.toHaveBeenCalled();
  });

  it('should emit cancelled event', () => {
    spyOn(component.cancelled, 'emit');
    
    component.onCancel();
    
    expect(component.cancelled.emit).toHaveBeenCalled();
  });

  it('should reset form', () => {
    fixture.detectChanges();
    
    component.form.patchValue({
      name: 'Test',
      description: 'Test description'
    });
    
    component.onReset();
    
    expect(component.form.get('name')?.value).toBe(null);
    expect(component.form.get('description')?.value).toBe(null);
  });

  it('should handle empty description as null', () => {
    apiService.createRole.and.returnValue(of(mockRole));
    
    fixture.detectChanges();
    
    component.form.patchValue({
      name: 'Test Role',
      description: ''
    });
    
    component.onSubmit();
    
    expect(apiService.createRole).toHaveBeenCalledWith({
      name: 'Test Role',
      description: null
    });
  });

  it('should prevent default form submission', () => {
    const mockEvent = jasmine.createSpyObj('Event', ['preventDefault']);
    apiService.createRole.and.returnValue(of(mockRole));
    
    fixture.detectChanges();
    
    component.form.patchValue({ name: 'Test Role' });
    component.onSubmit(mockEvent);
    
    expect(mockEvent.preventDefault).toHaveBeenCalled();
  });

  it('should display correct title for new role', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const title = compiled.querySelector('h3');
    
    expect(title?.textContent).toBe('Add New Role');
  });

  it('should display correct title for editing role', () => {
    component.editingRole = mockRole;
    
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const title = compiled.querySelector('h3');
    
    expect(title?.textContent).toBe('Edit Role');
  });

  it('should display correct button text for new role', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const submitButton = compiled.querySelector('.btn-primary') as HTMLButtonElement;
    
    expect(submitButton.textContent?.trim()).toBe('Create Role');
  });

  it('should display correct button text for editing role', () => {
    component.editingRole = mockRole;
    
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const submitButton = compiled.querySelector('.btn-primary') as HTMLButtonElement;
    
    expect(submitButton.textContent?.trim()).toBe('Update Role');
  });

  it('should disable submit button when form is invalid', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const submitButton = compiled.querySelector('.btn-primary') as HTMLButtonElement;
    
    expect(submitButton.disabled).toBe(true);
    
    component.form.patchValue({ name: 'Valid Name' });
    fixture.detectChanges();
    
    expect(submitButton.disabled).toBe(false);
  });
});
