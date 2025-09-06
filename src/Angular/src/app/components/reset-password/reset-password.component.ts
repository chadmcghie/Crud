import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { HttpErrorResponse } from '@angular/common/http';

interface ErrorResponse {
  error?: {
    error?: string;
  };
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  
  resetPasswordForm!: FormGroup;
  loading = false;
  errorMessage = '';
  token = '';
  tokenValid = true;
  showPassword = false;
  showConfirmPassword = false;

  ngOnInit(): void {
    this.resetPasswordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8), this.passwordStrengthValidator]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    this.route.queryParams.subscribe(params => {
      this.token = params['token'] || '';
      if (!this.token) {
        this.errorMessage = 'Invalid or missing reset token';
        this.tokenValid = false;
      } else {
        this.validateToken();
      }
    });
  }

  validateToken(): void {
    this.authService.validateResetToken(this.token).subscribe({
      next: () => {
        this.tokenValid = true;
      },
      error: (error: HttpErrorResponse & ErrorResponse) => {
        this.tokenValid = false;
        this.errorMessage = error.error?.error || 'Invalid or expired reset token';
      }
    });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');
    
    if (!password || !confirmPassword) {
      return null;
    }
    
    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  }

  passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) {
      return null;
    }

    const hasNumber = /[0-9]/.test(value);
    const hasUpper = /[A-Z]/.test(value);
    const hasLower = /[a-z]/.test(value);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const valid = hasNumber && hasUpper && hasLower && hasSpecial;
    
    if (!valid) {
      return { weakPassword: true };
    }
    
    return null;
  }

  get passwordStrength(): string {
    const password = this.resetPasswordForm.get('password')?.value || '';
    
    if (password.length < 6) {
      return 'weak';
    }
    
    let strength = 0;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) strength++;
    if (password.length >= 12) strength++;
    
    if (strength <= 2) return 'weak';
    if (strength <= 3) return 'medium';
    return 'strong';
  }

  onSubmit(): void {
    if (this.resetPasswordForm.invalid || this.loading || !this.tokenValid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const password = this.resetPasswordForm.get('password')?.value;

    this.authService.resetPassword(this.token, password).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/login'], {
          queryParams: { message: 'Password reset successful. Please login with your new password.' }
        });
      },
      error: (error: HttpErrorResponse & ErrorResponse) => {
        this.loading = false;
        if (error.error?.error) {
          this.errorMessage = error.error.error;
        } else {
          this.errorMessage = 'An error occurred. Please try again later.';
        }
      }
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  get passwordControl() {
    return this.resetPasswordForm.get('password');
  }

  get confirmPasswordControl() {
    return this.resetPasswordForm.get('confirmPassword');
  }

  hasMinLength(): boolean {
    const password = this.passwordControl?.value || '';
    return password.length >= 8;
  }

  hasUppercase(): boolean {
    const password = this.passwordControl?.value || '';
    return /[A-Z]/.test(password);
  }

  hasLowercase(): boolean {
    const password = this.passwordControl?.value || '';
    return /[a-z]/.test(password);
  }

  hasNumber(): boolean {
    const password = this.passwordControl?.value || '';
    return /[0-9]/.test(password);
  }

  hasSpecialChar(): boolean {
    const password = this.passwordControl?.value || '';
    return /[!@#$%^&*(),.?":{}|<>]/.test(password);
  }
}