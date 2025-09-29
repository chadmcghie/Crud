import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from './auth.service';

interface SiteRoute {
  path: string;
  title: string;
  description?: string;
  children?: SiteRoute[];
  requiresAuth?: boolean;
  requiresAdmin?: boolean;
  isLazyLoaded?: boolean;
}

@Component({
  selector: 'app-site-index',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="site-index-container">
      <div class="header-section">
        <h1>Site Index</h1>
        <p class="subtitle">Complete navigation tree of all pages in the application</p>
      </div>

      <div class="site-tree">
        <div class="tree-section">
          <h2 class="section-title">📋 Application Pages</h2>
          <div class="tree-container">
            <ul class="tree-root">
              <li *ngFor="let route of siteRoutes" class="tree-node">
                <div class="node-content" [class.auth-required]="route.requiresAuth" [class.admin-required]="route.requiresAdmin">
                  <span class="node-icon">📄</span>
                  <a [routerLink]="route.path" class="node-link" (click)="navigateToRoute(route.path, $event)">
                    {{ route.title }}
                  </a>
                  <span *ngIf="route.requiresAuth" class="auth-badge" title="Requires Authentication">🔒</span>
                  <span *ngIf="route.requiresAdmin" class="admin-badge" title="Admin Only">👑</span>
                  <span *ngIf="route.isLazyLoaded" class="lazy-badge" title="Lazy Loaded">⚡</span>
                </div>
                <div *ngIf="route.description" class="node-description">{{ route.description }}</div>

                <!-- Child routes -->
                <ul *ngIf="route.children && route.children.length > 0" class="tree-children">
                  <li *ngFor="let child of route.children" class="tree-node child-node">
                    <div class="node-content" [class.auth-required]="child.requiresAuth" [class.admin-required]="child.requiresAdmin">
                      <span class="node-icon">📄</span>
                      <a [routerLink]="child.path" class="node-link" (click)="navigateToRoute(child.path, $event)">
                        {{ child.title }}
                      </a>
                      <span *ngIf="child.requiresAuth" class="auth-badge" title="Requires Authentication">🔒</span>
                      <span *ngIf="child.requiresAdmin" class="admin-badge" title="Admin Only">👑</span>
                      <span *ngIf="child.isLazyLoaded" class="lazy-badge" title="Lazy Loaded">⚡</span>
                    </div>
                    <div *ngIf="child.description" class="node-description">{{ child.description }}</div>
                  </li>
                </ul>
              </li>
            </ul>
          </div>
        </div>

        <div class="tree-legend">
          <h3>Legend</h3>
          <div class="legend-items">
            <div class="legend-item">
              <span class="node-icon">📄</span>
              <span>Page/Route</span>
            </div>
            <div class="legend-item">
              <span class="auth-badge">🔒</span>
              <span>Requires Authentication</span>
            </div>
            <div class="legend-item">
              <span class="admin-badge">👑</span>
              <span>Admin Only</span>
            </div>
            <div class="legend-item">
              <span class="lazy-badge">⚡</span>
              <span>Lazy Loaded</span>
            </div>
          </div>
        </div>
      </div>

      <div class="current-user-info">
        <h3>Current Session</h3>
        <div class="user-details">
          <div class="detail-item">
            <strong>User:</strong> {{ (authService.currentUser$ | async)?.email || 'Not logged in' }}
          </div>
          <div class="detail-item">
            <strong>Roles:</strong> {{ (authService.currentUser$ | async)?.roles?.join(', ') || 'None' }}
          </div>
          <div class="detail-item">
            <strong>Can Access Admin:</strong>
            <span [class.access-yes]="(authService.currentUser$ | async)?.roles?.includes('Admin')"
                  [class.access-no]="!(authService.currentUser$ | async)?.roles?.includes('Admin')">
              {{ (authService.currentUser$ | async)?.roles?.includes('Admin') ? 'Yes' : 'No' }}
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .site-index-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 2rem;
      background: rgba(255, 255, 255, 0.95);
      color: #2d3748;
      border-radius: 12px;
    }

    .header-section {
      text-align: center;
      margin-bottom: 3rem;
      padding-bottom: 2rem;
      border-bottom: 3px solid #4299e1;
    }

    .header-section h1 {
      font-size: 3rem;
      font-weight: bold;
      color: #2d3748;
      margin-bottom: 1rem;
    }

    .subtitle {
      font-size: 1.25rem;
      color: #4a5568;
    }

    .site-tree {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 2rem;
      margin-bottom: 2rem;
    }

    .tree-section {
      background: #f7fafc;
      border-radius: 12px;
      padding: 1.5rem;
      border: 1px solid #e2e8f0;
    }

    .section-title {
      font-size: 1.5rem;
      font-weight: 600;
      color: #2d3748;
      margin-bottom: 1rem;
    }

    .tree-container {
      background: white;
      border-radius: 8px;
      padding: 1rem;
      max-height: 600px;
      overflow-y: auto;
    }

    .tree-root {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .tree-node {
      margin-bottom: 0.5rem;
    }

    .tree-children {
      list-style: none;
      margin-left: 2rem;
      margin-top: 0.5rem;
      border-left: 2px solid #e2e8f0;
      padding-left: 1rem;
    }

    .child-node {
      margin-bottom: 0.25rem;
    }

    .node-content {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.25rem 0;
    }

    .node-icon {
      font-size: 0.9rem;
    }

    .node-link {
      color: #4299e1;
      text-decoration: none;
      font-weight: 500;
      transition: color 0.2s;
    }

    .node-link:hover {
      color: #3182ce;
      text-decoration: underline;
    }

    .node-content.auth-required .node-link {
      color: #ed8936;
    }

    .node-content.admin-required .node-link {
      color: #9f7aea;
      font-weight: 600;
    }

    .node-description {
      margin-left: 2rem;
      font-size: 0.8rem;
      color: #718096;
      font-style: italic;
    }

    .auth-badge, .admin-badge, .lazy-badge {
      font-size: 0.8rem;
      padding: 0.1rem 0.3rem;
      border-radius: 3px;
      font-weight: bold;
    }

    .auth-badge {
      background: #fed7d7;
      color: #c53030;
    }

    .admin-badge {
      background: #e9d5ff;
      color: #7c3aed;
    }

    .lazy-badge {
      background: #d1fae5;
      color: #047857;
    }

    .tree-legend {
      background: #f7fafc;
      border-radius: 12px;
      padding: 1.5rem;
      border: 1px solid #e2e8f0;
      height: fit-content;
    }

    .tree-legend h3 {
      font-size: 1.2rem;
      font-weight: 600;
      color: #2d3748;
      margin-bottom: 1rem;
    }

    .legend-items {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .legend-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.9rem;
      color: #4a5568;
    }

    .current-user-info {
      background: #f7fafc;
      border-radius: 12px;
      padding: 1.5rem;
      border: 1px solid #e2e8f0;
    }

    .current-user-info h3 {
      font-size: 1.2rem;
      font-weight: 600;
      color: #2d3748;
      margin-bottom: 1rem;
    }

    .user-details {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .detail-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.9rem;
    }

    .detail-item strong {
      color: #2d3748;
      min-width: 120px;
    }

    .access-yes {
      color: #38a169;
      font-weight: 600;
    }

    .access-no {
      color: #e53e3e;
      font-weight: 600;
    }

    @media (max-width: 1024px) {
      .site-tree {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .site-index-container {
        padding: 1rem;
      }

      .header-section h1 {
        font-size: 2rem;
      }

      .tree-container {
        max-height: 400px;
      }
    }
  `]
})
export class SiteIndexComponent {
  authService = inject(AuthService);
  private router = inject(Router);

  siteRoutes: SiteRoute[] = [
    {
      path: '/',
      title: 'Home',
      description: 'Public homepage with authentication-aware content'
    },
    {
      path: '/login',
      title: 'Login',
      description: 'User authentication page'
    },
    {
      path: '/register',
      title: 'Register',
      description: 'User registration page'
    },
    {
      path: '/forgot-password',
      title: 'Forgot Password',
      description: 'Password reset request form',
      isLazyLoaded: true
    },
    {
      path: '/reset-password',
      title: 'Reset Password',
      description: 'Password reset form with token validation',
      isLazyLoaded: true
    },
    {
      path: '/unauthorized',
      title: 'Unauthorized',
      description: 'Access denied page',
      isLazyLoaded: true
    },
    {
      path: '/people',
      title: 'People Management',
      description: 'User management section',
      requiresAuth: true,
      children: [
        {
          path: '/people',
          title: 'Add Person',
          description: 'Create new person records',
          requiresAuth: true
        },
        {
          path: '/people-list',
          title: 'People List',
          description: 'View and manage all people',
          requiresAuth: true
        }
      ]
    },
    {
      path: '/roles',
      title: 'Role Management',
      description: 'Role and permission management',
      requiresAuth: true,
      children: [
        {
          path: '/roles',
          title: 'Add Role',
          description: 'Create new roles',
          requiresAuth: true
        },
        {
          path: '/roles-list',
          title: 'Roles List',
          description: 'View and manage all roles',
          requiresAuth: true
        }
      ]
    },
    {
      path: '/site-index',
      title: 'Site Index',
      description: 'Complete site navigation and administration',
      requiresAuth: true,
      requiresAdmin: true
    }
  ];

  navigateToRoute(path: string, event: MouseEvent) {
    // Allow ctrl+click to open in new tab
    if (event.ctrlKey || event.metaKey) {
      event.preventDefault();
      window.open(path, '_blank');
    }
    // Otherwise, normal navigation will happen via routerLink
  }
}