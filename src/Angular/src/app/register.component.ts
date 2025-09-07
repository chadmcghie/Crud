import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ValidatorFn, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from './auth.service';
import { HttpErrorResponse } from '@angular/common/http';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: {
    id: string;
    email: string;
    roles: string[];
  };
}

interface ErrorResponse {
  error?: {
    message?: string;
  };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  private formBuilder = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  
  registerForm!: FormGroup;
  loading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      acceptTerms: [false, Validators.requiredTrue]
    }, { validators: this.passwordMatchValidator() });
  }

  passwordMatchValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.get('password');
      const confirmPassword = control.get('confirmPassword');
      
      if (!password || !confirmPassword) {
        return null;
      }
      
      if (password.value !== confirmPassword.value) {
        return { passwordMismatch: true };
      }
      
      return null;
    };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const { email, password, confirmPassword } = this.registerForm.value;

    this.authService.register(email, password, confirmPassword).subscribe({
      next: (_response: AuthResponse) => {
        this.successMessage = 'Registration successful! Logging you in...';
        this.loading = false;
        
        // Auto-login after successful registration
        setTimeout(() => {
          this.authService.login(email, password, false).subscribe({
            next: () => {
              this.router.navigate(['/']);
            },
            error: (_error: HttpErrorResponse & ErrorResponse) => {
              this.errorMessage = 'Registration successful but login failed. Please try logging in manually.';
              setTimeout(() => {
                this.router.navigate(['/login']);
              }, 2000);
            }
          });
        }, 1000); // Give user time to see success message
      },
      error: (error: HttpErrorResponse & ErrorResponse) => {
        this.loading = false;
        this.errorMessage = error?.error?.message || 'An error occurred during registration. Please try again.';
      }
    });
  }

  getEmailErrorMessage(): string {
    const emailControl = this.registerForm.get('email');
    if (emailControl?.hasError('required') && emailControl?.touched) {
      return 'Email is required';
    }
    if (emailControl?.hasError('email') && emailControl?.touched) {
      return 'Please enter a valid email address';
    }
    return '';
  }

  getPasswordErrorMessage(): string {
    const passwordControl = this.registerForm.get('password');
    if (passwordControl?.hasError('required') && passwordControl?.touched) {
      return 'Password is required';
    }
    if (passwordControl?.hasError('minlength') && passwordControl?.touched) {
      return 'Password must be at least 6 characters';
    }
    return '';
  }

  getConfirmPasswordErrorMessage(): string {
    const confirmPasswordControl = this.registerForm.get('confirmPassword');
    if (confirmPasswordControl?.hasError('required') && confirmPasswordControl?.touched) {
      return 'Please confirm your password';
    }
    if (this.registerForm.hasError('passwordMismatch') && confirmPasswordControl?.touched) {
      return 'Passwords do not match';
    }
    return '';
  }

  getPasswordStrength(): string {
    const password = this.registerForm.get('password')?.value || '';
    
    if (!password) {
      return '';
    }
    
    let strength = 0;
    
    // Length check
    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;
    
    // Character variety checks
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;
    
    if (strength <= 2) return 'weak';
    if (strength <= 4) return 'medium';
    return 'strong';
  }
}