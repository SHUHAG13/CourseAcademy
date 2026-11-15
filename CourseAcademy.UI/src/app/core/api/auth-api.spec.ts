import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { AuthService } from '../services/auth.service';
import { PermissionService } from '../services/permission.service';
import { ApiError } from './api-error';
import { API_BASE_URL, API_MODE } from './api-mode';
import { ApiService } from './api.service';
import { AuthResultDto, MeDto } from './auth.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });
const fail = (message: string) => ({ success: false, data: null, message, errors: [] });
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

const studentMe: MeDto = {
  id: 2,
  name: 'Demo Student',
  email: 'student@courseacademy.demo',
  phone: null,
  roleCode: 'user',
  roleName: 'Student',
  permissions: ['dashboard.view', 'learn.view', 'checkout.view', 'community.view', 'community.post'],
  courseGrants: [],
  hasAdminPanel: false,
  homeUrl: '/dashboard',
  createdAt: '2026-11-01T00:00:00Z',
};

const adminMe: MeDto = {
  ...studentMe,
  id: 1,
  name: 'Academy Admin',
  email: 'admin@courseacademy.demo',
  roleCode: 'admin',
  roleName: 'Admin',
  permissions: ['reports.view', 'roles.view', 'roles.edit', 'access.view', 'access.edit', 'users.view', 'dashboard.view'],
  hasAdminPanel: true,
  homeUrl: '/admin',
};

const result = (me: MeDto, n = 1): AuthResultDto => ({
  accessToken: `access-${n}`,
  accessTokenExpiresAt: inOneHour(),
  refreshToken: `refresh-${n}`,
  me,
});

describe('API mode sign-in', () => {
  let http: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_MODE, useValue: true },
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => http.verify());

  /** Lets queued promise callbacks (e.g. a refresh followed by a retry) run. */
  const settle = () => new Promise((r) => setTimeout(r, 0));

  async function signIn(me: MeDto, remember = true): Promise<void> {
    const login = auth.login(me.email, 'secret', remember);
    http.expectOne(`${BASE}/auth/login`).flush(ok(result(me)));
    if (me.permissions.includes('users.view')) {
      await settle();
      http.expectOne((r) => r.url === `${BASE}/users`).flush(ok({ items: [], total: 0, page: 1, pageSize: 100 }));
    }
    await login;
  }

  it('signs in through the API and keeps "Remember me" tokens in localStorage', async () => {
    const login = auth.login(' student@courseacademy.demo ', 'Student@123', true);

    const request = http.expectOne(`${BASE}/auth/login`);
    expect(request.request.body).toEqual({ email: 'student@courseacademy.demo', password: 'Student@123', remember: true });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(ok(result(studentMe)));
    const user = await login;

    expect(user.id).toBe('2');
    expect(auth.user()?.role).toBe('user');
    expect(localStore.getItem('ca-auth')).toContain('refresh-1');
    expect(sessionStore.getItem('ca-auth')).toBeNull();

    const permissions = TestBed.inject(PermissionService);
    expect(permissions.can('dashboard.view')).toBe(true);
    expect(permissions.can('users.view')).toBe(false);
    expect(permissions.homeUrl()).toBe('/dashboard');
  });

  it('keeps session-only tokens in sessionStorage', async () => {
    await signIn(studentMe, false);

    expect(sessionStore.getItem('ca-auth')).toContain('access-1');
    expect(localStore.getItem('ca-auth')).toBeNull();
  });

  it('shows the API message for wrong credentials without trying a refresh', async () => {
    const login = auth.login('student@courseacademy.demo', 'wrong');

    http.expectOne(`${BASE}/auth/login`).flush(fail('Incorrect email or password.'), { status: 401, statusText: 'Unauthorized' });

    const error = (await login.catch((e: unknown) => e)) as ApiError;
    expect(error).toBeInstanceOf(ApiError);
    expect(error.message).toBe('Incorrect email or password.');
    expect(auth.isLoggedIn()).toBe(false);
  });

  it('adds the access token, and on 401 refreshes once and repeats the request', async () => {
    await signIn(studentMe);
    const api = TestBed.inject(ApiService);

    const first = api.get<string>('things/1');
    const second = api.get<string>('things/2');
    const call1 = http.expectOne(`${BASE}/things/1`);
    const call2 = http.expectOne(`${BASE}/things/2`);
    expect(call1.request.headers.get('Authorization')).toBe('Bearer access-1');

    call1.flush(fail('Please log in to continue.'), { status: 401, statusText: 'Unauthorized' });
    call2.flush(fail('Please log in to continue.'), { status: 401, statusText: 'Unauthorized' });
    await settle();

    // Both failures share one refresh, because a refresh token works only once.
    const refresh = http.expectOne(`${BASE}/auth/refresh`);
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush(ok({ accessToken: 'access-2', accessTokenExpiresAt: inOneHour(), refreshToken: 'refresh-2' }));
    await settle();

    const retries: TestRequest[] = http.match((r) => r.url.startsWith(`${BASE}/things/`));
    expect(retries.length).toBe(2);
    for (const retry of retries) {
      expect(retry.request.headers.get('Authorization')).toBe('Bearer access-2');
      retry.flush(ok('done'));
    }
    expect(await first).toBe('done');
    expect(await second).toBe('done');
    expect(localStore.getItem('ca-auth')).toContain('refresh-2');
  });

  it('ends the session and goes to the login page when the refresh is refused', async () => {
    await signIn(studentMe);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const call = TestBed.inject(ApiService).get('things/1');
    http.expectOne(`${BASE}/things/1`).flush(fail('Please log in to continue.'), { status: 401, statusText: 'Unauthorized' });
    await settle();
    http.expectOne(`${BASE}/auth/refresh`).flush(fail('Your session has expired. Please log in again.'), { status: 401, statusText: 'Unauthorized' });

    const error = (await call.catch((e: unknown) => e)) as ApiError;
    TestBed.tick();

    expect(error.status).toBe(401);
    expect(auth.isLoggedIn()).toBe(false);
    expect(localStore.getItem('ca-auth')).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('logs out on the server and clears the tokens', async () => {
    await signIn(studentMe);

    auth.logout();

    const request = http.expectOne(`${BASE}/auth/logout`);
    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush(ok(null));
    expect(auth.user()).toBeNull();
    expect(localStore.getItem('ca-auth')).toBeNull();
  });

  it('sends a new role to the API as a list of screens', async () => {
    await signIn(adminMe);
    const permissions = TestBed.inject(PermissionService);
    TestBed.tick();
    http.expectOne(`${BASE}/roles`).flush(ok([]));

    const save = permissions.saveRole({
      id: permissions.newRoleId(),
      name: ' Support ',
      description: '',
      permissions: ['enrollments.view', 'enrollments.edit', 'transactions.view'],
      system: false,
    });

    const create = http.expectOne((r) => r.method === 'POST' && r.url === `${BASE}/roles`);
    expect(create.request.body).toEqual({ name: 'Support', description: '', screens: ['enrollments', 'transactions'] });
    create.flush(ok({ code: 'role-abc', name: 'Support', description: '', isSystem: false, screens: ['enrollments', 'transactions'], permissions: ['enrollments.view'], memberCount: 0 }));
    await settle();
    http.expectOne((r) => r.method === 'GET' && r.url === `${BASE}/roles`).flush(ok([]));

    expect((await save).id).toBe('role-abc');
  });
});
