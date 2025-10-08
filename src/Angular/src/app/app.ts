import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, 
    HttpClientModule, 
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  template: `
    <div class="app-container">
      <header class="app-header">
        <h1>CRUD Template Application</h1>
        <nav class="nav-links">

          <!-- Show when not authenticated -->
          <ng-container *ngIf="(authService.currentUser$ | async) === null">
            <a routerLink="/login" routerLinkActive="active">Login</a>
          </ng-container>

          <!-- Show when authenticated -->
          <ng-container *ngIf="authService.currentUser$ | async as user">
            <a routerLink="/home" routerLinkActive="active">Home</a>
            <a routerLink="/people" routerLinkActive="active">Add Person</a>
            <a routerLink="/people-list" routerLinkActive="active">People List</a>
            <a routerLink="/roles-list" routerLinkActive="active">Roles</a>

            <div class="user-profile">
              <div class="flex items-center gap-2 text-white/80">
                <div class="profile-icon">
                  <svg class="h-6 w-6" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
                  </svg>
                </div>
                <span class="text-sm">{{ user.email }}</span>
                <button (click)="logout()" class="logout-btn text-sm hover:text-white transition-colors">
                  Logout
                </button>
              </div>
            </div>
          </ng-container>
        </nav>
      </header>

      <main class="main-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    .app-header {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 1000;
      background: rgba(255, 255, 255, 0.1);
      backdrop-filter: blur(10px);
      color: white;
      padding: 1rem 2rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
      box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    }

    .app-header h1 {
      margin: 0;
      font-size: 1.5rem;
      font-weight: 600;
      text-shadow: 1px 1px 2px rgba(0,0,0,0.2);
    }

    .nav-links {
      display: flex;
      gap: 1.5rem;
    }

    .nav-links a {
      color: rgba(255, 255, 255, 0.8);
      text-decoration: none;
      padding: 0.5rem 1rem;
      border-radius: 4px;
      transition: all 0.3s;
    }

    .nav-links a:hover {
      background: rgba(255, 255, 255, 0.15);
      color: white;
    }

    .nav-links a.active {
      background: rgba(255, 255, 255, 0.2);
      color: white;
    }

    .user-profile {
      margin-left: 1rem;
      padding-left: 1rem;
      border-left: 1px solid rgba(255, 255, 255, 0.2);
    }

    .profile-icon {
      background: rgba(255, 255, 255, 0.1);
      border-radius: 50%;
      padding: 0.25rem;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      flex-shrink: 0;
    }

    .profile-icon svg {
      width: 18px;
      height: 18px;
    }

    .logout-btn {
      background: rgba(255, 255, 255, 0.1);
      border: 1px solid rgba(255, 255, 255, 0.2);
      padding: 0.25rem 0.75rem;
      border-radius: 4px;
      cursor: pointer;
      transition: all 0.3s;
    }

    .logout-btn:hover {
      background: rgba(255, 255, 255, 0.2);
    }

    .main-content {
      padding: 2rem;
      padding-top: 6rem; /* Add space for fixed header */
      max-width: 1400px;
      margin: 0 auto;
    }

    .tab-container {
      max-width: 1400px;
      margin: 0 auto;
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 10px 30px rgba(0,0,0,0.3);
    }

    .tab-buttons {
      display: flex;
      background: #f8f9fa;
      border-bottom: 1px solid #e9ecef;
    }

    .tab-button {
      flex: 1;
      padding: 16px 24px;
      border: none;
      background: transparent;
      cursor: pointer;
      font-size: 16px;
      font-weight: 500;
      transition: all 0.3s;
      border-bottom: 3px solid transparent;
    }

    .tab-button:hover {
      background: #e9ecef;
    }

    .tab-button.active {
      background: white;
      color: #007bff;
      border-bottom-color: #007bff;
    }

    .tab-content {
      padding: 30px;
      min-height: 70vh;
    }

    .management-layout {
      display: grid;
      grid-template-columns: 1fr auto;
      gap: 30px;
      align-items: start;
    }

    .list-section {
      min-width: 0; /* Allows grid item to shrink */
    }

    .form-section {
      width: 600px;
      position: sticky;
      top: 30px;
    }

    /* Responsive design */
    @media (max-width: 1200px) {
      .management-layout {
        grid-template-columns: 1fr;
        gap: 20px;
      }

      .form-section {
        width: 100%;
        position: static;
        max-width: 600px;
        margin: 0 auto;
      }
    }

    @media (max-width: 768px) {
      .app-container {
        padding: 10px;
      }

      .app-header h1 {
        font-size: 2rem;
      }

      .tab-content {
        padding: 20px 15px;
      }

      .tab-button {
        font-size: 14px;
        padding: 12px 16px;
      }
    }
  `]
})
export class App {
  title = 'CRUD Template Application';

  authService = inject(AuthService);
  private router = inject(Router);

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/']); // Go to public homepage
      },
      error: (error) => {
        console.error('Logout error:', error);
        // Even if logout fails on server, clear local state
        this.router.navigate(['/']); // Go to public homepage
      }
    });
  }
}
