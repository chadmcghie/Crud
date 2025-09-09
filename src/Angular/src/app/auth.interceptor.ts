import { inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptorFn,
  HttpErrorResponse,
  HttpInterceptor
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap, finalize } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

// Shared state for token refresh coordination
const refreshState = {
  isRefreshing: false,
  refreshTokenSubject: new BehaviorSubject<string | null>(null)
};

/**
 * Functional auth interceptor
 * Adds authentication token to requests and handles token refresh
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip authentication for auth endpoints
  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  // Skip authentication for external APIs
  if (isExternalUrl(req.url)) {
    return next(req);
  }

  // Add token to request if available
  const token = authService.getAccessToken();
  if (token) {
    req = addToken(req, token);
  }

  return next(req).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return handle401Error(req, next, authService, router);
      }
      return throwError(() => error);
    })
  );
};

function addToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

function handle401Error(
  request: HttpRequest<unknown>, 
  next: HttpInterceptorFn,
  authService: AuthService,
  router: Router
): Observable<HttpEvent<unknown>> {
  // Don't retry auth endpoints
  if (isAuthEndpoint(request.url)) {
    return throwError(() => new HttpErrorResponse({ status: 401 }));
  }

  if (!refreshState.isRefreshing) {
    refreshState.isRefreshing = true;
    refreshState.refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((tokenResponse: TokenResponse) => {
        refreshState.isRefreshing = false;
        refreshState.refreshTokenSubject.next(tokenResponse.accessToken);
        
        // Retry the original request with the new token
        return next(addToken(request, tokenResponse.accessToken));
      }),
      catchError((err) => {
        refreshState.isRefreshing = false;
        authService.logout().subscribe();
        router.navigate(['/login']);
        return throwError(() => err);
      }),
      finalize(() => {
        refreshState.isRefreshing = false;
      })
    );
  } else {
    // Wait for the refresh to complete and retry the request
    return refreshState.refreshTokenSubject.pipe(
      filter(token => token != null),
      take(1),
      switchMap(token => {
        return next(addToken(request, token!));
      })
    );
  }
}

function isAuthEndpoint(url: string): boolean {
  const authEndpoints = [
    '/api/auth/login',
    '/api/auth/register',
    '/api/auth/refresh',
    '/api/auth/logout',
    '/api/auth/forgot-password',
    '/api/auth/reset-password'
  ];
  return authEndpoints.some(endpoint => url.includes(endpoint));
}

function isExternalUrl(url: string): boolean {
  return url.startsWith('http://') || url.startsWith('https://');
}

// Legacy class-based interceptor for backward compatibility
// @deprecated Use authInterceptor instead
import { Injectable } from '@angular/core';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Delegate to functional interceptor
    return authInterceptor(request, (req) => next.handle(req));
  }
}