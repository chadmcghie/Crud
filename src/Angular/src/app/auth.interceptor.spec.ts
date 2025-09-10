import { TestBed, fakeAsync, tick, flush } from '@angular/core/testing';
import { HttpRequest, HttpHandler, HttpErrorResponse, HttpResponse, HttpHeaders, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { delay } from 'rxjs/operators';

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

describe('AuthInterceptor', () => {
  let interceptor: AuthInterceptor;
  let authService: jasmine.SpyObj<AuthService>;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', 
      ['getAccessToken', 'refreshToken', 'logout'],
      { isRefreshing: false }
    );
    const routerSpy = jasmine.createSpyObj('Router', ['navigate', 'createUrlTree', 'serializeUrl']);
    routerSpy.createUrlTree.and.returnValue(Promise.resolve(true));
    routerSpy.serializeUrl.and.returnValue('/test');

    TestBed.configureTestingModule({
      providers: [
        AuthInterceptor,
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        provideHttpClient(withInterceptors([])),
        provideHttpClientTesting()
      ]
    });

    interceptor = TestBed.inject(AuthInterceptor);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  describe('Token Attachment', () => {
    it('should add Authorization header when token exists', () => {
      const token = 'test-token';
      authService.getAccessToken.and.returnValue(token);
      const request = new HttpRequest('GET', '/api/test');
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      expect(next.handle).toHaveBeenCalled();
      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.get('Authorization')).toBe(`Bearer ${token}`);
    });

    it('should not add Authorization header when token is null', () => {
      authService.getAccessToken.and.returnValue(null);
      const request = new HttpRequest('GET', '/api/test');
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      expect(next.handle).toHaveBeenCalled();
      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.has('Authorization')).toBeFalsy();
    });

    it('should not add Authorization header for auth endpoints', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const request = new HttpRequest('POST', '/api/auth/login', {});
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      expect(next.handle).toHaveBeenCalled();
      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.has('Authorization')).toBeFalsy();
    });

    it('should not add Authorization header for register endpoint', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const request = new HttpRequest('POST', '/api/auth/register', {});
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      expect(next.handle).toHaveBeenCalled();
      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.has('Authorization')).toBeFalsy();
    });

    it('should not add Authorization header for refresh endpoint', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const request = new HttpRequest('POST', '/api/auth/refresh', {});
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      expect(next.handle).toHaveBeenCalled();
      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.has('Authorization')).toBeFalsy();
    });
  });

  describe('401 Response Handling', () => {
    it('should handle 401 error and attempt token refresh', fakeAsync(() => {
      authService.getAccessToken.and.returnValue('expired-token');
      authService.refreshToken.and.returnValue(of({ 
        accessToken: 'new-token', 
        refreshToken: 'new-refresh-token' 
      }).pipe(delay(25)));
      
      const request = new HttpRequest('GET', '/api/protected');
      const error = new HttpErrorResponse({ status: 401 });
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle')
          .and.returnValues(
            throwError(() => error),
            of(new HttpResponse({ body: { data: 'success' } }))
          )
      };

      let responseReceived = false;
      interceptor.intercept(request, next).subscribe({
        next: (response) => {
          expect(response).toBeTruthy();
          responseReceived = true;
        },
        error: (err) => fail('Should not error after refresh: ' + err)
      });

      // Advance time to process the refresh delay
      tick(25);
      
      // Process all pending async operations
      flush();

      // Assertions
      expect(responseReceived).toBeTruthy();
      expect(authService.refreshToken).toHaveBeenCalled();
      expect(next.handle).toHaveBeenCalledTimes(2);
    }));

    it('should redirect to login when refresh token fails', fakeAsync(() => {
      authService.getAccessToken.and.returnValue('expired-token');
      authService.refreshToken.and.returnValue(throwError(() => new Error('Refresh failed')).pipe(delay(25)));
      
      const request = new HttpRequest('GET', '/api/protected');
      const error = new HttpErrorResponse({ status: 401 });
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(throwError(() => error))
      };

      let errorReceived = false;
      interceptor.intercept(request, next).subscribe({
        next: () => fail('Should not succeed'),
        error: (err) => {
          expect(err).toBeTruthy();
          errorReceived = true;
        }
      });

      // Advance time to process the refresh failure delay
      tick(25);
      
      // Process all pending async operations
      flush();

      // Assertions
      expect(errorReceived).toBeTruthy();
      expect(authService.refreshToken).toHaveBeenCalled();
      expect(authService.logout).toHaveBeenCalled();
    }));

    it('should not attempt refresh for auth endpoints', () => {
      const request = new HttpRequest('POST', '/api/auth/login', {});
      const error = new HttpErrorResponse({ status: 401 });
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(throwError(() => error))
      };

      interceptor.intercept(request, next).subscribe({
        next: () => fail('Should not succeed'),
        error: (err) => {
          expect(err.status).toBe(401);
        }
      });

      expect(authService.refreshToken).not.toHaveBeenCalled();
    });

    it('should pass through non-401 errors', () => {
      authService.getAccessToken.and.returnValue('valid-token');
      const request = new HttpRequest('GET', '/api/test');
      const error = new HttpErrorResponse({ status: 500, statusText: 'Server Error' });
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(throwError(() => error))
      };

      interceptor.intercept(request, next).subscribe({
        next: () => fail('Should not succeed'),
        error: (err) => {
          expect(err.status).toBe(500);
        }
      });

      expect(authService.refreshToken).not.toHaveBeenCalled();
    });
  });

  describe('Request Queuing', () => {
    it('should queue requests during token refresh', fakeAsync(() => {
      const firstRequest = new HttpRequest('GET', '/api/data1');
      const secondRequest = new HttpRequest('GET', '/api/data2');
      
      authService.getAccessToken.and.returnValue('expired-token');
      const refreshSubject = new Subject<TokenResponse>();
      authService.refreshToken.and.returnValue(refreshSubject.asObservable());
      
      const error = new HttpErrorResponse({ status: 401 });
      const next1: HttpHandler = {
        handle: jasmine.createSpy('handle1')
          .and.returnValues(
            throwError(() => error),
            of(new HttpResponse({ body: { data: 'data1' } }))
          )
      };
      const next2: HttpHandler = {
        handle: jasmine.createSpy('handle2')
          .and.returnValues(
            throwError(() => error),
            of(new HttpResponse({ body: { data: 'data2' } }))
          )
      };

      let response1Received = false;
      let response2Received = false;

      // Start first request which will trigger refresh
      interceptor.intercept(firstRequest, next1).subscribe({
        next: () => { 
          response1Received = true;
        },
        error: (err) => fail('First request should not error: ' + err)
      });

      // Advance time to ensure first request has started processing
      tick();

      // Start second request which should be queued
      interceptor.intercept(secondRequest, next2).subscribe({
        next: () => { 
          response2Received = true;
        },
        error: (err) => fail('Second request should not error: ' + err)
      });

      // Advance time to process request queueing
      tick();

      // Complete the refresh to release queued requests
      refreshSubject.next({ accessToken: 'new-token', refreshToken: 'new-refresh' } as TokenResponse);
      refreshSubject.complete();

      // Process all pending async operations
      flush();

      // Assertions
      expect(authService.refreshToken).toHaveBeenCalledTimes(1);
      expect(response1Received).toBeTruthy();
      expect(response2Received).toBeTruthy();
    }));

    it('should handle multiple concurrent 401 responses', fakeAsync(() => {
      authService.getAccessToken.and.returnValue('expired-token');
      authService.refreshToken.and.returnValue(of({ 
        accessToken: 'new-token', 
        refreshToken: 'new-refresh-token' 
      }).pipe(delay(100)));

      const requests = [
        new HttpRequest('GET', '/api/data1'),
        new HttpRequest('GET', '/api/data2'),
        new HttpRequest('GET', '/api/data3')
      ];

      const error = new HttpErrorResponse({ status: 401 });
      let completedRequests = 0;

      requests.forEach((request, index) => {
        const next: HttpHandler = {
          handle: jasmine.createSpy(`handle${index}`)
            .and.returnValues(
              throwError(() => error),
              of(new HttpResponse({ body: { data: `data${index}` } }))
            )
        };

        interceptor.intercept(request, next).subscribe({
          next: () => {
            completedRequests++;
          },
          error: (err) => fail(`Request ${index} should not error: ` + err)
        });
      });

      // Advance time to process the refresh delay
      tick(100);
      
      // Process all pending async operations
      flush();

      // Assertions
      expect(completedRequests).toBe(requests.length);
      expect(authService.refreshToken).toHaveBeenCalledTimes(1);
    }));

    it('should release queued requests when refresh fails', fakeAsync(() => {
      const request1 = new HttpRequest('GET', '/api/data1');
      const request2 = new HttpRequest('GET', '/api/data2');
      
      authService.getAccessToken.and.returnValue('expired-token');
      authService.refreshToken.and.returnValue(throwError(() => new Error('Refresh failed')).pipe(delay(50)));
      
      const error = new HttpErrorResponse({ status: 401 });
      const next1: HttpHandler = {
        handle: jasmine.createSpy('handle1').and.returnValue(throwError(() => error))
      };
      const next2: HttpHandler = {
        handle: jasmine.createSpy('handle2').and.returnValue(throwError(() => error))
      };

      let error1Received = false;
      let error2Received = false;

      // First request triggers refresh which will fail
      interceptor.intercept(request1, next1).subscribe({
        next: () => fail('Request 1 should not succeed'),
        error: () => { 
          error1Received = true;
        }
      });

      // Advance time slightly to ensure first request is processing
      tick();

      // Second request should also fail when refresh fails
      interceptor.intercept(request2, next2).subscribe({
        next: () => fail('Request 2 should not succeed'),
        error: () => { 
          error2Received = true;
        }
      });

      // Advance time to process the refresh failure delay
      tick(50);
      
      // Process all pending async operations
      flush();

      // Assertions
      expect(error1Received).toBeTruthy();
      expect(error2Received).toBeTruthy();
      expect(authService.logout).toHaveBeenCalled();
    }));
  });

  describe('Edge Cases', () => {
    it('should handle request with existing Authorization header', () => {
      authService.getAccessToken.and.returnValue('new-token');
      const request = new HttpRequest('GET', '/api/test', null, {
        headers: new HttpHeaders({ Authorization: 'Bearer old-token' })
      });
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.get('Authorization')).toBe('Bearer new-token');
    });

    it('should handle external API requests', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const request = new HttpRequest('GET', 'https://external-api.com/data');
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.has('Authorization')).toBeFalsy();
    });

    it('should handle requests with query parameters', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const request = new HttpRequest('GET', '/api/test?param=value');
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.get('Authorization')).toBe('Bearer test-token');
      expect(modifiedRequest.url).toBe('/api/test?param=value');
    });

    it('should handle POST requests with body', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const body = { data: 'test' };
      const request = new HttpRequest('POST', '/api/test', body);
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(of(new HttpResponse()))
      };

      interceptor.intercept(request, next);

      const modifiedRequest = (next.handle as jasmine.Spy).calls.mostRecent().args[0];
      expect(modifiedRequest.headers.get('Authorization')).toBe('Bearer test-token');
      expect(modifiedRequest.body).toEqual(body);
    });

    it('should handle network errors', () => {
      authService.getAccessToken.and.returnValue('test-token');
      const request = new HttpRequest('GET', '/api/test');
      const networkError = new Error('Network error');
      const next: HttpHandler = {
        handle: jasmine.createSpy('handle').and.returnValue(throwError(() => networkError))
      };

      interceptor.intercept(request, next).subscribe({
        next: () => fail('Should not succeed'),
        error: (err) => {
          expect(err.message).toBe('Network error');
        }
      });

      expect(authService.refreshToken).not.toHaveBeenCalled();
    });
  });
});