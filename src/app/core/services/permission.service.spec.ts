import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { screenGuard } from '../guards/auth.guards';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

describe('PermissionService', () => {
  let auth: AuthService;
  let permissions: PermissionService;
  let courses: CourseService;

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: PaymentGateway, useExisting: DemoPaymentGateway }],
    });
    auth = TestBed.inject(AuthService);
    permissions = TestBed.inject(PermissionService);
    courses = TestBed.inject(CourseService);
    await Promise.all([auth.ready, permissions.ready, courses.ready]);
    vi.useFakeTimers({ toFake: ['setTimeout'] });
  });

  afterEach(() => vi.useRealTimers());

  async function logIn(email: string, password: string): Promise<void> {
    const login = auth.login(email, password);
    await vi.advanceTimersByTimeAsync(1000);
    await login;
  }

  it('gives the admin every permission', async () => {
    await logIn('admin@courseacademy.demo', 'Admin@123');
    expect(permissions.can('roles.edit')).toBe(true);
    expect(permissions.can('courses.delete', 2)).toBe(true);
  });

  it('gives students no admin permissions', async () => {
    await logIn('student@courseacademy.demo', 'Student@123');
    expect(permissions.can('admin.access')).toBe(false);
    expect(permissions.canOnAnyCourse('courses.view')).toBe(false);
  });

  it('limits instructors to the courses they are granted', async () => {
    await logIn('instructor@courseacademy.demo', 'Instructor@123');
    expect(permissions.can('courses.edit', 1)).toBe(true);
    expect(permissions.can('courses.edit', 2)).toBe(false);
    expect(permissions.can('courses.edit')).toBe(false);
  });

  it('blocks changes in the service, not only in the page', async () => {
    await logIn('instructor@courseacademy.demo', 'Instructor@123');
    const own = courses.byId(1)!;
    const other = courses.byId(2)!;

    await expect(courses.saveCourse({ ...own, subtitle: 'Updated' })).resolves.toBeTruthy();
    await expect(courses.saveCourse({ ...other, subtitle: 'Updated' })).rejects.toBeInstanceOf(PermissionDeniedError);
    await expect(courses.removeCourse(1)).rejects.toBeInstanceOf(PermissionDeniedError);
    await expect(permissions.saveRole({ ...permissions.role('user')!, permissions: ['roles.edit'] })).rejects.toBeInstanceOf(
      PermissionDeniedError,
    );
  });

  it('applies role changes made by an admin', async () => {
    await logIn('admin@courseacademy.demo', 'Admin@123');
    await permissions.saveRole({ ...permissions.role('user')!, permissions: ['users.view'] });
    expect(permissions.role('user')!.permissions).toEqual(['users.view']);

    await expect(permissions.setUserRole('user-admin', 'user')).rejects.toThrow("can't change your own role");
    await expect(permissions.removeRole('instructor')).rejects.toThrow('Built-in roles');
  });

  describe('screen access for one user', () => {
    beforeEach(async () => {
      await logIn('admin@courseacademy.demo', 'Admin@123');
      await permissions.setUserAccess('user-student', ['instructors.view']);
      await logIn('student@courseacademy.demo', 'Student@123');
    });

    it('replaces the permissions of the role', () => {
      expect(permissions.can('instructors.view')).toBe(true);
      expect(permissions.can('dashboard.view')).toBe(false);
      expect(permissions.hasAdminPanel()).toBe(true);
      expect(permissions.homeUrl()).toBe('/admin/instructors');
    });

    it('blocks screens whose View is not ticked', () => {
      const run = (id: string) =>
        TestBed.runInInjectionContext(() => screenGuard(id)({} as ActivatedRouteSnapshot, { url: '/x' } as RouterStateSnapshot));
      expect(run('instructors')).toBe(true);
      expect(TestBed.inject(Router).serializeUrl(run('dashboard') as UrlTree)).toBe('/admin/instructors');
    });

    it('refuses actions that are not ticked, in the service', async () => {
      await expect(courses.removeInstructor('tanvir-hasan')).rejects.toBeInstanceOf(PermissionDeniedError);
    });

    it('returns to the role defaults after a reset', async () => {
      await logIn('admin@courseacademy.demo', 'Admin@123');
      await permissions.resetUserAccess('user-student');
      expect(auth.users().find((u) => u.id === 'user-student')!.access).toBeUndefined();

      await logIn('student@courseacademy.demo', 'Student@123');
      expect(permissions.can('dashboard.view')).toBe(true);
      expect(permissions.hasAdminPanel()).toBe(false);
    });
  });

  it('protects admins and your own account from access changes', async () => {
    await logIn('admin@courseacademy.demo', 'Admin@123');
    await expect(permissions.setUserAccess('user-admin', [])).rejects.toThrow("can't change your own access");

    await permissions.setUserRole('user-sample', 'admin');
    await expect(permissions.setUserAccess('user-sample', [])).rejects.toThrow('always have full access');
  });

  it("never stores admin.access in a user's access, because it is worked out", async () => {
    await logIn('admin@courseacademy.demo', 'Admin@123');
    await permissions.setUserAccess('user-student', ['admin.access', 'users.view']);
    expect(auth.users().find((u) => u.id === 'user-student')!.access).toEqual(['users.view']);
  });

  it('only stores permissions that work per course in a course grant', async () => {
    await logIn('admin@courseacademy.demo', 'Admin@123');
    await permissions.setCourseGrant('user-student', 2, ['courses.view', 'roles.edit']);
    expect(permissions.grantsFor('user-student')[0].permissions).toEqual(['courses.view']);

    await permissions.setCourseGrant('user-student', 2, []);
    expect(permissions.grantsFor('user-student')).toEqual([]);
  });
});
