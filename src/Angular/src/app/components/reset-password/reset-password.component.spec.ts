import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from '../../auth.service';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let activatedRoute: any;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['resetPassword', 'validateResetToken']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    
    activatedRoute = {
      queryParams: of({ token: 'valid-token' })
    };

    await TestBed.configureTestingModule({
      imports: [
        ResetPasswordComponent,
        ReactiveFormsModule,
        CommonModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRoute }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with password fields', () => {
    expect(component.resetPasswordForm.get('password')).toBeDefined();
    expect(component.resetPasswordForm.get('confirmPassword')).toBeDefined();
  });

  it('should extract token from query parameters', () => {
    expect(component.token).toBe('valid-token');
  });

  it('should validate password requirements', () => {
    const passwordControl = component.resetPasswordForm.get('password');
    
    passwordControl?.setValue('');
    expect(passwordControl?.hasError('required')).toBeTruthy();
    
    passwordControl?.setValue('short');
    expect(passwordControl?.hasError('minlength')).toBeTruthy();
    
    passwordControl?.setValue('ValidPassword123!');
    expect(passwordControl?.valid).toBeTruthy();
  });

  it('should validate password confirmation match', () => {
    component.resetPasswordForm.get('password')?.setValue('Password123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('DifferentPassword123!');
    
    expect(component.resetPasswordForm.hasError('passwordMismatch')).toBeTruthy();
    
    component.resetPasswordForm.get('confirmPassword')?.setValue('Password123!');
    expect(component.resetPasswordForm.hasError('passwordMismatch')).toBeFalsy();
  });

  it('should show error if no token is provided', () => {
    component.token = '';
    fixture.detectChanges();
    
    expect(component.errorMessage).toContain('Invalid or missing reset token');
  });

  it('should call authService.resetPassword on valid form submission', () => {
    authService.resetPassword.and.returnValue(of({ success: true }));
    
    component.token = 'valid-token';
    component.resetPasswordForm.get('password')?.setValue('NewPassword123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('NewPassword123!');
    
    component.onSubmit();
    
    expect(authService.resetPassword).toHaveBeenCalledWith('valid-token', 'NewPassword123!');
  });

  it('should navigate to login on successful password reset', () => {
    authService.resetPassword.and.returnValue(of({ success: true }));
    
    component.token = 'valid-token';
    component.resetPasswordForm.get('password')?.setValue('NewPassword123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('NewPassword123!');
    
    component.onSubmit();
    fixture.detectChanges();
    
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { 
      queryParams: { message: 'Password reset successful. Please login with your new password.' }
    });
  });

  it('should show error message on failed password reset', () => {
    authService.resetPassword.and.returnValue(
      throwError(() => ({ error: { error: 'Token expired' } }))
    );
    
    component.token = 'expired-token';
    component.resetPasswordForm.get('password')?.setValue('NewPassword123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('NewPassword123!');
    
    component.onSubmit();
    fixture.detectChanges();
    
    expect(component.errorMessage).toBe('Token expired');
  });

  it('should calculate password strength', () => {
    component.resetPasswordForm.get('password')?.setValue('weak');
    expect(component.passwordStrength).toBe('weak');
    
    component.resetPasswordForm.get('password')?.setValue('Medium123');
    expect(component.passwordStrength).toBe('medium');
    
    component.resetPasswordForm.get('password')?.setValue('Strong123!@#');
    expect(component.passwordStrength).toBe('strong');
  });

  it('should show password strength indicator', () => {
    component.resetPasswordForm.get('password')?.setValue('StrongPassword123!');
    fixture.detectChanges();
    
    const strengthIndicator = fixture.nativeElement.querySelector('.password-strength');
    expect(strengthIndicator).toBeTruthy();
  });

  it('should set loading state during submission', () => {
    authService.resetPassword.and.returnValue(of({ success: true }));
    
    component.token = 'valid-token';
    component.resetPasswordForm.get('password')?.setValue('NewPassword123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('NewPassword123!');
    
    expect(component.loading).toBeFalsy();
    
    component.onSubmit();
    expect(component.loading).toBeTruthy();
    
    fixture.detectChanges();
    expect(component.loading).toBeFalsy();
  });

  it('should disable submit button when form is invalid', () => {
    component.resetPasswordForm.get('password')?.setValue('');
    fixture.detectChanges();
    
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton?.disabled).toBeTruthy();
  });

  it('should enable submit button when form is valid', () => {
    component.token = 'valid-token';
    component.resetPasswordForm.get('password')?.setValue('ValidPassword123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('ValidPassword123!');
    fixture.detectChanges();
    
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(submitButton?.disabled).toBeFalsy();
  });

  it('should validate token on component initialization', () => {
    authService.validateResetToken.and.returnValue(of({ valid: true }));
    
    component.ngOnInit();
    
    expect(authService.validateResetToken).toHaveBeenCalledWith('valid-token');
  });

  it('should show error for invalid token', () => {
    authService.validateResetToken.and.returnValue(
      throwError(() => ({ error: { error: 'Invalid token' } }))
    );
    
    component.ngOnInit();
    fixture.detectChanges();
    
    expect(component.errorMessage).toContain('Invalid token');
    expect(component.tokenValid).toBeFalsy();
  });

  it('should toggle password visibility', () => {
    expect(component.showPassword).toBeFalsy();
    
    component.togglePasswordVisibility();
    expect(component.showPassword).toBeTruthy();
    
    component.togglePasswordVisibility();
    expect(component.showPassword).toBeFalsy();
  });

  it('should handle network error gracefully', () => {
    authService.resetPassword.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    
    component.token = 'valid-token';
    component.resetPasswordForm.get('password')?.setValue('NewPassword123!');
    component.resetPasswordForm.get('confirmPassword')?.setValue('NewPassword123!');
    
    component.onSubmit();
    fixture.detectChanges();
    
    expect(component.errorMessage).toContain('An error occurred');
  });
});