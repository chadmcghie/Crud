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
export class AuthGuard implements CanActivate, CanActivateChild, CanLoad, CanMatch {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route?: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return this.checkAuth(state?.url);
  }

  canActivateChild(
    childRoute?: ActivatedRouteSnapshot,
    state?: RouterStateSnapshot
  ): boolean | UrlTree {
    return this.checkAuth(state?.url);
  }

  canLoad(
    route?: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return this.checkAuth(url);
  }

  canMatch(
    route?: Route,
    segments?: UrlSegment[]
  ): boolean | UrlTree {
    const url = segments ? '/' + segments.map(s => s.path).join('/') : undefined;
    return this.checkAuth(url);
  }

  private checkAuth(returnUrl?: string): boolean | UrlTree {
    if (this.authService.isAuthenticated()) {
      return true;
    }

    // Redirect to login page with return URL
    const queryParams = returnUrl ? { returnUrl } : undefined;
    return this.router.createUrlTree(['/login'], { queryParams });
  }
}