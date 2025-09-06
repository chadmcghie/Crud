import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { PeopleComponent } from './people.component';
import { ApiService, RoleDto, PersonResponse } from './api.service';

describe('PeopleComponent', () => {
  let component: PeopleComponent;
  let fixture: ComponentFixture<PeopleComponent>;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockRoles: RoleDto[] = [
    { id: '1', name: 'Admin', description: 'Administrator role' },
    { id: '2', name: 'User', description: 'Regular user role' }
  ];

  const mockPerson: PersonResponse = {
    id: '1',
    fullName: 'John Doe',
    phone: '123-456-7890',
    roles: [{ id: '1', name: 'Admin', description: 'Administrator role' }]
  };

  beforeEach(async () => {
    const apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'listRoles',
      'createPerson',
      'updatePerson'
    ]);

    await TestBed.configureTestingModule({
      imports: [PeopleComponent, ReactiveFormsModule, HttpClientTestingModule],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PeopleComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
  });

  beforeEach(() => {
    apiService.listRoles.and.returnValue(of(mockRoles));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty values', () => {
    fixture.detectChanges();
    
    expect(component.form.get('fullName')?.value).toBe('');
    expect(component.form.get('phone')?.value).toBe('');
    expect(component.selectedRoleIds.size).toBe(0);
  });

  it('should load roles on init', () => {
    fixture.detectChanges();
    
    expect(apiService.listRoles).toHaveBeenCalled();
    expect(component.roles).toEqual(mockRoles);
  });

  it('should handle roles loading error', () => {
    apiService.listRoles.and.returnValue(throwError(() => new Error('API Error')));
    
    // Suppress expected console error during test
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    expect(component.rolesError).toBe('Failed to load roles. Role assignment may not work properly.');
    expect(component.roles).toEqual([]);
    expect(console.error).toHaveBeenCalledWith('Error loading roles:', jasmine.any(Error));
  });

  it('should populate form when editing person', () => {
    component.editingPerson = mockPerson;
    
    fixture.detectChanges();
    
    expect(component.form.get('fullName')?.value).toBe(mockPerson.fullName);
    expect(component.form.get('phone')?.value).toBe(mockPerson.phone);
    expect(component.selectedRoleIds.has('1')).toBe(true);
  });

  it('should validate required fields', () => {
    fixture.detectChanges();
    
    const fullNameControl = component.form.get('fullName');
    fullNameControl?.markAsTouched();
    
    expect(component.isFieldInvalid('fullName')).toBe(true);
    
    fullNameControl?.setValue('John Doe');
    expect(component.isFieldInvalid('fullName')).toBe(false);
  });

  it('should toggle role selection', () => {
    fixture.detectChanges();
    
    expect(component.selectedRoleIds.has('1')).toBe(false);
    
    component.toggleRole('1', true);
    expect(component.selectedRoleIds.has('1')).toBe(true);
    
    component.toggleRole('1', false);
    expect(component.selectedRoleIds.has('1')).toBe(false);
  });

  it('should not toggle role with empty id', () => {
    fixture.detectChanges();
    
    component.toggleRole('', true);
    expect(component.selectedRoleIds.size).toBe(0);
  });

  it('should create person successfully', () => {
    const newPerson: PersonResponse = {
      id: '2',
      fullName: 'Jane Smith',
      phone: '555-0123',
      roles: []
    };
    
    apiService.createPerson.and.returnValue(of(newPerson));
    spyOn(component.personSaved, 'emit');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      fullName: 'Jane Smith',
      phone: '555-0123'
    });
    
    component.onSubmit();
    
    expect(apiService.createPerson).toHaveBeenCalledWith({
      fullName: 'Jane Smith',
      phone: '555-0123',
      roleIds: []
    });
    expect(component.personSaved.emit).toHaveBeenCalledWith(newPerson);
    expect(component.isSubmitting).toBe(false);
  });

  it('should update person successfully', () => {
    component.editingPerson = mockPerson;
    apiService.updatePerson.and.returnValue(of(undefined));
    spyOn(component.personSaved, 'emit');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      fullName: 'John Updated',
      phone: '999-888-7777'
    });
    // Clear existing roles and add only role '2'
    component.selectedRoleIds.clear();
    component.selectedRoleIds.add('2');
    
    component.onSubmit();
    
    expect(apiService.updatePerson).toHaveBeenCalledWith('1', {
      fullName: 'John Updated',
      phone: '999-888-7777',
      roleIds: ['2']
    });
    expect(component.personSaved.emit).toHaveBeenCalledWith(mockPerson);
    expect(component.isSubmitting).toBe(false);
  });

  it('should handle create person error', () => {
    apiService.createPerson.and.returnValue(throwError(() => new Error('API Error')));
    
    // Suppress expected console error during test
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      fullName: 'Jane Smith',
      phone: '555-0123'
    });
    
    component.onSubmit();
    
    expect(component.error).toBe('Failed to create person. Please check your input and try again.');
    expect(component.isSubmitting).toBe(false);
    expect(console.error).toHaveBeenCalledWith('Error creating person:', jasmine.any(Error));
  });

  it('should handle update person error', () => {
    component.editingPerson = mockPerson;
    apiService.updatePerson.and.returnValue(throwError(() => new Error('API Error')));
    
    // Suppress expected console error during test
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    component.form.patchValue({
      fullName: 'John Updated'
    });
    
    component.onSubmit();
    
    expect(component.error).toBe('Failed to update person. Please check your input and try again.');
    expect(component.isSubmitting).toBe(false);
    expect(console.error).toHaveBeenCalledWith('Error updating person:', jasmine.any(Error));
  });

  it('should not submit invalid form', () => {
    fixture.detectChanges();
    
    // Form is invalid (fullName is required)
    component.onSubmit();
    
    expect(apiService.createPerson).not.toHaveBeenCalled();
    expect(apiService.updatePerson).not.toHaveBeenCalled();
  });

  it('should not submit when already submitting', () => {
    fixture.detectChanges();
    
    component.form.patchValue({ fullName: 'Test' });
    component.isSubmitting = true;
    
    component.onSubmit();
    
    expect(apiService.createPerson).not.toHaveBeenCalled();
  });

  it('should emit cancelled event', () => {
    spyOn(component.cancelled, 'emit');
    
    component.onCancel();
    
    expect(component.cancelled.emit).toHaveBeenCalled();
  });

  it('should reset form', () => {
    fixture.detectChanges();
    
    component.form.patchValue({
      fullName: 'Test',
      phone: '123'
    });
    component.selectedRoleIds.add('1');
    component.error = 'Some error';
    
    component.onReset();
    
    expect(component.form.get('fullName')?.value).toBe(null);
    expect(component.form.get('phone')?.value).toBe(null);
    expect(component.selectedRoleIds.size).toBe(0);
    expect(component.error).toBeNull();
  });

  it('should handle phone as null when empty', () => {
    apiService.createPerson.and.returnValue(of(mockPerson));
    
    fixture.detectChanges();
    
    component.form.patchValue({
      fullName: 'Test User',
      phone: ''
    });
    
    component.onSubmit();
    
    expect(apiService.createPerson).toHaveBeenCalledWith({
      fullName: 'Test User',
      phone: null,
      roleIds: []
    });
  });
});
