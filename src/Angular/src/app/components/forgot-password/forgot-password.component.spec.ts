import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { of, throwError, EMPTY } from 'rxjs';
import { delay } from 'rxjs/operators';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../../auth.service';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  // Router is injected but not used in these tests

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['forgotPassword']);
    // Set up default return value for auth service method
    authServiceSpy.forgotPassword.and.returnValue(of({}));
    
    const routerSpy = jasmine.createSpyObj('Router', ['navigate', 'createUrlTree', 'serializeUrl']);
    routerSpy.createUrlTree.and.returnValue(Promise.resolve(true));
    routerSpy.serializeUrl.and.returnValue('/test');
    routerSpy.events = EMPTY;
    const activatedRouteSpy = {
      snapshot: { queryParams: {} },
      queryParams: of({})
    };

    await TestBed.configureTestingModule({
      imports: [
        ForgotPasswordComponent,
        ReactiveFormsModule,
        CommonModule,
        RouterModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteSpy }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    // Router setup handled by TestBed
    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with email control', () => {
    expect(component.forgotPasswordForm.get('email')).toBeDefined();
    expect(component.forgotPasswordForm.get('email')?.value).toBe('');
  });

  it('should validate email format', () => {
    const emailControl = component.forgotPasswordForm.get('email');
    
    emailControl?.setValue('');
    expect(emailControl?.hasError('required')).toBeTruthy();
    
    emailControl?.setValue('invalid-email');
    expect(emailControl?.hasError('email')).toBeTruthy();
    
    emailControl?.setValue('test@example.com');
    expect(emailControl?.valid).toBeTruthy();
  });

  it('should disable submit button when form is invalid', () => {
    component.forgotPasswordForm.get('email')?.setValue('');
    fixture.detectChanges();
    
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton?.disabled).toBeTruthy();
  });

  it('should enable submit button when form is valid', () => {
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    fixture.detectChanges();
    
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton?.disabled).toBeFalsy();
  });

  it('should call authService.forgotPassword on valid form submission', () => {
    authService.forgotPassword.and.returnValue(of({ success: true }));
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    
    expect(authService.forgotPassword).toHaveBeenCalledWith('test@example.com');
  });

  it('should show success message on successful password reset request', () => {
    authService.forgotPassword.and.returnValue(of({ success: true }));
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    fixture.detectChanges();
    
    expect(component.successMessage).toBeTruthy();
    expect(component.errorMessage).toBeFalsy();
  });

  it('should show error message on failed password reset request', () => {
    authService.forgotPassword.and.returnValue(throwError(() => ({ error: { error: 'Email not found' } })));
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    fixture.detectChanges();
    
    expect(component.errorMessage).toBe('Email not found');
    expect(component.successMessage).toBeFalsy();
  });

  it('should set loading state during submission', fakeAsync(() => {
    authService.forgotPassword.and.returnValue(of({ success: true }).pipe(delay(100)));
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    
    expect(component.loading).toBeFalsy();
    
    component.onSubmit();
    expect(component.loading).toBeTruthy();
    
    tick(100);
    fixture.detectChanges();
    expect(component.loading).toBeFalsy();
  }));

  it('should clear error message when form is modified', () => {
    component.errorMessage = 'Previous error';
    component.forgotPasswordForm.get('email')?.setValue('new@example.com');
    fixture.detectChanges();
    
    expect(component.errorMessage).toBeFalsy();
  });

  it('should not submit when already loading', () => {
    authService.forgotPassword.and.returnValue(of({ success: true }));
    component.loading = true;
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    
    expect(authService.forgotPassword).not.toHaveBeenCalled();
  });

  it('should handle rate limiting error', () => {
    authService.forgotPassword.and.returnValue(
      throwError(() => ({ error: { error: 'Too many requests. Please try again later.' } }))
    );
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    fixture.detectChanges();
    
    expect(component.errorMessage).toContain('Too many requests');
  });

  it('should clear form after successful submission', fakeAsync(() => {
    authService.forgotPassword.and.returnValue(of({ success: true }));
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    tick();
    fixture.detectChanges();
    
    expect(component.forgotPasswordForm.get('email')?.value).toBeNull();
  }));

  it('should show loading spinner when loading is true', () => {
    component.loading = true;
    fixture.detectChanges();
    
    const spinner = fixture.nativeElement.querySelector('.spinner');
    expect(spinner).toBeTruthy();
  });

  it('should handle network error gracefully', () => {
    authService.forgotPassword.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    
    component.forgotPasswordForm.get('email')?.setValue('test@example.com');
    component.onSubmit();
    fixture.detectChanges();
    
    expect(component.errorMessage).toContain('An error occurred');
  });
});