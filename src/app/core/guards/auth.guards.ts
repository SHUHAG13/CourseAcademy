import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

// These guards only control navigation. Browser data can be edited by the user,
// so a real backend must also enforce authentication and roles on every API call.

export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).isLoggedIn()) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAdmin()) return true;
  if (auth.isLoggedIn()) {
    inject(ToastService).error('That page is only available to admins.');
    return router.createUrlTree(['/dashboard']);
  }
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
