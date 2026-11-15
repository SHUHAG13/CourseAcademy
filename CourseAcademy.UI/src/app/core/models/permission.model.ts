/** An action in one area of the app, named `area.action`. */
export type Permission =
  | 'admin.access'
  | 'reports.view'
  | 'system.reset'
  | 'courses.view'
  | 'courses.create'
  | 'courses.edit'
  | 'courses.delete'
  | 'instructors.view'
  | 'instructors.create'
  | 'instructors.edit'
  | 'instructors.delete'
  | 'users.view'
  | 'enrollments.view'
  | 'enrollments.edit'
  | 'transactions.view'
  | 'transactions.edit'
  | 'transactions.delete'
  | 'roles.view'
  | 'roles.edit'
  | 'access.view'
  | 'access.edit'
  | 'dashboard.view'
  | 'learn.view'
  | 'checkout.view'
  | 'community.view'
  | 'community.post'
  | 'community.moderate'
  | 'certificates.manage';

export interface PermissionGroup {
  area: string;
  permissions: { id: Permission; label: string }[];
}

/** Every permission with a readable label. Screens in `screens.data.ts` arrange them into the access matrix. */
export const PERMISSION_GROUPS: PermissionGroup[] = [
  {
    area: 'Admin panel',
    permissions: [
      { id: 'admin.access', label: 'Open the admin panel' },
      { id: 'reports.view', label: 'View the overview and reports' },
      { id: 'system.reset', label: 'Reset demo data' },
    ],
  },
  {
    area: 'Courses',
    permissions: [
      { id: 'courses.view', label: 'View courses, including drafts' },
      { id: 'courses.create', label: 'Create courses' },
      { id: 'courses.edit', label: 'Edit and publish courses' },
      { id: 'courses.delete', label: 'Delete courses' },
      { id: 'instructors.view', label: 'View instructors' },
      { id: 'instructors.create', label: 'Create instructors' },
      { id: 'instructors.edit', label: 'Edit instructors' },
      { id: 'instructors.delete', label: 'Delete instructors' },
    ],
  },
  {
    area: 'Users',
    permissions: [
      { id: 'users.view', label: 'View users' },
      { id: 'roles.view', label: 'View roles' },
      { id: 'roles.edit', label: 'Create, edit and assign roles' },
      { id: 'access.view', label: 'View user access' },
      { id: 'access.edit', label: 'Change user access' },
    ],
  },
  {
    area: 'Enrollments',
    permissions: [
      { id: 'enrollments.view', label: 'View enrollments' },
      { id: 'enrollments.edit', label: 'Change enrollments' },
    ],
  },
  {
    area: 'Payments',
    permissions: [
      { id: 'transactions.view', label: 'View transactions' },
      { id: 'transactions.edit', label: 'Change transactions' },
      { id: 'transactions.delete', label: 'Delete transactions' },
    ],
  },
  {
    area: 'Student area',
    permissions: [
      { id: 'dashboard.view', label: 'Open the student dashboard' },
      { id: 'learn.view', label: 'Open enrolled courses' },
      { id: 'checkout.view', label: 'Buy courses' },
    ],
  },
  {
    area: 'Community',
    permissions: [
      { id: 'community.view', label: 'Read course communities' },
      { id: 'community.post', label: 'Write posts, comments and replies' },
      { id: 'community.moderate', label: 'Moderate course communities' },
    ],
  },
  {
    area: 'Certificates',
    permissions: [{ id: 'certificates.manage', label: 'Set certification rules, review exams and manage certificates' }],
  },
];

export const ALL_PERMISSIONS: Permission[] = PERMISSION_GROUPS.flatMap((g) => g.permissions.map((p) => p.id));

/** The permissions that mean something for a single course, so they can be granted per course. */
export const COURSE_PERMISSIONS: Permission[] = ['courses.view', 'courses.edit', 'community.moderate', 'certificates.manage'];

export const ADMIN_ROLE_ID = 'admin';
export const INSTRUCTOR_ROLE_ID = 'instructor';
/** Kept as `user` so accounts saved before roles existed still match. */
export const STUDENT_ROLE_ID = 'user';

export interface Role {
  id: string;
  name: string;
  description: string;
  /** Applies to every course. The Admin role always has every permission, whatever is listed. */
  permissions: Permission[];
  /** Built-in roles can be edited but not deleted. */
  system: boolean;
}

/** Permissions one user has on one course only, e.g. an instructor on the courses they teach. */
export interface CourseGrant {
  id: string;
  userId: string;
  courseId: number;
  permissions: Permission[];
}
