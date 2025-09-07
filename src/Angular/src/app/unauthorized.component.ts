import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="unauthorized-container">
      <div class="unauthorized-card">
        <div class="unauthorized-icon">
          <svg xmlns="http://www.w3.org/2000/svg" width="80" height="80" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
        </div>
        <h1 class="unauthorized-title">Access Denied</h1>
        <p class="unauthorized-message">
          Sorry, you don't have permission to access this page. 
          You may need to sign in with a different account or contact your administrator for access.
        </p>
        <div class="unauthorized-actions">
          <button class="btn-back" (click)="goBack()">Go Back</button>
          <a routerLink="/login" class="btn-login">Sign In</a>
          <a routerLink="/" class="btn-home">Go to Home</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .unauthorized-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
    }

    .unauthorized-card {
      background: white;
      border-radius: 12px;
      padding: 48px;
      max-width: 500px;
      width: 100%;
      text-align: center;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
    }

    .unauthorized-icon {
      color: #ef4444;
      margin-bottom: 24px;
      display: flex;
      justify-content: center;
    }

    .unauthorized-title {
      font-size: 2rem;
      font-weight: 700;
      color: #1f2937;
      margin: 0 0 16px 0;
    }

    .unauthorized-message {
      color: #6b7280;
      font-size: 1rem;
      line-height: 1.5;
      margin: 0 0 32px 0;
    }

    .unauthorized-actions {
      display: flex;
      gap: 12px;
      justify-content: center;
      flex-wrap: wrap;
    }

    .btn-back,
    .btn-login,
    .btn-home {
      padding: 10px 20px;
      border-radius: 6px;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s;
      cursor: pointer;
      border: none;
      font-size: 14px;
    }

    .btn-back {
      background: #f3f4f6;
      color: #4b5563;
    }

    .btn-back:hover {
      background: #e5e7eb;
    }

    .btn-login {
      background: #667eea;
      color: white;
    }

    .btn-login:hover {
      background: #5a67d8;
    }

    .btn-home {
      background: #10b981;
      color: white;
    }

    .btn-home:hover {
      background: #059669;
    }

    @media (max-width: 640px) {
      .unauthorized-card {
        padding: 32px 24px;
      }

      .unauthorized-title {
        font-size: 1.5rem;
      }

      .unauthorized-actions {
        flex-direction: column;
      }

      .btn-back,
      .btn-login,
      .btn-home {
        width: 100%;
      }
    }
  `]
})
export class UnauthorizedComponent {
  private router = inject(Router);

  goBack(): void {
    window.history.back();
  }
}