import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from './auth.service';
import { MarkdownService } from './markdown.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="home-container">
      <!-- Public Homepage for non-authenticated users -->
      <div *ngIf="(authService.currentUser$ | async) === null" class="public-homepage">
        <div class="readme-content" [innerHTML]="readmeContent$ | async">
          <!-- Dynamic README content will be loaded here -->
        </div>

        <div class="auth-actions">
          <h2 class="section-title">Get Started</h2>
          <div class="action-buttons">
            <a routerLink="/login" class="auth-button primary">Sign In</a>
            <a routerLink="/register" class="auth-button secondary">Sign Up</a>
          </div>
        </div>
      </div>

      <!-- Authenticated User Dashboard -->
      <div *ngIf="authService.currentUser$ | async as user" class="user-dashboard">
        <div class="welcome-section">
          <h1 class="welcome-title">Welcome to CRUD Template</h1>
          <p class="welcome-subtitle">Hello, {{ user.email }}!</p>
        </div>

        <div class="quick-actions">
          <h2 class="section-title">Quick Actions</h2>
          <div class="action-cards">
            <div class="action-card">
              <h3 class="card-title">Add Person</h3>
              <p class="card-description">Create a new person record</p>
              <a routerLink="/people" class="card-button">Add Person</a>
            </div>

            <div class="action-card">
              <h3 class="card-title">View People</h3>
              <p class="card-description">Browse all people records</p>
              <a routerLink="/people-list" class="card-button">View People</a>
            </div>

            <div class="action-card">
              <h3 class="card-title">Manage Roles</h3>
              <p class="card-description">Create and manage user roles</p>
              <a routerLink="/roles-list" class="card-button">Manage Roles</a>
            </div>

            <div class="action-card" *ngIf="(authService.currentUser$ | async)?.roles?.includes('Admin')">
              <h3 class="card-title">Site Administration</h3>
              <p class="card-description">Comprehensive site management and administration</p>
              <a routerLink="/site-index" class="card-button admin-button">Admin Panel</a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .home-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    /* Public Homepage Styles */
    .public-homepage {
      background: rgba(255, 255, 255, 0.95);
      color: #333;
      border-radius: 12px;
      padding: 2rem;
      margin-bottom: 2rem;
    }

    .readme-content {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
      line-height: 1.6;
    }

    .readme-content h1 {
      font-size: 2.5rem;
      font-weight: bold;
      color: #2d3748;
      margin-bottom: 1rem;
      border-bottom: 3px solid #4299e1;
      padding-bottom: 0.5rem;
    }

    .readme-content h2 {
      font-size: 1.8rem;
      font-weight: 600;
      color: #2d3748;
      margin-top: 2rem;
      margin-bottom: 1rem;
    }

    .readme-content h3 {
      font-size: 1.4rem;
      font-weight: 600;
      color: #4a5568;
      margin-top: 1.5rem;
      margin-bottom: 0.75rem;
    }

    .readme-content p {
      margin-bottom: 1rem;
      color: #4a5568;
    }

    .readme-content ul, .readme-content ol {
      margin-bottom: 1rem;
      padding-left: 2rem;
      list-style-position: outside;
    }

    .readme-content ul {
      list-style-type: disc !important;
      list-style-position: outside !important;
    }

    .readme-content ol {
      list-style-type: decimal !important;
      list-style-position: outside !important;
    }

    .readme-content li {
      margin-bottom: 0.5rem;
      color: #4a5568;
      display: list-item !important;
      list-style-type: inherit !important;
      margin-left: 0;
      padding-left: 0;
    }

    .readme-content strong {
      font-weight: 600;
      color: #2d3748;
    }

    .auth-actions {
      text-align: center;
      padding: 2rem;
      background: rgba(66, 153, 225, 0.1);
      border-radius: 12px;
      margin-top: 2rem;
    }

    .action-buttons {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }

    .auth-button {
      display: inline-block;
      padding: 0.75rem 2rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
      transition: all 0.3s ease;
      min-width: 120px;
      text-align: center;
    }

    .auth-button.primary {
      background: #4299e1;
      color: white;
      border: 2px solid #4299e1;
    }

    .auth-button.primary:hover {
      background: #3182ce;
      border-color: #3182ce;
    }

    .auth-button.secondary {
      background: transparent;
      color: #4299e1;
      border: 2px solid #4299e1;
    }

    .auth-button.secondary:hover {
      background: #4299e1;
      color: white;
    }

    /* Authenticated User Dashboard Styles */
    .user-dashboard {
      color: white;
    }

    .welcome-section {
      text-align: center;
      margin-bottom: 3rem;
    }

    .welcome-title {
      font-size: 3rem;
      font-weight: bold;
      color: white;
      margin-bottom: 1rem;
      text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
    }

    .welcome-subtitle {
      font-size: 1.25rem;
      color: rgba(255, 255, 255, 0.9);
      margin-bottom: 2rem;
    }

    .section-title {
      font-size: 2rem;
      font-weight: bold;
      color: white;
      margin-bottom: 2rem;
      text-align: center;
    }

    .action-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 2rem;
      margin-top: 2rem;
    }

    .action-card {
      background: rgba(255, 255, 255, 0.1);
      backdrop-filter: blur(10px);
      border: 1px solid rgba(255, 255, 255, 0.2);
      border-radius: 12px;
      padding: 2rem;
      text-align: center;
      transition: all 0.3s ease;
    }

    .action-card:hover {
      transform: translateY(-4px);
      background: rgba(255, 255, 255, 0.15);
      box-shadow: 0 8px 25px rgba(0,0,0,0.2);
    }

    .card-title {
      font-size: 1.5rem;
      font-weight: bold;
      color: white;
      margin-bottom: 0.5rem;
    }

    .card-description {
      color: rgba(255, 255, 255, 0.7);
      margin-bottom: 1.5rem;
      font-size: 0.9rem;
    }

    .card-button {
      display: inline-block;
      background: rgba(255, 255, 255, 0.2);
      color: white;
      padding: 0.75rem 1.5rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 500;
      transition: all 0.3s ease;
      border: 1px solid rgba(255, 255, 255, 0.3);
    }

    .card-button:hover {
      background: rgba(255, 255, 255, 0.3);
      transform: translateY(-2px);
    }

    .admin-button {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
      border: 1px solid rgba(102, 126, 234, 0.5) !important;
    }

    .admin-button:hover {
      background: linear-gradient(135deg, #5a6fd8 0%, #6a4190 100%) !important;
      box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
    }

    @media (max-width: 768px) {
      .home-container {
        padding: 1rem;
      }

      .welcome-title {
        font-size: 2rem;
      }

      .action-cards {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .action-buttons {
        flex-direction: column;
        align-items: center;
      }
    }
  `]
})
export class HomeComponent implements OnInit {
  authService = inject(AuthService);
  markdownService = inject(MarkdownService);

  readmeContent$: Observable<string>;

  constructor() {
    this.readmeContent$ = this.markdownService.getReadmeContent();
  }

  ngOnInit() {
    // Debug: Log current user and roles
    this.authService.currentUser$.subscribe(user => {
      console.log('HomeComponent - Current user:', user);
      console.log('HomeComponent - User roles:', user?.roles);
      console.log('HomeComponent - Has Admin role:', user?.roles?.includes('Admin'));
    });
  }
}