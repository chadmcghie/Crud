import { Routes } from '@angular/router';
import { LoginComponent } from './login.component';
import { RegisterComponent } from './register.component';
import { HomeComponent } from './home.component';
import { PeopleComponent } from './people.component';
import { PeopleListComponent } from './people-list.component';
import { RolesComponent } from './roles.component';
import { RolesListComponent } from './roles-list.component';
import { SiteIndexComponent } from './site-index.component';
import { canActivateGuard } from './auth.guard';
import { canActivateAdmin } from './admin.guard';

export const routes: Routes = [
  // Public routes
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { 
    path: 'forgot-password', 
    loadComponent: () => import('./components/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  { 
    path: 'reset-password', 
    loadComponent: () => import('./components/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },

  // Home route - public, handles auth state internally
  {
    path: 'home',
    component: HomeComponent
  },

  // Protected routes
  {
    path: 'people',
    component: PeopleComponent,
    canActivate: [canActivateGuard]
  },
  {
    path: 'people-list',
    component: PeopleListComponent,
    canActivate: [canActivateGuard]
  },
  
  // Role management routes - temporarily accessible to all authenticated users
  {
    path: 'roles',
    component: RolesComponent,
    canActivate: [canActivateGuard]
  },
  {
    path: 'roles-list',
    component: RolesListComponent,
    canActivate: [canActivateGuard]
  },

  // Admin-only routes
  {
    path: 'site-index',
    component: SiteIndexComponent,
    canActivate: [canActivateAdmin]
  },

  // Unauthorized page (lazy loaded)
  {
    path: 'unauthorized',
    loadComponent: () => import('./unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  
  // Default route - always redirect to home (handles auth state internally)
  { path: '', component: HomeComponent },

  // Wildcard route - must be last! Catches all unmatched routes
  { path: '**', component: HomeComponent }
];