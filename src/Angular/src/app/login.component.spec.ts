import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError, EMPTY } from 'rxjs';
import { delay } from 'rxjs/operators';
import { LoginComponent } from './login.component';
import { AuthService } from './auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);
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
      imports: [LoginComponent, ReactiveFormsModule],
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
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Validation', () => {
    it('should initialize form with empty fields', () => {
      expect(component.loginForm.get('email')?.value).toBe('');
      expect(component.loginForm.get('password')?.value).toBe('');
    });

    it('should mark email as invalid when empty', () => {
      const emailControl = component.loginForm.get('email');
      emailControl?.setValue('');
      emailControl?.markAsTouched();
      expect(emailControl?.invalid).toBeTruthy();
      expect(emailControl?.errors?.['required']).toBeTruthy();
    });

    it('should mark email as invalid with incorrect format', () => {
      const emailControl = component.loginForm.get('email');
      emailControl?.setValue('invalid-email');
      emailControl?.markAsTouched();
      expect(emailControl?.invalid).toBeTruthy();
      expect(emailControl?.errors?.['email']).toBeTruthy();
    });

    it('should mark email as valid with correct format', () => {
      const emailControl = component.loginForm.get('email');
      emailControl?.setValue('test@example.com');
      emailControl?.markAsTouched();
      expect(emailControl?.valid).toBeTruthy();
    });

    it('should mark password as invalid when empty', () => {
      const passwordControl = component.loginForm.get('password');
      passwordControl?.setValue('');
      passwordControl?.markAsTouched();
      expect(passwordControl?.invalid).toBeTruthy();
      expect(passwordControl?.errors?.['required']).toBeTruthy();
    });

    it('should mark password as valid when not empty', () => {
      const passwordControl = component.loginForm.get('password');
      passwordControl?.setValue('password123');
      passwordControl?.markAsTouched();
      expect(passwordControl?.valid).toBeTruthy();
    });

    it('should mark form as invalid when fields are empty', () => {
      expect(component.loginForm.invalid).toBeTruthy();
    });

    it('should mark form as valid when all fields are valid', () => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123'
      });
      expect(component.loginForm.valid).toBeTruthy();
    });
  });

  describe('Form Submission', () => {
    beforeEach(() => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123'
      });
    });

    it('should not submit when form is invalid', () => {
      component.loginForm.patchValue({ email: '', password: '' });
      component.onSubmit();
      expect(authService.login).not.toHaveBeenCalled();
    });

    it('should submit when form is valid', () => {
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      component.onSubmit();
      expect(authService.login).toHaveBeenCalledWith('test@example.com', 'password123', false);
    });

    it('should set loading to true when submitting', () => {
      // Use delay to simulate async behavior
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }).pipe(delay(100)));
      component.onSubmit();
      expect(component.loading).toBeTruthy();
    });

    it('should set loading to false after successful login', fakeAsync(() => {
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      component.onSubmit();
      tick(100);
      expect(component.loading).toBeFalsy();
    }));

    it('should navigate to return URL after successful login', fakeAsync(() => {
      const returnUrl = '/dashboard';
      component.returnUrl = returnUrl;
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      
      component.onSubmit();
      tick(100);
      
      expect(router.navigate).toHaveBeenCalledWith([returnUrl]);
    }));

    it('should navigate to default URL when no return URL specified', fakeAsync(() => {
      component.returnUrl = '/';
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      
      component.onSubmit();
      tick(100);
      
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    }));

    it('should display error message on login failure', fakeAsync(() => {
      const errorMessage = 'Invalid credentials';
      authService.login.and.returnValue(throwError(() => ({ error: { message: errorMessage } })));
      
      component.onSubmit();
      tick(100);
      
      expect(component.errorMessage).toBe(errorMessage);
      expect(component.loading).toBeFalsy();
    }));

    it('should display generic error message when no specific error provided', fakeAsync(() => {
      authService.login.and.returnValue(throwError(() => ({})));
      
      component.onSubmit();
      tick(100);
      
      expect(component.errorMessage).toBe('An error occurred during login. Please try again.');
      expect(component.loading).toBeFalsy();
    }));

    it('should clear error message when form is resubmitted', () => {
      component.errorMessage = 'Previous error';
      authService.login.and.returnValue(of({ 
        accessToken: 'fake-access-token', 
        refreshToken: 'fake-refresh-token',
        user: { id: '1', email: 'test@example.com', roles: ['user'] }
      }));
      
      component.onSubmit();
      
      expect(component.errorMessage).toBe('');
    });
  });

  describe('UI State', () => {
    it('should disable submit button when form is invalid', () => {
      component.loginForm.patchValue({ email: '', password: '' });
      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
      expect(submitButton.disabled).toBeTruthy();
    });

    it('should enable submit button when form is valid', () => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123'
      });
      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
      expect(submitButton.disabled).toBeFalsy();
    });

    it('should disable submit button when loading', () => {
      component.loginForm.patchValue({
        email: 'test@example.com',
        password: 'password123'
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

    it('should not display error message when empty', () => {
      component.errorMessage = '';
      fixture.detectChanges();
      const errorElement = fixture.nativeElement.querySelector('.error-message');
      expect(errorElement).toBeFalsy();
    });
  });

  describe('Error Handling', () => {
    it('should get error message for required email', () => {
      const emailControl = component.loginForm.get('email');
      emailControl?.setValue('');
      emailControl?.markAsTouched();
      const errorMessage = component.getEmailErrorMessage();
      expect(errorMessage).toBe('Email is required');
    });

    it('should get error message for invalid email format', () => {
      const emailControl = component.loginForm.get('email');
      emailControl?.setValue('invalid');
      emailControl?.markAsTouched();
      const errorMessage = component.getEmailErrorMessage();
      expect(errorMessage).toBe('Please enter a valid email address');
    });

    it('should return empty string for valid email', () => {
      const emailControl = component.loginForm.get('email');
      emailControl?.setValue('test@example.com');
      emailControl?.markAsTouched();
      const errorMessage = component.getEmailErrorMessage();
      expect(errorMessage).toBe('');
    });

    it('should get error message for required password', () => {
      const passwordControl = component.loginForm.get('password');
      passwordControl?.setValue('');
      passwordControl?.markAsTouched();
      const errorMessage = component.getPasswordErrorMessage();
      expect(errorMessage).toBe('Password is required');
    });

    it('should return empty string for valid password', () => {
      const passwordControl = component.loginForm.get('password');
      passwordControl?.setValue('password123');
      passwordControl?.markAsTouched();
      const errorMessage = component.getPasswordErrorMessage();
      expect(errorMessage).toBe('');
    });
  });
});