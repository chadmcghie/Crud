import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiService, RoleDto, CreateRoleRequest, UpdateRoleRequest, PersonResponse, CreatePersonRequest, UpdatePersonRequest } from './api.service';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiService]
    });
    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Roles API', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should list roles', () => {
      const mockRoles: RoleDto[] = [
        { id: '1', name: 'Admin', description: 'Administrator role' },
        { id: '2', name: 'User', description: 'Regular user role' }
      ];

      service.listRoles().subscribe(roles => {
        expect(roles).toEqual(mockRoles);
        expect(roles.length).toBe(2);
      });

      const req = httpMock.expectOne('/api/roles');
      expect(req.request.method).toBe('GET');
      req.flush(mockRoles);
    });

    it('should create a role', () => {
      const createRequest: CreateRoleRequest = {
        name: 'Manager',
        description: 'Manager role'
      };
      const mockResponse: RoleDto = {
        id: '3',
        name: 'Manager',
        description: 'Manager role'
      };

      service.createRole(createRequest).subscribe(role => {
        expect(role).toEqual(mockResponse);
      });

      const req = httpMock.expectOne('/api/roles');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createRequest);
      req.flush(mockResponse);
    });

    it('should update a role', () => {
      const roleId = '1';
      const updateRequest: UpdateRoleRequest = {
        name: 'Updated Admin',
        description: 'Updated administrator role'
      };

      service.updateRole(roleId, updateRequest).subscribe(response => {
        expect(response).toBeNull(); // PUT returns void (null)
      });

      const req = httpMock.expectOne(`/api/roles/${roleId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });

    it('should delete a role', () => {
      const roleId = '1';

      service.deleteRole(roleId).subscribe(response => {
        expect(response).toBeNull(); // DELETE returns void (null)
      });

      const req = httpMock.expectOne(`/api/roles/${roleId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('People API', () => {
    it('should list people', () => {
      const mockPeople: PersonResponse[] = [
        {
          id: '1',
          fullName: 'John Doe',
          phone: '123-456-7890',
          roles: [{ id: '1', name: 'Admin', description: 'Administrator' }]
        },
        {
          id: '2',
          fullName: 'Jane Smith',
          phone: null,
          roles: []
        }
      ];

      service.listPeople().subscribe(people => {
        expect(people).toEqual(mockPeople);
        expect(people.length).toBe(2);
      });

      const req = httpMock.expectOne('/api/people');
      expect(req.request.method).toBe('GET');
      req.flush(mockPeople);
    });

    it('should create a person', () => {
      const createRequest: CreatePersonRequest = {
        fullName: 'Bob Johnson',
        phone: '555-0123',
        roleIds: ['1', '2']
      };
      const mockResponse: PersonResponse = {
        id: '3',
        fullName: 'Bob Johnson',
        phone: '555-0123',
        roles: [
          { id: '1', name: 'Admin', description: 'Administrator' },
          { id: '2', name: 'User', description: 'Regular user' }
        ]
      };

      service.createPerson(createRequest).subscribe(person => {
        expect(person).toEqual(mockResponse);
      });

      const req = httpMock.expectOne('/api/people');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createRequest);
      req.flush(mockResponse);
    });

    it('should update a person', () => {
      const personId = '1';
      const updateRequest: UpdatePersonRequest = {
        fullName: 'John Updated',
        phone: '999-888-7777',
        roleIds: ['2']
      };

      service.updatePerson(personId, updateRequest).subscribe(response => {
        expect(response).toBeNull(); // PUT returns void (null)
      });

      const req = httpMock.expectOne(`/api/people/${personId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateRequest);
      req.flush(null);
    });

    it('should delete a person', () => {
      const personId = '1';

      service.deletePerson(personId).subscribe(response => {
        expect(response).toBeNull(); // DELETE returns void (null)
      });

      const req = httpMock.expectOne(`/api/people/${personId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('Error handling', () => {
    it('should handle HTTP errors for roles', () => {
      service.listRoles().subscribe({
        next: () => fail('should have failed with 404 error'),
        error: (error) => {
          expect(error.status).toBe(404);
        }
      });

      const req = httpMock.expectOne('/api/roles');
      req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });

    it('should handle HTTP errors for people', () => {
      service.listPeople().subscribe({
        next: () => fail('should have failed with 500 error'),
        error: (error) => {
          expect(error.status).toBe(500);
        }
      });

      const req = httpMock.expectOne('/api/people');
      req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });
  });
});
