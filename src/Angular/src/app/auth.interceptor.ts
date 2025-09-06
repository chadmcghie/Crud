import { inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptorFn,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, take, switchMap, finalize } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

// Functional interceptor (preferred approach)
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Create a temporary instance to use existing logic
  const interceptorInstance = new AuthInterceptor();
  
  // Mock HttpHandler for compatibility
  const handler: HttpHandler = {
    handle: (request: HttpRequest<unknown>) => next(request)
  };
  
  return interceptorInstance.intercept(req, handler);
};

// Legacy class-based interceptor for backward compatibility
export class AuthInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);

  private authService = inject(AuthService);
  private router = inject(Router);

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Skip authentication for auth endpoints
    if (this.isAuthEndpoint(request.url)) {
      return next.handle(request);
    }

    // Skip authentication for external APIs
    if (this.isExternalUrl(request.url)) {
      return next.handle(request);
    }

    // Add token to request if available
    const token = this.authService.getAccessToken();
    if (token) {
      request = this.addToken(request, token);
    }

    return next.handle(request).pipe(
      catchError(error => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          return this.handle401Error(request, next);
        }
        return throwError(() => error);
      })
    );
  }

  private addToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private handle401Error(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Don't retry auth endpoints
    if (this.isAuthEndpoint(request.url)) {
      return throwError(() => new HttpErrorResponse({ status: 401 }));
    }

    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refreshToken().pipe(
        switchMap((tokenResponse: TokenResponse) => {
          this.isRefreshing = false;
          this.refreshTokenSubject.next(tokenResponse.accessToken);
          
          // Retry the original request with the new token
          return next.handle(this.addToken(request, tokenResponse.accessToken));
        }),
        catchError((err) => {
          this.isRefreshing = false;
          this.authService.logout().subscribe();
          this.router.navigate(['/login']);
          return throwError(() => err);
        }),
        finalize(() => {
          this.isRefreshing = false;
        })
      );
    } else {
      // Wait for the refresh to complete and retry the request
      return this.refreshTokenSubject.pipe(
        filter(token => token != null),
        take(1),
        switchMap(token => {
          return next.handle(this.addToken(request, token));
        })
      );
    }
  }

  private isAuthEndpoint(url: string): boolean {
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

  private isExternalUrl(url: string): boolean {
    return url.startsWith('http://') || url.startsWith('https://');
  }
}