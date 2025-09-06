import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from './auth.service';
import { HttpErrorResponse } from '@angular/common/http';

interface ErrorResponse {
  error?: {
    message?: string;
  };
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  private formBuilder = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  
  loginForm!: FormGroup;
  loading = false;
  errorMessage = '';
  returnUrl = '/';

  ngOnInit(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rememberMe: [false]
    });

    // Get return url from route parameters or default to '/'
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email, password, rememberMe } = this.loginForm.value;

    this.authService.login(email, password, rememberMe).subscribe({
      next: () => {
        this.router.navigate([this.returnUrl]);
        this.loading = false;
      },
      error: (error: HttpErrorResponse & ErrorResponse) => {
        this.loading = false;
        this.errorMessage = error?.error?.message || 'An error occurred during login. Please try again.';
      }
    });
  }

  getEmailErrorMessage(): string {
    const emailControl = this.loginForm.get('email');
    if (emailControl?.hasError('required') && emailControl?.touched) {
      return 'Email is required';
    }
    if (emailControl?.hasError('email') && emailControl?.touched) {
      return 'Please enter a valid email address';
    }
    return '';
  }

  getPasswordErrorMessage(): string {
    const passwordControl = this.loginForm.get('password');
    if (passwordControl?.hasError('required') && passwordControl?.touched) {
      return 'Password is required';
    }
    return '';
  }
}