import { Permission } from '../models/permission.model';

export type ScreenArea = 'admin' | 'student';

export type ScreenAction = 'view' | 'create' | 'edit' | 'delete';

export const SCREEN_ACTIONS: ScreenAction[] = ['view', 'create', 'edit', 'delete'];

/**
 * A page that access can be granted to. Each action is the permission behind one
 * cell of the access matrix, so the services' permission checks stay the gate.
 */
export interface Screen {
  id: string;
  label: string;
  area: ScreenArea;
  path: string;
  icon: string;
  view: Permission;
  create?: Permission;
  edit?: Permission;
  delete?: Permission;
  /** Actions that are not create, edit or delete. */
  extra?: { label: string; permission: Permission }[];
  /** A grant on a single course also opens this screen, which then lists only those courses. */
  courseScoped?: boolean;
  /** Screens in a section share one sidebar link and are shown as tabs. */
  section?: 'security';
  exact?: boolean;
}

// The admin sidebar lists the admin screens in this order. Add new screens here.
export const SCREENS: Screen[] = [
  {
    id: 'overview',
    label: 'Overview',
    area: 'admin',
    path: '/admin',
    icon: 'bi-speedometer2',
    view: 'reports.view',
    extra: [{ label: 'Reset demo data', permission: 'system.reset' }],
    exact: true,
  },
  {
    id: 'courses',
    label: 'Courses',
    area: 'admin',
    path: '/admin/courses',
    icon: 'bi-journal-text',
    view: 'courses.view',
    create: 'courses.create',
    edit: 'courses.edit',
    delete: 'courses.delete',
    courseScoped: true,
  },
  {
    id: 'instructors',
    label: 'Instructors',
    area: 'admin',
    path: '/admin/instructors',
    icon: 'bi-person-video3',
    view: 'instructors.view',
    create: 'instructors.create',
    edit: 'instructors.edit',
    delete: 'instructors.delete',
  },
  { id: 'users', label: 'Users', area: 'admin', path: '/admin/users', icon: 'bi-people', view: 'users.view' },
  {
    id: 'enrollments',
    label: 'Enrollments',
    area: 'admin',
    path: '/admin/enrollments',
    icon: 'bi-card-checklist',
    view: 'enrollments.view',
    edit: 'enrollments.edit',
  },
  {
    id: 'transactions',
    label: 'Transactions',
    area: 'admin',
    path: '/admin/transactions',
    icon: 'bi-credit-card',
    view: 'transactions.view',
    edit: 'transactions.edit',
    delete: 'transactions.delete',
  },
  {
    id: 'roles',
    label: 'Roles & permissions',
    area: 'admin',
    path: '/admin/security/roles',
    icon: 'bi-person-badge',
    view: 'roles.view',
    edit: 'roles.edit',
    section: 'security',
  },
  {
    id: 'access',
    label: 'User access',
    area: 'admin',
    path: '/admin/security/access',
    icon: 'bi-person-lock',
    view: 'access.view',
    edit: 'access.edit',
    section: 'security',
  },
  {
    id: 'community-moderation',
    label: 'Community',
    area: 'admin',
    path: '/admin/community',
    icon: 'bi-chat-square-text',
    view: 'community.moderate',
    courseScoped: true,
  },
  {
    id: 'certificates',
    label: 'Certificates',
    area: 'admin',
    path: '/admin/certificates',
    icon: 'bi-award',
    view: 'certificates.manage',
    courseScoped: true,
  },
  { id: 'dashboard', label: 'My dashboard', area: 'student', path: '/dashboard', icon: 'bi-grid', view: 'dashboard.view' },
  { id: 'learn', label: 'Learning area', area: 'student', path: '/dashboard', icon: 'bi-play-circle', view: 'learn.view' },
  { id: 'checkout', label: 'Checkout', area: 'student', path: '/courses', icon: 'bi-bag', view: 'checkout.view' },
  {
    id: 'community',
    label: 'Course community',
    area: 'student',
    path: '/dashboard',
    icon: 'bi-chat-dots',
    view: 'community.view',
    create: 'community.post',
  },
];

export function screen(id: string): Screen {
  const found = SCREENS.find((s) => s.id === id);
  if (!found) throw new Error(`Unknown screen "${id}"`);
  return found;
}

/** Every permission a screen uses: View first, then its actions. */
export function screenPermissions(s: Screen): Permission[] {
  return [s.view, s.create, s.edit, s.delete, ...(s.extra ?? []).map((e) => e.permission)].filter(
    (p): p is Permission => p !== undefined,
  );
}
