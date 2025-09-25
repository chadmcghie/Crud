import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { map } from 'rxjs/operators';

export const canActivateAdmin = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.currentUser$.pipe(
    map(user => {
      console.log('Admin guard - checking user:', user);
      console.log('User roles:', user?.roles);

      if (!user) {
        console.log('No user - redirecting to login');
        router.navigate(['/login']);
        return false;
      }

      if (!user.roles?.includes('Admin')) {
        console.log('User does not have Admin role - redirecting to unauthorized');
        console.log('Available roles:', user.roles);
        router.navigate(['/unauthorized']);
        return false;
      }

      console.log('User has Admin role - allowing access');
      return true;
    })
  );
};