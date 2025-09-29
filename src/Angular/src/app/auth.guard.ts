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
import { TestAuthService } from './test-auth.service';

function checkAuth(authService: AuthService, router: Router, testAuthService: TestAuthService, returnUrl?: string): boolean | UrlTree {
  // Allow bypass for E2E tests in non-production environments
  if (testAuthService.isE2ETestMode()) {
    return true;
  }

  if (authService.isAuthenticated()) {
    return true;
  }

  // Redirect to login page with return URL
  const queryParams = returnUrl ? { returnUrl } : undefined;
  return router.createUrlTree(['/login'], { queryParams });
}

export const canActivateGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const testAuthService = inject(TestAuthService);
  return checkAuth(authService, router, testAuthService, state?.url);
};

export const canActivateChildGuard: CanActivateChildFn = (
  _childRoute: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const testAuthService = inject(TestAuthService);
  return checkAuth(authService, router, testAuthService, state?.url);
};

export const canLoadGuard: CanLoadFn = (
  _route: Route,
  segments: UrlSegment[]
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const testAuthService = inject(TestAuthService);
  const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
  return checkAuth(authService, router, testAuthService, url);
};

export const canMatchGuard: CanMatchFn = (
  _route: Route,
  segments: UrlSegment[]
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const testAuthService = inject(TestAuthService);
  const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
  return checkAuth(authService, router, testAuthService, url);
};

// Legacy class-based guard for backward compatibility
export class AuthGuard {
  private authService = inject(AuthService);
  private router = inject(Router);
  private testAuthService = inject(TestAuthService);

  canActivate(
    _route?: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return checkAuth(this.authService, this.router, this.testAuthService, state?.url);
  }

  canActivateChild(
    _childRoute?: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return checkAuth(this.authService, this.router, this.testAuthService, state?.url);
  }

  canLoad(
    _route?: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return checkAuth(this.authService, this.router, this.testAuthService, url);
  }

  canMatch(
    _route?: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return checkAuth(this.authService, this.router, this.testAuthService, url);
  }
}