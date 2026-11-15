import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { authGuard, permissionGuard } from '../guards/auth.guards';
import { AuthService } from './auth.service';
import { PermissionService } from './permission.service';

describe('AuthService', () => {
  let auth: AuthService;
  let permissions: PermissionService;

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    auth = TestBed.inject(AuthService);
    permissions = TestBed.inject(PermissionService);
    await Promise.all([auth.ready, permissions.ready]);
    vi.useFakeTimers({ toFake: ['setTimeout'] });
  });

  afterEach(() => vi.useRealTimers());

  async function settled<T>(promise: Promise<T>): Promise<T> {
    await vi.advanceTimersByTimeAsync(1000);
    return promise;
  }

  it('logs in the seeded admin with the admin role', async () => {
    await settled(auth.login('ADMIN@courseacademy.demo', 'Admin@123'));
    expect(auth.isAdmin()).toBe(true);
    expect(permissions.homeUrl()).toBe('/admin');
  });

  it('rejects a wrong password', async () => {
    const attempt = auth.login('admin@courseacademy.demo', 'wrong-password');
    const assertion = expect(attempt).rejects.toThrow('Incorrect email or password');
    await vi.advanceTimersByTimeAsync(1000);
    await assertion;
    expect(auth.isLoggedIn()).toBe(false);
  });

  it('registers new accounts with the User role only', async () => {
    const user = await settled(auth.register('New Student', 'new@example.com', 'Passw0rd1'));
    expect(user.role).toBe('user');
    expect(auth.isAdmin()).toBe(false);
    expect(permissions.homeUrl()).toBe('/dashboard');
  });

  it('refuses a duplicate email', async () => {
    const attempt = auth.register('Copy', 'student@courseacademy.demo', 'Passw0rd1');
    const assertion = expect(attempt).rejects.toThrow('already exists');
    await vi.advanceTimersByTimeAsync(1000);
    await assertion;
  });

  it('logs out', async () => {
    await settled(auth.login('student@courseacademy.demo', 'Student@123'));
    auth.logout();
    expect(auth.user()).toBeNull();
  });

  describe('route guards', () => {
    const state = { url: '/admin/courses' } as RouterStateSnapshot;
    const route = {} as ActivatedRouteSnapshot;
    const runAuth = () => TestBed.runInInjectionContext(() => authGuard(route, state));
    const runAdmin = () => TestBed.runInInjectionContext(() => permissionGuard('admin.access')(route, state));
    const url = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

    it('send visitors to login and remember where they were going', () => {
      expect(url(runAuth())).toBe('/login?returnUrl=%2Fadmin%2Fcourses');
      expect(url(runAdmin())).toBe('/login?returnUrl=%2Fadmin%2Fcourses');
    });

    it('keep ordinary users out of the admin area', async () => {
      await settled(auth.login('student@courseacademy.demo', 'Student@123'));
      expect(runAuth()).toBe(true);
      expect(url(runAdmin())).toBe('/dashboard');
    });

    it('let admins into the admin area', async () => {
      await settled(auth.login('admin@courseacademy.demo', 'Admin@123'));
      expect(runAdmin()).toBe(true);
    });

    it('let instructors into the admin area, starting at their courses', async () => {
      await settled(auth.login('instructor@courseacademy.demo', 'Instructor@123'));
      expect(runAdmin()).toBe(true);
      expect(permissions.homeUrl()).toBe('/admin/courses');
    });
  });
});
