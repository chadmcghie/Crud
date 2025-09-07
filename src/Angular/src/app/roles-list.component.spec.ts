import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RolesListComponent } from './roles-list.component';
import { ApiService, RoleDto } from './api.service';

describe('RolesListComponent', () => {
  let component: RolesListComponent;
  let fixture: ComponentFixture<RolesListComponent>;
  let apiService: jasmine.SpyObj<ApiService>;
  let router: jasmine.SpyObj<Router>;

  const mockRoles: RoleDto[] = [
    {
      id: '1',
      name: 'Admin',
      description: 'Administrator role with full access'
    },
    {
      id: '2',
      name: 'User',
      description: null
    },
    {
      id: '3',
      name: 'Manager',
      description: 'Manager role with limited admin access'
    }
  ];

  beforeEach(async () => {
    const apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'listRoles',
      'deleteRole'
    ]);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [RolesListComponent, HttpClientTestingModule],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RolesListComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  beforeEach(() => {
    apiService.listRoles.and.returnValue(of(mockRoles));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load roles on init', () => {
    fixture.detectChanges();
    
    expect(apiService.listRoles).toHaveBeenCalled();
    expect(component.roles).toEqual(mockRoles);
  });

  it('should handle loading error gracefully', () => {
    apiService.listRoles.and.returnValue(throwError(() => new Error('API Error')));
    spyOn(console, 'error'); // Suppress console error in test
    
    fixture.detectChanges();
    
    // Component should still be created even if API fails
    expect(component.roles).toEqual([]);
  });

  it('should refresh roles list', () => {
    fixture.detectChanges();
    
    // Reset the spy to track new calls
    apiService.listRoles.calls.reset();
    
    component.refresh();
    
    expect(apiService.listRoles).toHaveBeenCalled();
  });

  it('should navigate to roles form on add role', () => {
    component.onAddRole();
    
    expect(router.navigate).toHaveBeenCalledWith(['/roles']);
  });

  it('should navigate to roles form with edit parameters', () => {
    const roleToEdit = mockRoles[0];
    
    component.onEditRole(roleToEdit);
    
    expect(router.navigate).toHaveBeenCalledWith(['/roles'], { queryParams: { edit: roleToEdit.id } });
  });

  it('should delete role after confirmation', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    apiService.deleteRole.and.returnValue(of(undefined));
    
    fixture.detectChanges();
    
    const roleToDelete = mockRoles[0];
    component.onDeleteRole(roleToDelete);
    
    expect(window.confirm).toHaveBeenCalledWith('Are you sure you want to delete the role "Admin"?');
    expect(apiService.deleteRole).toHaveBeenCalledWith('1');
    expect(apiService.listRoles).toHaveBeenCalledTimes(2); // Once on init, once after delete
  });

  it('should not delete role if not confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    
    fixture.detectChanges();
    
    const roleToDelete = mockRoles[0];
    component.onDeleteRole(roleToDelete);
    
    expect(window.confirm).toHaveBeenCalledWith('Are you sure you want to delete the role "Admin"?');
    expect(apiService.deleteRole).not.toHaveBeenCalled();
  });

  it('should handle delete error gracefully', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    apiService.deleteRole.and.returnValue(throwError(() => new Error('Delete failed')));
    spyOn(console, 'error'); // Suppress console error in test
    
    fixture.detectChanges();
    
    const roleToDelete = mockRoles[0];
    component.onDeleteRole(roleToDelete);
    
    expect(apiService.deleteRole).toHaveBeenCalledWith('1');
    expect(console.error).toHaveBeenCalledWith('Error deleting role:', jasmine.any(Error));
    expect(apiService.listRoles).toHaveBeenCalledTimes(1); // Only the initial load, not after failed delete
  });

  it('should display roles in template', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('.role-row');
    
    expect(rows.length).toBe(3);
    expect(compiled.textContent).toContain('Admin');
    expect(compiled.textContent).toContain('User');
    expect(compiled.textContent).toContain('Manager');
    expect(compiled.textContent).toContain('Administrator role with full access');
  });

  it('should display empty state when no roles', () => {
    apiService.listRoles.and.returnValue(of([]));
    
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const emptyState = compiled.querySelector('.empty-state');
    
    expect(emptyState).toBeTruthy();
    expect(emptyState?.textContent).toContain('No roles found');
  });

  it('should display N/A for null descriptions', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const descriptionCells = compiled.querySelectorAll('.description-cell');
    
    expect(descriptionCells[0].textContent?.trim()).toBe('Administrator role with full access');
    expect(descriptionCells[1].textContent?.trim()).toBe('N/A');
    expect(descriptionCells[2].textContent?.trim()).toBe('Manager role with limited admin access');
  });

  it('should call onAddRole when clicking add button', () => {
    spyOn(component, 'onAddRole');
    
    fixture.detectChanges();
    
    const addButton = fixture.nativeElement.querySelector('.btn-primary') as HTMLButtonElement;
    addButton.click();
    
    expect(component.onAddRole).toHaveBeenCalled();
  });

  it('should call refresh when clicking refresh button', () => {
    spyOn(component, 'refresh');
    
    fixture.detectChanges();
    
    const refreshButton = fixture.nativeElement.querySelector('.btn-secondary') as HTMLButtonElement;
    refreshButton.click();
    
    expect(component.refresh).toHaveBeenCalled();
  });

  it('should call onEditRole when clicking edit button', () => {
    spyOn(component, 'onEditRole');
    
    fixture.detectChanges();
    
    const editButton = fixture.nativeElement.querySelector('.btn-edit') as HTMLButtonElement;
    editButton.click();
    
    expect(component.onEditRole).toHaveBeenCalledWith(mockRoles[0]);
  });

  it('should call onDeleteRole when clicking delete button', () => {
    spyOn(component, 'onDeleteRole');
    
    fixture.detectChanges();
    
    const deleteButton = fixture.nativeElement.querySelector('.btn-delete') as HTMLButtonElement;
    deleteButton.click();
    
    expect(component.onDeleteRole).toHaveBeenCalledWith(mockRoles[0]);
  });

  it('should show table when roles exist', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const tableContainer = compiled.querySelector('.table-container');
    const emptyState = compiled.querySelector('.empty-state');
    
    expect(tableContainer).toBeTruthy();
    expect(emptyState).toBeFalsy();
  });

  it('should show empty state when no roles exist', () => {
    apiService.listRoles.and.returnValue(of([]));
    
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const tableContainer = compiled.querySelector('.table-container');
    const emptyState = compiled.querySelector('.empty-state');
    
    expect(tableContainer).toBeFalsy();
    expect(emptyState).toBeTruthy();
  });

  it('should have correct table headers', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const headers = compiled.querySelectorAll('th');
    
    expect(headers.length).toBe(3);
    expect(headers[0].textContent?.trim()).toBe('Name');
    expect(headers[1].textContent?.trim()).toBe('Description');
    expect(headers[2].textContent?.trim()).toBe('Actions');
  });
});
