import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SCREENS, screen } from '../data/screens.data';
import { Permission } from '../models/permission.model';
import { AuthService } from '../services/auth.service';
import { PermissionService } from '../services/permission.service';
import { ToastService } from '../services/toast.service';

// These guards only control navigation. Services check permissions again before
// every change, and a real backend must also enforce them on every API call.

export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).isLoggedIn()) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Allows the route when the user has `permission`.
 * - `courseParam`: also accept a grant on the course whose id is in this route param, e.g. `'id'`.
 * - `anyCourse`: also accept a grant on any course, e.g. for a list that shows only those courses.
 */
export function permissionGuard(
  permission: Permission,
  options: { courseParam?: string; anyCourse?: boolean } = {},
): CanActivateFn {
  return (route, state) => {
    const auth = inject(AuthService);
    const permissions = inject(PermissionService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }
    const courseId = options.courseParam ? Number(route.paramMap.get(options.courseParam)) : undefined;
    const allowed = options.anyCourse ? permissions.canOnAnyCourse(permission) : permissions.can(permission, courseId);
    if (allowed) return true;

    inject(ToastService).error("You don't have permission to open that page.");
    // The home URL is always a page this user can open, so this never loops.
    return router.parseUrl(permissions.homeUrl());
  };
}

/** Allows the route when the user can view the screen with this id in `SCREENS`. */
export function screenGuard(screenId: string): CanActivateFn {
  const s = screen(screenId);
  return permissionGuard(s.view, { anyCourse: s.courseScoped });
}

/** Sends a section's own URL, e.g. `/admin/security`, to the first of its screens the user can view. */
export function sectionGuard(section: 'security'): CanActivateFn {
  return () => {
    const permissions = inject(PermissionService);
    const first = SCREENS.find((s) => s.section === section && permissions.canView(s));
    return inject(Router).parseUrl(first?.path ?? permissions.homeUrl());
  };
}
