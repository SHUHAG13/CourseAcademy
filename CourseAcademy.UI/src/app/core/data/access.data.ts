import { ALL_PERMISSIONS, ADMIN_ROLE_ID, CourseGrant, INSTRUCTOR_ROLE_ID, Role, STUDENT_ROLE_ID } from '../models/permission.model';
import { COURSES } from './courses.data';
import { DEMO_USERS } from './demo-records.data';

// Default roles and course access, copied into browser storage on first run.

export const DEFAULT_ROLES: Role[] = [
  {
    id: ADMIN_ROLE_ID,
    name: 'Admin',
    description: 'Full access to every part of the academy.',
    permissions: ALL_PERMISSIONS,
    system: true,
  },
  {
    id: INSTRUCTOR_ROLE_ID,
    name: 'Instructor',
    description: 'Opens the admin panel for the courses they are granted.',
    permissions: ['dashboard.view', 'learn.view', 'checkout.view', 'community.view', 'community.post'],
    system: true,
  },
  {
    id: STUDENT_ROLE_ID,
    name: 'Student',
    description: 'Learns in the courses they are enrolled in.',
    permissions: ['dashboard.view', 'learn.view', 'checkout.view', 'community.view', 'community.post'],
    system: true,
  },
];

/** Demo instructors can view, edit, moderate and certify the courses taught by their instructor profile. */
export const DEMO_COURSE_GRANTS: CourseGrant[] = DEMO_USERS.filter((u) => u.instructorId).flatMap((u) =>
  COURSES.filter((c) => c.instructorId === u.instructorId).map((c) => ({
    id: `grant-${u.id}-${c.id}`,
    userId: u.id,
    courseId: c.id,
    permissions: ['courses.view', 'courses.edit', 'community.moderate', 'certificates.manage'],
  })),
);
