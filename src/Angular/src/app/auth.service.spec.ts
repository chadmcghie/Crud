import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { take } from 'rxjs/operators';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const apiUrl = 'http://localhost:5172/api';

  beforeEach(() => {
    // Clear any stored tokens before each test
    sessionStorage.clear();
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should send login request and store tokens', () => {
      const mockCredentials = { email: 'test@example.com', password: 'password123' };
      const mockResponse = {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        user: {
          id: '123',
          email: 'test@example.com',
          roles: ['user']
        }
      };

      service.login(mockCredentials.email, mockCredentials.password).subscribe(response => {
        expect(response).toEqual(mockResponse);
        expect(service.getAccessToken()).toBe('mock-access-token');
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(mockCredentials);
      req.flush(mockResponse);
    });

    it('should update current user on successful login', () => {
      const mockResponse = {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        user: {
          id: '123',
          email: 'test@example.com',
          roles: ['user']
        }
      };

      let currentUser = null;
      service.currentUser$.pipe(take(1)).subscribe(user => {
        currentUser = user;
      });
      expect(currentUser).toBeNull();

      service.login('test@example.com', 'password123').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush(mockResponse);

      service.currentUser$.pipe(take(1)).subscribe(user => {
        expect(user).toEqual(mockResponse.user);
      });
    });

    it('should handle login error', () => {
      const mockError = { error: { message: 'Invalid credentials' } };

      service.login('test@example.com', 'wrong-password').subscribe(
        () => fail('should have failed'),
        error => {
          expect(error.error.error.message).toBe('Invalid credentials');
        }
      );

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush(mockError, { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('register', () => {
    it('should send registration request and store tokens', () => {
      const mockRegistration = {
        email: 'newuser@example.com',
        password: 'password123',
        confirmPassword: 'password123'
      };
      const mockResponse = {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        user: {
          id: '456',
          email: 'newuser@example.com',
          roles: ['user']
        }
      };

      service.register(mockRegistration.email, mockRegistration.password, mockRegistration.confirmPassword)
        .subscribe(response => {
          expect(response).toEqual(mockResponse);
          expect(service.getAccessToken()).toBe('mock-access-token');
        });

      const req = httpMock.expectOne(`${apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(mockRegistration);
      req.flush(mockResponse);
    });

    it('should update current user on successful registration', () => {
      const mockResponse = {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        user: {
          id: '456',
          email: 'newuser@example.com',
          roles: ['user']
        }
      };

      service.register('newuser@example.com', 'password123', 'password123').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/auth/register`);
      req.flush(mockResponse);

      service.currentUser$.pipe(take(1)).subscribe(user => {
        expect(user).toEqual(mockResponse.user);
      });
    });

    it('should handle registration error', () => {
      const mockError = { error: { message: 'Email already exists' } };

      service.register('existing@example.com', 'password123', 'password123').subscribe(
        () => fail('should have failed'),
        error => {
          expect(error.error.error.message).toBe('Email already exists');
        }
      );

      const req = httpMock.expectOne(`${apiUrl}/auth/register`);
      req.flush(mockError, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('logout', () => {
    it('should clear tokens and reset user state', () => {
      // Setup initial state
      sessionStorage.setItem('access_token', 'mock-token');
      service['currentUserSubject'].next({
        id: '123',
        email: 'test@example.com',
        roles: ['user']
      });

      service.logout().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/auth/logout`);
      req.flush({});

      expect(sessionStorage.getItem('access_token')).toBeNull();
      service.currentUser$.pipe(take(1)).subscribe(user => {
        expect(user).toBeNull();
      });
    });

    it('should send logout request to server', () => {
      service.logout().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/auth/logout`);
      expect(req.request.method).toBe('POST');
      req.flush({});
    });
  });

  describe('refreshToken', () => {
    it('should refresh access token', () => {
      const mockResponse = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token'
      };

      service.refreshToken().subscribe(response => {
        expect(response).toEqual(mockResponse);
        expect(service.getAccessToken()).toBe('new-access-token');
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });

    it('should handle refresh token error', () => {
      const mockError = { error: { message: 'Invalid refresh token' } };

      service.refreshToken().subscribe(
        () => fail('should have failed'),
        error => {
          expect(error.error.error.message).toBe('Invalid refresh token');
        }
      );

      const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
      req.flush(mockError, { status: 401, statusText: 'Unauthorized' });
    });

    it('should clear tokens and user on refresh failure', () => {
      sessionStorage.setItem('access_token', 'old-token');
      service['currentUserSubject'].next({
        id: '123',
        email: 'test@example.com',
        roles: ['user']
      });

      service.refreshToken().subscribe(
        () => fail('should have failed'),
        () => {
          expect(sessionStorage.getItem('access_token')).toBeNull();
          service.currentUser$.pipe(take(1)).subscribe(user => {
            expect(user).toBeNull();
          });
        }
      );

      const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
      req.flush({}, { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('isAuthenticated', () => {
    it('should return true when access token exists', () => {
      sessionStorage.setItem('access_token', 'mock-token');
      expect(service.isAuthenticated()).toBe(true);
    });

    it('should return false when access token does not exist', () => {
      sessionStorage.removeItem('access_token');
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('getAccessToken', () => {
    it('should return stored access token', () => {
      sessionStorage.setItem('access_token', 'stored-token');
      expect(service.getAccessToken()).toBe('stored-token');
    });

    it('should return null when no token exists', () => {
      sessionStorage.removeItem('access_token');
      expect(service.getAccessToken()).toBeNull();
    });
  });

  describe('hasRole', () => {
    it('should return true when user has the specified role', () => {
      service['currentUserSubject'].next({
        id: '123',
        email: 'test@example.com',
        roles: ['user', 'admin']
      });

      expect(service.hasRole('admin')).toBe(true);
      expect(service.hasRole('user')).toBe(true);
    });

    it('should return false when user does not have the specified role', () => {
      service['currentUserSubject'].next({
        id: '123',
        email: 'test@example.com',
        roles: ['user']
      });

      expect(service.hasRole('admin')).toBe(false);
    });

    it('should return false when no user is logged in', () => {
      service['currentUserSubject'].next(null);
      expect(service.hasRole('admin')).toBe(false);
    });
  });

  describe('token persistence', () => {
    it('should store tokens in sessionStorage by default', () => {
      const mockResponse = {
        accessToken: 'session-token',
        refreshToken: 'refresh-token',
        user: { id: '123', email: 'test@example.com', roles: ['user'] }
      };

      service.login('test@example.com', 'password123').subscribe();
      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush(mockResponse);

      expect(sessionStorage.getItem('access_token')).toBe('session-token');
      expect(localStorage.getItem('access_token')).toBeNull();
    });

    it('should store tokens in localStorage when remember me is true', () => {
      const mockResponse = {
        accessToken: 'persistent-token',
        refreshToken: 'refresh-token',
        user: { id: '123', email: 'test@example.com', roles: ['user'] }
      };

      service.login('test@example.com', 'password123', true).subscribe();
      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush(mockResponse);

      expect(localStorage.getItem('access_token')).toBe('persistent-token');
      expect(sessionStorage.getItem('access_token')).toBeNull();
    });
  });

  describe('initialization', () => {
    it('should load user from stored token on service creation', () => {
      // Store a token before creating the service
      sessionStorage.setItem('access_token', 'existing-token');
      
      // Recreate TestBed with new service instance  
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [AuthService]
      });
      
      const newService = TestBed.inject(AuthService);
      const newHttpMock = TestBed.inject(HttpTestingController);
      
      // Service constructor should have called validateStoredToken
      const req = newHttpMock.expectOne(`${apiUrl}/auth/me`);
      req.flush({
        id: '789',
        email: 'existing@example.com',
        roles: ['user']
      });

      newService.currentUser$.pipe(take(1)).subscribe(user => {
        expect(user).toEqual({
          id: '789',
          email: 'existing@example.com',
          roles: ['user']
        });
      });
      
      newHttpMock.verify();
    });
  });
});