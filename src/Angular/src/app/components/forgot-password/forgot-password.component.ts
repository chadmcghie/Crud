import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { HttpErrorResponse } from '@angular/common/http';

interface ErrorResponse {
  error?: {
    error?: string;
  };
}

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  
  forgotPasswordForm!: FormGroup;
  loading = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.forgotPasswordForm.valueChanges.subscribe(() => {
      this.errorMessage = '';
    });
  }

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid || this.loading) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const email = this.forgotPasswordForm.get('email')?.value;

    this.authService.forgotPassword(email).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = 'Password reset instructions have been sent to your email address.';
        this.forgotPasswordForm.reset();
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

  get emailControl() {
    return this.forgotPasswordForm.get('email');
  }
}