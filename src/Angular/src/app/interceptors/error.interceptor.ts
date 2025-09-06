import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        let errorMessage = 'An unexpected error occurred';
        
        if (error.error instanceof ErrorEvent) {
          // Client-side error
          errorMessage = `Client Error: ${error.error.message}`;
        } else {
          // Server-side error
          if (error.error?.detail) {
            errorMessage = error.error.detail;
          } else if (error.error?.title) {
            errorMessage = error.error.title;
          } else if (error.error?.errors) {
            // Validation errors from API
            const errors = error.error.errors;
            const messages = Object.keys(errors).map(key => {
              const fieldErrors = Array.isArray(errors[key]) ? errors[key] : [errors[key]];
              return fieldErrors.join(', ');
            });
            errorMessage = messages.join('; ');
          } else if (error.status === 0) {
            errorMessage = 'Unable to connect to the server. Please check your connection.';
          } else if (error.status === 404) {
            errorMessage = 'The requested resource was not found.';
          } else if (error.status === 400) {
            errorMessage = error.error?.message || 'Invalid request. Please check your input.';
          } else if (error.status === 500) {
            errorMessage = 'Internal server error. Please try again later.';
          } else if (error.status === 401) {
            errorMessage = 'You are not authorized to perform this action.';
          } else if (error.status === 403) {
            errorMessage = 'Access forbidden. You do not have permission to access this resource.';
          }
        }
        
        console.error('HTTP Error:', {
          status: error.status,
          message: errorMessage,
          url: error.url,
          error: error.error
        });
        
        // Pass the error object with enhanced message
        return throwError(() => ({
          ...error,
          message: errorMessage
        }));
      })
    );
  }
}