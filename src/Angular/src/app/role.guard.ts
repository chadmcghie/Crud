import { Injectable } from '@angular/core';
import { 
  CanActivate, 
  CanActivateChild, 
  CanLoad, 
  CanMatch,
  Router, 
  UrlTree, 
  Route, 
  UrlSegment,
  ActivatedRouteSnapshot,
  RouterStateSnapshot
} from '@angular/router';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate, CanActivateChild, CanLoad, CanMatch {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return this.checkRole(route.data['roles'], state?.url);
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return this.checkRole(childRoute.data['roles'], state?.url);
  }

  canLoad(
    route: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return this.checkRole(route.data?.['roles'], url);
  }

  canMatch(
    route: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return this.checkRole(route.data?.['roles'], url);
  }

  private checkRole(requiredRoles?: string[], returnUrl?: string): boolean | UrlTree {
    // First check if user is authenticated
    if (!this.authService.isAuthenticated()) {
      // Redirect to login page with return URL
      const queryParams = returnUrl ? { returnUrl } : undefined;
      return this.router.createUrlTree(['/login'], { queryParams });
    }

    // If no roles are required, allow access
    if (!requiredRoles || requiredRoles.length === 0) {
      return true;
    }

    // Check if user has at least one of the required roles
    for (const role of requiredRoles) {
      if (this.authService.hasRole(role)) {
        return true;
      }
    }

    // User doesn't have required role, redirect to unauthorized page
    return this.router.createUrlTree(['/unauthorized']);
  }
}