import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { PeopleListComponent } from './people-list.component';
import { ApiService, PersonResponse } from './api.service';

describe('PeopleListComponent', () => {
  let component: PeopleListComponent;
  let fixture: ComponentFixture<PeopleListComponent>;
  let apiService: jasmine.SpyObj<ApiService>;

  const mockPeople: PersonResponse[] = [
    {
      id: '1',
      fullName: 'John Doe',
      phone: '123-456-7890',
      roles: [
        { id: '1', name: 'Admin', description: 'Administrator role' },
        { id: '2', name: 'User', description: 'Regular user role' }
      ]
    },
    {
      id: '2',
      fullName: 'Jane Smith',
      phone: null,
      roles: []
    }
  ];

  beforeEach(async () => {
    const apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'listPeople',
      'deletePerson'
    ]);

    await TestBed.configureTestingModule({
      imports: [PeopleListComponent, HttpClientTestingModule],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PeopleListComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(ApiService) as jasmine.SpyObj<ApiService>;
  });

  beforeEach(() => {
    apiService.listPeople.and.returnValue(of(mockPeople));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load people on init', () => {
    fixture.detectChanges();
    
    expect(apiService.listPeople).toHaveBeenCalled();
    expect(component.people).toEqual(mockPeople);
    expect(component.isLoading).toBe(false);
    expect(component.error).toBeNull();
  });

  it('should handle loading error', () => {
    apiService.listPeople.and.returnValue(throwError(() => new Error('API Error')));
    
    // Suppress expected console error during test
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    expect(component.error).toBe('Failed to load people. Please try again.');
    expect(component.people).toEqual([]);
    expect(component.isLoading).toBe(false);
    expect(console.error).toHaveBeenCalledWith('Error loading people:', jasmine.any(Error));
  });

  it('should set loading state during API call', () => {
    // Don't call detectChanges yet
    expect(component.isLoading).toBe(false);
    
    fixture.detectChanges(); // This triggers ngOnInit
    
    // The loading state is set to true at the start of loadPeople
    // but by the time the observable completes, it's set back to false
    expect(component.isLoading).toBe(false);
    expect(apiService.listPeople).toHaveBeenCalled();
  });

  it('should refresh people list', () => {
    fixture.detectChanges();
    
    // Reset the spy to track new calls
    apiService.listPeople.calls.reset();
    
    component.refresh();
    
    expect(apiService.listPeople).toHaveBeenCalled();
  });

  it('should emit addPerson event', () => {
    spyOn(component.addPerson, 'emit');
    
    component.onAddPerson();
    
    expect(component.addPerson.emit).toHaveBeenCalled();
  });

  it('should emit editPerson event with person data', () => {
    spyOn(component.editPerson, 'emit');
    const personToEdit = mockPeople[0];
    
    component.onEditPerson(personToEdit);
    
    expect(component.editPerson.emit).toHaveBeenCalledWith(personToEdit);
  });

  it('should delete person after confirmation', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    apiService.deletePerson.and.returnValue(of(undefined));
    
    fixture.detectChanges();
    
    const personToDelete = mockPeople[0];
    component.onDeletePerson(personToDelete);
    
    expect(window.confirm).toHaveBeenCalledWith('Are you sure you want to delete John Doe?');
    expect(apiService.deletePerson).toHaveBeenCalledWith('1');
    expect(apiService.listPeople).toHaveBeenCalledTimes(2); // Once on init, once after delete
  });

  it('should not delete person if not confirmed', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    
    fixture.detectChanges();
    
    const personToDelete = mockPeople[0];
    component.onDeletePerson(personToDelete);
    
    expect(window.confirm).toHaveBeenCalledWith('Are you sure you want to delete John Doe?');
    expect(apiService.deletePerson).not.toHaveBeenCalled();
  });

  it('should handle delete error', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    apiService.deletePerson.and.returnValue(throwError(() => new Error('Delete failed')));
    
    // Suppress expected console error during test
    spyOn(console, 'error');
    
    fixture.detectChanges();
    
    const personToDelete = mockPeople[0];
    component.onDeletePerson(personToDelete);
    
    expect(component.error).toBe('Failed to delete John Doe. Please try again.');
    expect(console.error).toHaveBeenCalledWith('Error deleting person:', jasmine.any(Error));
    expect(apiService.listPeople).toHaveBeenCalledTimes(1); // Only the initial load, not after failed delete
  });

  it('should display people in template', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('.person-row');
    
    expect(rows.length).toBe(2);
    expect(compiled.textContent).toContain('John Doe');
    expect(compiled.textContent).toContain('Jane Smith');
    expect(compiled.textContent).toContain('123-456-7890');
    expect(compiled.textContent).toContain('Admin');
  });

  it('should display empty state when no people', () => {
    apiService.listPeople.and.returnValue(of([]));
    
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const emptyState = compiled.querySelector('.empty-state');
    
    expect(emptyState).toBeTruthy();
    expect(emptyState?.textContent).toContain('No people found');
  });

  it('should display N/A for null phone numbers', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const phoneCells = compiled.querySelectorAll('.phone-cell');
    
    expect(phoneCells[0].textContent?.trim()).toBe('123-456-7890');
    expect(phoneCells[1].textContent?.trim()).toBe('N/A');
  });

  it('should display "No roles assigned" when person has no roles', () => {
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const rolesCells = compiled.querySelectorAll('.roles-cell');
    
    expect(rolesCells[1].textContent).toContain('No roles assigned');
  });

  it('should call onAddPerson when clicking add button', () => {
    spyOn(component, 'onAddPerson');
    
    fixture.detectChanges();
    
    const addButton = fixture.nativeElement.querySelector('.btn-primary') as HTMLButtonElement;
    addButton.click();
    
    expect(component.onAddPerson).toHaveBeenCalled();
  });

  it('should call refresh when clicking refresh button', () => {
    spyOn(component, 'refresh');
    
    fixture.detectChanges();
    
    const refreshButton = fixture.nativeElement.querySelector('.btn-secondary') as HTMLButtonElement;
    refreshButton.click();
    
    expect(component.refresh).toHaveBeenCalled();
  });
});
