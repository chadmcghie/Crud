import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError, EMPTY } from 'rxjs';
import { delay } from 'rxjs/operators';
import { RegisterComponent } from './register.component';
import { AuthService } from './auth.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['register', 'login']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate', 'createUrlTree', 'serializeUrl']);
    routerSpy.createUrlTree.and.returnValue(Promise.resolve(true));
    routerSpy.serializeUrl.and.returnValue('/test');
    routerSpy.events = EMPTY;
    const activatedRouteSpy = {
      snapshot: {
        queryParams: {},
        paramMap: {
          get: () => null
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteSpy }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Validation', () => {
    it('should initialize form with empty fields', () => {
      expect(component.registerForm.get('email')?.value).toBe('');
      expect(component.registerForm.get('password')?.value).toBe('');
      expect(component.registerForm.get('confirmPassword')?.value).toBe('');
      expect(component.registerForm.get('acceptTerms')?.value).toBe(false);
    });

    it('should mark email as invalid when empty', () => {
      const emailControl = component.registerForm.get('email');
      emailControl?.setValue('');
      emailControl?.markAsTouched();
      expect(emailControl?.invalid).toBeTruthy();
      expect(emailControl?.errors?.['required']).toBeTruthy();
    });

    it('should mark email as invalid with incorrect format', () => {
      const emailControl = component.registerForm.get('email');
      emailControl?.setValue('invalid-email');
      emailControl?.markAsTouched();
      expect(emailControl?.invalid).toBeTruthy();
      expect(emailControl?.errors?.['email']).toBeTruthy();
    });

    it('should mark email as valid with correct format', () => {
      const emailControl = component.registerForm.get('email');
      emailControl?.setValue('test@example.com');
      emailControl?.markAsTouched();
      expect(emailControl?.valid).toBeTruthy();
    });

    it('should mark password as invalid when empty', () => {
      const passwordControl = component.registerForm.get('password');
      passwordControl?.setValue('');
      passwordControl?.markAsTouched();
      expect(passwordControl?.invalid).toBeTruthy();
      expect(passwordControl?.errors?.['required']).toBeTruthy();
    });

    it('should mark password as invalid when too short', () => {
      const passwordControl = component.registerForm.get('password');
      passwordControl?.setValue('12345');
      passwordControl?.markAsTouched();
      expect(passwordControl?.invalid).toBeTruthy();
      expect(passwordControl?.errors?.['minlength']).toBeTruthy();
    });

    it('should mark password as valid when meets requirements', () => {
      const passwordControl = component.registerForm.get('password');
      passwordControl?.setValue('password123');
      passwordControl?.markAsTouched();
      expect(passwordControl?.valid).toBeTruthy();
    });

    it('should mark confirmPassword as invalid when empty', () => {
      const confirmPasswordControl = component.registerForm.get('confirmPassword');
      confirmPasswordControl?.setValue('');
      confirmPasswordControl?.markAsTouched();
      expect(confirmPasswordControl?.invalid).toBeTruthy();
      expect(confirmPasswordControl?.errors?.['required']).toBeTruthy();
    });

    it('should mark form as invalid when passwords do not match', () => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'different123',
        acceptTerms: true
      });
      expect(component.registerForm.errors?.['passwordMismatch']).toBeTruthy();
      expect(component.registerForm.invalid).toBeTruthy();
    });

    it('should mark form as valid when passwords match', () => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        acceptTerms: true
      });
      expect(component.registerForm.errors?.['passwordMismatch']).toBeFalsy();
      expect(component.registerForm.valid).toBeTruthy();
    });

    it('should mark acceptTerms as invalid when not checked', () => {
      const acceptTermsControl = component.registerForm.get('acceptTerms');
      acceptTermsControl?.setValue(false);
      acceptTermsControl?.markAsTouched();
      expect(acceptTermsControl?.invalid).toBeTruthy();
      expect(acceptTermsControl?.errors?.['required']).toBeTruthy();
    });

    it('should mark form as invalid when terms not accepted', () => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        acceptTerms: false
      });
      expect(component.registerForm.invalid).toBeTruthy();
    });

    it('should mark form as valid when all fields are valid', () => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        acceptTerms: true
      });
      expect(component.registerForm.valid).toBeTruthy();
    });
  });

  describe('Form Submission', () => {
    beforeEach(() => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        acceptTerms: true
      });
    });

    it('should not submit when form is invalid', () => {
      component.registerForm.patchValue({ email: '', password: '' });
      component.onSubmit();
      expect(authService.register).not.toHaveBeenCalled();
    });

    it('should submit when form is valid', () => {
      authService.register.and.returnValue(of({ 
        accessToken: 'fake-access-token',
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      component.onSubmit();
      expect(authService.register).toHaveBeenCalledWith('test@example.com', 'password123', 'password123');
    });

    it('should set loading to true when submitting', () => {
      authService.register.and.returnValue(of({ 
        accessToken: 'fake-access-token',
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }).pipe(delay(100)));
      component.onSubmit();
      expect(component.loading).toBeTruthy();
    });

    it('should show success message after successful registration', fakeAsync(() => {
      authService.register.and.returnValue(of({ 
        accessToken: 'fake-access-token',
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      // Mock login for auto-login that happens after registration
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      
      component.onSubmit();
      tick(100);
      
      expect(component.successMessage).toBe('Registration successful! Logging you in...');
      expect(component.loading).toBeFalsy();
    }));

    it('should auto-login after successful registration', fakeAsync(() => {
      authService.register.and.returnValue(of({ 
        accessToken: 'fake-access-token',
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      
      component.onSubmit();
      tick(1100); // After 1 second delay for success message
      
      expect(authService.login).toHaveBeenCalledWith('test@example.com', 'password123', false);
    }));

    it('should navigate to home after auto-login', fakeAsync(() => {
      authService.register.and.returnValue(of({ 
        accessToken: 'fake-access-token',
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      
      component.onSubmit();
      tick(1200);
      
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    }));

    it('should display error message on registration failure', fakeAsync(() => {
      const errorMessage = 'Email already exists';
      authService.register.and.returnValue(throwError(() => ({ error: { message: errorMessage } })));
      
      component.onSubmit();
      tick(100);
      
      expect(component.errorMessage).toBe(errorMessage);
      expect(component.loading).toBeFalsy();
    }));

    it('should display generic error message when no specific error provided', fakeAsync(() => {
      authService.register.and.returnValue(throwError(() => ({})));
      
      component.onSubmit();
      tick(100);
      
      expect(component.errorMessage).toBe('An error occurred during registration. Please try again.');
      expect(component.loading).toBeFalsy();
    }));

    it('should clear messages when form is resubmitted', () => {
      component.errorMessage = 'Previous error';
      component.successMessage = 'Previous success';
      // Use delay to simulate async and prevent immediate success message
      authService.register.and.returnValue(of({ 
        accessToken: 'fake-access-token',
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }).pipe(delay(100)));
      
      component.onSubmit();
      
      expect(component.errorMessage).toBe('');
      // successMessage should still be empty before async completes
      expect(component.successMessage).toBe('');
    });
  });

  describe('UI State', () => {
    it('should disable submit button when form is invalid', () => {
      component.registerForm.patchValue({ email: '', password: '' });
      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
      expect(submitButton.disabled).toBeTruthy();
    });

    it('should enable submit button when form is valid', () => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        acceptTerms: true
      });
      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
      expect(submitButton.disabled).toBeFalsy();
    });

    it('should disable submit button when loading', () => {
      component.registerForm.patchValue({
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123',
        acceptTerms: true
      });
      component.loading = true;
      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
      expect(submitButton.disabled).toBeTruthy();
    });

    it('should show loading indicator when loading', () => {
      component.loading = true;
      fixture.detectChanges();
      const loadingElement = fixture.nativeElement.querySelector('.loading-indicator');
      expect(loadingElement).toBeTruthy();
    });

    it('should hide loading indicator when not loading', () => {
      component.loading = false;
      fixture.detectChanges();
      const loadingElement = fixture.nativeElement.querySelector('.loading-indicator');
      expect(loadingElement).toBeFalsy();
    });

    it('should display error message when present', () => {
      component.errorMessage = 'Test error message';
      fixture.detectChanges();
      const errorElement = fixture.nativeElement.querySelector('.error-message');
      expect(errorElement).toBeTruthy();
      expect(errorElement.textContent).toContain('Test error message');
    });

    it('should display success message when present', () => {
      component.successMessage = 'Test success message';
      fixture.detectChanges();
      const successElement = fixture.nativeElement.querySelector('.success-message');
      expect(successElement).toBeTruthy();
      expect(successElement.textContent).toContain('Test success message');
    });
  });

  describe('Error Handling', () => {
    it('should get error message for required email', () => {
      const emailControl = component.registerForm.get('email');
      emailControl?.setValue('');
      emailControl?.markAsTouched();
      const errorMessage = component.getEmailErrorMessage();
      expect(errorMessage).toBe('Email is required');
    });

    it('should get error message for invalid email format', () => {
      const emailControl = component.registerForm.get('email');
      emailControl?.setValue('invalid');
      emailControl?.markAsTouched();
      const errorMessage = component.getEmailErrorMessage();
      expect(errorMessage).toBe('Please enter a valid email address');
    });

    it('should get error message for required password', () => {
      const passwordControl = component.registerForm.get('password');
      passwordControl?.setValue('');
      passwordControl?.markAsTouched();
      const errorMessage = component.getPasswordErrorMessage();
      expect(errorMessage).toBe('Password is required');
    });

    it('should get error message for short password', () => {
      const passwordControl = component.registerForm.get('password');
      passwordControl?.setValue('12345');
      passwordControl?.markAsTouched();
      const errorMessage = component.getPasswordErrorMessage();
      expect(errorMessage).toBe('Password must be at least 6 characters');
    });

    it('should get error message for password mismatch', () => {
      component.registerForm.patchValue({
        password: 'password123',
        confirmPassword: 'different123'
      });
      component.registerForm.get('confirmPassword')?.markAsTouched();
      const errorMessage = component.getConfirmPasswordErrorMessage();
      expect(errorMessage).toBe('Passwords do not match');
    });

    it('should get error message for required confirmPassword', () => {
      const confirmPasswordControl = component.registerForm.get('confirmPassword');
      confirmPasswordControl?.setValue('');
      confirmPasswordControl?.markAsTouched();
      const errorMessage = component.getConfirmPasswordErrorMessage();
      expect(errorMessage).toBe('Please confirm your password');
    });
  });

  describe('Password Strength', () => {
    it('should calculate weak password strength', () => {
      component.registerForm.get('password')?.setValue('123');
      expect(component.getPasswordStrength()).toBe('weak');
    });

    it('should calculate medium password strength', () => {
      component.registerForm.get('password')?.setValue('password123');
      expect(component.getPasswordStrength()).toBe('medium');
    });

    it('should calculate strong password strength', () => {
      component.registerForm.get('password')?.setValue('P@ssw0rd123!');
      expect(component.getPasswordStrength()).toBe('strong');
    });

    it('should return empty string for no password', () => {
      component.registerForm.get('password')?.setValue('');
      expect(component.getPasswordStrength()).toBe('');
    });
  });
});