import { inject } from '@angular/core';
import { 
  CanActivateFn,
  CanActivateChildFn,
  CanLoadFn,
  CanMatchFn,
  Router, 
  UrlTree, 
  Route, 
  UrlSegment,
  ActivatedRouteSnapshot,
  RouterStateSnapshot
} from '@angular/router';
import { AuthService } from './auth.service';

function checkRole(authService: AuthService, router: Router, requiredRoles?: string[], returnUrl?: string): boolean | UrlTree {
  // First check if user is authenticated
  if (!authService.isAuthenticated()) {
    // Redirect to login page with return URL
    const queryParams = returnUrl ? { returnUrl } : undefined;
    return router.createUrlTree(['/login'], { queryParams });
  }

  // If no roles are required, allow access
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  // Check if user has at least one of the required roles
  for (const role of requiredRoles) {
    if (authService.hasRole(role)) {
      return true;
    }
  }

  // User doesn't have required role, redirect to unauthorized page
  return router.createUrlTree(['/unauthorized']);
}

export const canActivateRole: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state?: RouterStateSnapshot
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return checkRole(authService, router, route.data['roles'] as string[], state?.url);
};

export const canActivateChildRole: CanActivateChildFn = (
  childRoute: ActivatedRouteSnapshot,
  state?: RouterStateSnapshot
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return checkRole(authService, router, childRoute.data['roles'] as string[], state?.url);
};

export const canLoadRole: CanLoadFn = (
  route: Route,
  segments?: UrlSegment[]
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
  return checkRole(authService, router, route.data?.['roles'] as string[], url);
};

export const canMatchRole: CanMatchFn = (
  route: Route,
  segments?: UrlSegment[]
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
  return checkRole(authService, router, route.data?.['roles'] as string[], url);
};

// Legacy class-based guard for backward compatibility
export class RoleGuard {
  private authService = inject(AuthService);
  private router = inject(Router);

  canActivate(
    route: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return checkRole(this.authService, this.router, route.data['roles'] as string[], state?.url);
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return checkRole(this.authService, this.router, childRoute.data['roles'] as string[], state?.url);
  }

  canLoad(
    route: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return checkRole(this.authService, this.router, route.data?.['roles'] as string[], url);
  }

  canMatch(
    route: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return checkRole(this.authService, this.router, route.data?.['roles'] as string[], url);
  }
}