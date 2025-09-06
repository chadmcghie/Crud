import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, ActivatedRouteSnapshot } from '@angular/router';
import { RoleGuard } from './role.guard';
import { AuthService } from './auth.service';
import { BehaviorSubject } from 'rxjs';

describe('RoleGuard', () => {
  let guard: RoleGuard;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let currentUserSubject: BehaviorSubject<any>;

  beforeEach(() => {
    currentUserSubject = new BehaviorSubject(null);
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'hasRole'], {
      currentUser$: currentUserSubject.asObservable()
    });
    const routerSpy = jasmine.createSpyObj('Router', ['createUrlTree', 'navigate']);

    TestBed.configureTestingModule({
      providers: [
        RoleGuard,
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(RoleGuard);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should be created', () => {
    expect(guard).toBeTruthy();
  });

  describe('canActivate', () => {
    it('should allow access when user has required role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['admin'] });
      const route = createRouteSnapshot(['admin']);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
    });

    it('should allow access when user has one of multiple required roles', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValues(false, true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = createRouteSnapshot(['admin', 'user']);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
      expect(authService.hasRole).toHaveBeenCalledWith('user');
    });

    it('should redirect to unauthorized when user lacks required role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = createRouteSnapshot(['admin']);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate(route);

      expect(result).toBe(urlTree);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
      expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized']);
    });

    it('should redirect to login when user is not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const route = createRouteSnapshot(['admin']);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);
      const state = { url: '/admin/dashboard', root: null } as any;

      const result = guard.canActivate(route, state);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/admin/dashboard' }
      });
      expect(authService.hasRole).not.toHaveBeenCalled();
    });

    it('should allow access when no roles are specified in route', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = createRouteSnapshot(undefined);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).not.toHaveBeenCalled();
    });

    it('should allow access when empty roles array is specified', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = createRouteSnapshot([]);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).not.toHaveBeenCalled();
    });
  });

  describe('canActivateChild', () => {
    it('should allow child access when user has required role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['admin'] });
      const route = createRouteSnapshot(['admin']);

      const result = guard.canActivateChild(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
    });

    it('should redirect to unauthorized for child routes when lacking role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = createRouteSnapshot(['admin']);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivateChild(route);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized']);
    });
  });

  describe('canLoad', () => {
    it('should allow module loading when user has required role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['admin'] });
      const route = { data: { roles: ['admin'] }, path: 'admin' };

      const result = guard.canLoad(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
    });

    it('should redirect to unauthorized when loading module without role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = { data: { roles: ['admin'] }, path: 'admin' };
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canLoad(route);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized']);
    });

    it('should redirect to login when not authenticated for module loading', () => {
      authService.isAuthenticated.and.returnValue(false);
      currentUserSubject.next(null);
      const route = { data: { roles: ['admin'] }, path: 'admin' };
      const segments = [{ path: 'admin', parameters: {}, parameterMap: { get: () => null, has: () => false, getAll: () => [], keys: [] } as any }];
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canLoad(route, segments);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
        queryParams: { returnUrl: '/admin' }
      });
    });
  });

  describe('canMatch', () => {
    it('should allow route matching when user has required role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['admin'] });
      const route = { data: { roles: ['admin'] }, path: 'admin' };

      const result = guard.canMatch(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
    });

    it('should redirect to unauthorized when matching without role', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = { data: { roles: ['admin'] }, path: 'admin' };
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canMatch(route);

      expect(result).toBe(urlTree);
      expect(router.createUrlTree).toHaveBeenCalledWith(['/unauthorized']);
    });
  });

  describe('Role Checking Logic', () => {
    it('should check multiple roles in order', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValues(false, false, true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['moderator'] });
      const route = createRouteSnapshot(['admin', 'user', 'moderator']);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledTimes(3);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
      expect(authService.hasRole).toHaveBeenCalledWith('user');
      expect(authService.hasRole).toHaveBeenCalledWith('moderator');
    });

    it('should stop checking roles after finding a match', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValues(false, true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = createRouteSnapshot(['admin', 'user', 'moderator']);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledTimes(2);
      expect(authService.hasRole).not.toHaveBeenCalledWith('moderator');
    });

    it('should handle case-sensitive role names', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['admin'] });
      const route = createRouteSnapshot(['Admin']);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate(route);

      expect(result).toBe(urlTree);
      expect(authService.hasRole).toHaveBeenCalledWith('Admin');
    });
  });

  describe('Edge Cases', () => {
    it('should handle user with null roles', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: null });
      const route = createRouteSnapshot(['admin']);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate(route);

      expect(result).toBe(urlTree);
    });

    it('should handle user with undefined roles', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(false);
      currentUserSubject.next({ id: '1', email: 'test@example.com' });
      const route = createRouteSnapshot(['admin']);
      const urlTree = {} as UrlTree;
      router.createUrlTree.and.returnValue(urlTree);

      const result = guard.canActivate(route);

      expect(result).toBe(urlTree);
    });

    it('should handle route with roles in data property', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.hasRole.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['admin'] });
      const route = createRouteSnapshot(['admin']);

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).toHaveBeenCalledWith('admin');
    });

    it('should handle missing data property in route', () => {
      authService.isAuthenticated.and.returnValue(true);
      currentUserSubject.next({ id: '1', email: 'test@example.com', roles: ['user'] });
      const route = {
        url: [],
        params: {},
        queryParams: {},
        fragment: null,
        outlet: 'primary',
        component: null,
        routeConfig: {},
        root: null as any,
        parent: null,
        firstChild: null,
        children: [],
        pathFromRoot: [],
        paramMap: null as any,
        queryParamMap: null as any,
        data: {},
        title: undefined
      } as ActivatedRouteSnapshot;

      const result = guard.canActivate(route);

      expect(result).toBe(true);
      expect(authService.hasRole).not.toHaveBeenCalled();
    });
  });

  // Helper function to create route snapshot with roles
  function createRouteSnapshot(roles?: string[]): ActivatedRouteSnapshot {
    return {
      data: roles ? { roles } : {},
      url: [],
      params: {},
      queryParams: {},
      fragment: null,
      outlet: 'primary',
      component: null,
      routeConfig: { data: roles ? { roles } : {} },
      root: null as any,
      parent: null,
      firstChild: null,
      children: [],
      pathFromRoot: [],
      paramMap: null as any,
      queryParamMap: null as any,
      title: undefined
    } as ActivatedRouteSnapshot;
  }
});