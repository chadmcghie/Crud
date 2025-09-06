import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, ActivatedRouteSnapshot, RouterStateSnapshot, Route, UrlSegment } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthService } from './auth.service';
import { BehaviorSubject } from 'rxjs';

interface User {
  id: string;
  email: string;
  roles?: string[];
}



describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let currentUserSubject: BehaviorSubject<User | null>;

  beforeEach(() => {
    currentUserSubject = new BehaviorSubject<User | null>(null);
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated'], {
      currentUser$: currentUserSubject.asObservable()
    });
    const routerSpy = jasmine.createSpyObj('Router', ['createUrlTree', 'navigate']);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(AuthGuard);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  describe('canActivate', () => {
    it('should allow access when user is authenticated', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });

      const result = guard.canActivate();

      expect(result).toBe(true);
      expect(router.createUrlTree).not.toHaveBeenCalled();
    });

    it('should redirect to login when user is not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate();

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: undefined });
    });

    it('should redirect to login with return URL when route has state', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);
      const route = {} as ActivatedRouteSnapshot;
      const state = { url: '/protected' } as RouterStateSnapshot;

      const result = guard.canActivate(route, state);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/protected' }
      });
    });

    it('should check authentication status from service', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });

      guard.canActivate();

      expect(authService.isAuthenticated).toHaveBeenCalled();
    });
  });

  describe('canActivateChild', () => {
    it('should allow child access when user is authenticated', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });

      const result = guard.canActivateChild();

      expect(result).toBe(true);
      expect(router.createUrlTree).not.toHaveBeenCalled();
    });

    it('should redirect to login for child routes when not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivateChild();

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: undefined });
    });

    it('should redirect with return URL for child routes', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);
      const childRoute = {} as ActivatedRouteSnapshot;
      const state = { url: '/admin/users' } as RouterStateSnapshot;

      const result = guard.canActivateChild(childRoute, state);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/admin/users' }
      });
    });
  });

  describe('canLoad', () => {
    it('should allow module loading when user is authenticated', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });

      const result = guard.canLoad();

      expect(result).toBe(true);
      expect(router.createUrlTree).not.toHaveBeenCalled();
    });

    it('should redirect to login when trying to load module without authentication', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canLoad();

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: undefined });
    });

    it('should redirect with return URL when loading route with segments', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);
      const route: Route = {
        path: 'admin'
      };
      const segments: UrlSegment[] = [
        new UrlSegment('admin', {}),
        new UrlSegment('dashboard', {})
      ];

      const result = guard.canLoad(route, segments);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/admin/dashboard' }
      });
    });

    it('should handle empty segments array', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);
      const route: Route = { path: '' };

      const result = guard.canLoad(route, []);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/' } });
    });
  });

  describe('canMatch', () => {
    it('should allow route matching when user is authenticated', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });

      const result = guard.canMatch();

      expect(result).toBe(true);
      expect(router.createUrlTree).not.toHaveBeenCalled();
    });

    it('should redirect to login when route matching without authentication', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canMatch();

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: undefined });
    });

    it('should handle route with segments for matching', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);
      const route: Route = { path: 'secure' };
      const segments: UrlSegment[] = [new UrlSegment('secure', {})];

      const result = guard.canMatch(route, segments);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/secure' }
      });
    });
  });

  describe('Edge Cases', () => {
    it('should handle user with empty roles array', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: [] });

      const result = guard.canActivate();

      expect(result).toBe(true);
    });

    it('should handle user without roles property', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com' } as User);

      const result = guard.canActivate();

      expect(result).toBe(true);
    });

    it('should handle authentication service returning undefined', () => {
      authService.isAuthenticated.and.returnValue(undefined as unknown as boolean);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate();

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: undefined });
    });

    it('should handle null user from service', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate();

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], { queryParams: undefined });
    });
  });
});