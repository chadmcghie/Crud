import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { tap, catchError, retry } from 'rxjs/operators';

interface User {
  id: string;
  email: string;
  roles: string[];
}

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5172/api';
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser$: Observable<User | null>;
  private refreshTokenTimeout: ReturnType<typeof setTimeout> | null = null;
  private useLocalStorage = false;

  private http = inject(HttpClient);

  constructor() {
    // Check if there's an access token in storage
    const hasToken = !!(localStorage.getItem('access_token') || sessionStorage.getItem('access_token'));
    if (hasToken) {
      this.useLocalStorage = !!localStorage.getItem('access_token');
    }
    
    const storedUser = this.getStoredUser();
    this.currentUserSubject = new BehaviorSubject<User | null>(storedUser);
    this.currentUser$ = this.currentUserSubject.asObservable();
    
    if (this.getAccessToken()) {
      this.validateStoredToken();
      this.scheduleTokenRefresh();
    }
  }

  login(email: string, password: string, rememberMe = false): Observable<AuthResponse> {
    this.useLocalStorage = rememberMe;
    
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, { email, password })
      .pipe(
        tap(response => {
          this.storeTokens(response.accessToken, response.refreshToken);
          this.currentUserSubject.next(response.user);
          this.storeUser(response.user);
          this.scheduleTokenRefresh();
        }),
        catchError(error => {
          console.error('Login error:', error);
          return throwError(() => error);
        })
      );
  }

  register(email: string, password: string, confirmPassword: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, {
      email,
      password,
      confirmPassword
    }).pipe(
      tap(response => {
        this.storeTokens(response.accessToken, response.refreshToken);
        this.currentUserSubject.next(response.user);
        this.storeUser(response.user);
        this.scheduleTokenRefresh();
      }),
      catchError(error => {
        console.error('Registration error:', error);
        return throwError(() => error);
      })
    );
  }

  logout(): Observable<unknown> {
    this.clearTokenRefreshTimer();
    
    // Clear local state immediately
    this.clearTokens();
    this.currentUserSubject.next(null);
    
    // Send logout request to server
    return this.http.post(`${this.apiUrl}/auth/logout`, {}).pipe(
      tap(() => console.log('Logout successful')),
      catchError(error => {
        console.error('Logout error:', error);
        return throwError(() => error);
      })
    );
  }

  refreshToken(): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/auth/refresh`, {})
      .pipe(
        tap(response => {
          this.storeTokens(response.accessToken, response.refreshToken);
          this.scheduleTokenRefresh();
        }),
        catchError(error => {
          console.error('Token refresh error:', error);
          this.clearTokens();
          this.currentUserSubject.next(null);
          return throwError(() => error);
        })
      );
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  getAccessToken(): string | null {
    return this.useLocalStorage 
      ? localStorage.getItem('access_token')
      : sessionStorage.getItem('access_token');
  }

  hasRole(role: string): boolean {
    const currentUser = this.currentUserSubject.value;
    return currentUser ? currentUser.roles.includes(role) : false;
  }

  private validateStoredToken(): void {
    this.http.get<User>(`${this.apiUrl}/auth/me`)
      .pipe(
        retry(1),
        catchError(error => {
          console.error('Token validation failed:', error);
          this.clearTokens();
          this.currentUserSubject.next(null);
          return throwError(() => error);
        })
      )
      .subscribe({
        next: (user) => {
          this.currentUserSubject.next(user);
          this.storeUser(user);
        },
        error: () => {
          // Error already handled in catchError
        }
      });
  }

  private scheduleTokenRefresh(): void {
    this.clearTokenRefreshTimer();
    
    // Parse JWT to get expiration time
    const token = this.getAccessToken();
    if (!token) return;

    try {
      // Check if token has the expected JWT structure
      const parts = token.split('.');
      if (parts.length !== 3) {
        console.warn('Invalid token format');
        return;
      }

      const payload = JSON.parse(atob(parts[1])) as { exp: number; };
      const expirationTime = payload.exp * 1000; // Convert to milliseconds
      const currentTime = Date.now();
      const timeUntilRefresh = expirationTime - currentTime - 60000; // Refresh 1 minute before expiration

      if (timeUntilRefresh > 0) {
        this.refreshTokenTimeout = setTimeout(() => {
          this.refreshToken().subscribe({
            next: () => console.log('Token refreshed successfully'),
            error: (error) => console.error('Auto-refresh failed:', error)
          });
        }, timeUntilRefresh);
      } else {
        // Token is expired or about to expire, refresh immediately
        this.refreshToken().subscribe({
          next: () => console.log('Token refreshed successfully'),
          error: (error) => console.error('Auto-refresh failed:', error)
        });
      }
    } catch (error) {
      console.error('Error parsing token:', error);
    }
  }

  private clearTokenRefreshTimer(): void {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
      this.refreshTokenTimeout = null;
    }
  }

  private storeTokens(accessToken: string, _refreshToken: string): void {
    if (this.useLocalStorage) {
      localStorage.setItem('access_token', accessToken);
      sessionStorage.removeItem('access_token');
    } else {
      sessionStorage.setItem('access_token', accessToken);
      localStorage.removeItem('access_token');
    }
    // Note: Refresh token should be stored as HTTP-only cookie by the backend
  }

  private clearTokens(): void {
    sessionStorage.removeItem('access_token');
    localStorage.removeItem('access_token');
    sessionStorage.removeItem('user');
    localStorage.removeItem('user');
  }

  private storeUser(user: User): void {
    const userJson = JSON.stringify(user);
    if (this.useLocalStorage) {
      localStorage.setItem('user', userJson);
      sessionStorage.removeItem('user');
    } else {
      sessionStorage.setItem('user', userJson);
      localStorage.removeItem('user');
    }
  }

  private getStoredUser(): User | null {
    const userJson = localStorage.getItem('user') || sessionStorage.getItem('user');
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch {
        return null;
      }
    }
    return null;
  }

  // Password Reset Methods with Rate Limiting Awareness
  forgotPassword(email: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/auth/forgot-password`, { email })
      .pipe(
        catchError(error => {
          // Handle rate limiting specifically
          if (error.status === 429) {
            const retryAfter = error.headers?.get('Retry-After');
            const message = retryAfter 
              ? `Too many requests. Please try again in ${retryAfter} seconds.`
              : 'Too many requests. Please try again later.';
            return throwError(() => ({ error: { error: message } }));
          }
          return throwError(() => error);
        })
      );
  }

  resetPassword(token: string, newPassword: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/auth/reset-password`, { 
      token, 
      newPassword 
    }).pipe(
      catchError(error => {
        // Handle rate limiting specifically
        if (error.status === 429) {
          const retryAfter = error.headers?.get('Retry-After');
          const message = retryAfter 
            ? `Too many requests. Please try again in ${retryAfter} seconds.`
            : 'Too many requests. Please try again later.';
          return throwError(() => ({ error: { error: message } }));
        }
        return throwError(() => error);
      })
    );
  }

  validateResetToken(token: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/auth/validate-reset-token`, { token })
      .pipe(
        catchError(error => {
          return throwError(() => error);
        })
      );
  }
}