import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { Course } from '../models/course.model';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from '../services/auth.service';
import { ProgressService } from '../services/progress.service';
import { API_BASE_URL, API_MODE } from './api-mode';
import { MeDto } from './auth.models';
import { EnrollmentDto } from './commerce.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';
import { CourseProgressDto } from './learning.models';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

const me: MeDto = {
  id: 2, name: 'Demo Student', email: 'student@courseacademy.demo', phone: null, roleCode: 'user', roleName: 'Student',
  permissions: ['dashboard.view', 'learn.view', 'checkout.view'], courseGrants: [], hasAdminPanel: false, homeUrl: '/dashboard',
  createdAt: '2026-11-01T00:00:00Z',
};

const enrollment: EnrollmentDto = {
  id: 1, userId: 2, studentName: 'Demo Student', studentEmail: me.email, courseId: 5, courseTitle: 'Photography',
  statusCode: 'active', enrolledAt: '2026-11-15T09:00:00Z', transactionNo: null,
};

/** "Intro" has two videos (100, 101); "Reading" has none, so it is a lesson itself. */
const course = {
  id: 5,
  slug: 'photo',
  modules: [{ id: '1', title: 'M', submodules: [{ id: '2', title: 'S', sections: [
    { id: '10', title: 'Intro', documents: [], topics: [], videos: [
      { id: '100', title: 'Welcome', url: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ', minutes: 5, preview: false },
      { id: '101', title: 'Tour', url: 'https://cdn.example.com/tour.mp4', minutes: 10, preview: false },
    ] },
    { id: '11', title: 'Reading', videos: [], documents: [], topics: [] },
  ] }] }],
} as unknown as Course;

const progress = (over: Partial<CourseProgressDto>): CourseProgressDto => ({
  courseId: 5, completedLessons: 0, totalLessons: 3, percent: 0, completedSectionIds: [], videos: [], ...over,
});

describe('Progress in API mode', () => {
  let http: HttpTestingController;
  let service: ProgressService;

  const settle = () => new Promise((r) => setTimeout(r, 0));

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_MODE, useValue: true },
        { provide: API_BASE_URL, useValue: BASE },
        { provide: PaymentGateway, useExisting: DemoPaymentGateway },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);
    const login = auth.login(me.email, 'Student@123');
    http.expectOne(`${BASE}/auth/login`).flush(ok({ accessToken: 'a', accessTokenExpiresAt: inOneHour(), refreshToken: 'r', me }));
    await login;
    service = TestBed.inject(ProgressService);
    // Start-up loads: the enrollment list, the dashboard totals and the catalogue.
    for (let round = 0; round < 3; round++) {
      await settle();
      for (const r of http.match(() => true)) {
        if (r.request.url.endsWith('/enrollments/mine')) r.flush(ok([enrollment]));
        else if (r.request.url.endsWith('/progress/mine')) r.flush(ok([{ courseId: 5, completedLessons: 1, totalLessons: 3, percent: 33 }]));
        else r.flush(ok([]));
      }
    }
  });

  afterEach(() => http.verify());

  it('shows the dashboard totals from the server', () => {
    expect(service.percent(course)).toBe(33);
    expect(service.completedCount(course)).toBe(1);
    expect(service.totalCount(course)).toBe(3);
  });

  it('marks a video complete and keeps the returned progress', async () => {
    const done = service.setVideoComplete(course, '100', true);
    const request = http.expectOne(`${BASE}/progress/videos/100/complete`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ completed: true });
    request.flush(ok(progress({ completedLessons: 2, percent: 67, videos: [{ videoId: 100, positionSeconds: 0, durationSeconds: null, isCompleted: true }] })));
    await done;

    expect(service.isVideoComplete(5, '100')).toBe(true);
    expect(service.isVideoComplete(5, '101')).toBe(false);
    expect(service.isComplete(5, '10')).toBe(false);
    expect(service.percent(course)).toBe(67);
  });

  it('completes a section without videos through the section endpoint', async () => {
    const done = service.setComplete(course, '11', true);
    http.expectOne(`${BASE}/progress/sections/11/complete`).flush(ok(progress({ completedLessons: 1, percent: 33, completedSectionIds: [11] })));
    await done;

    expect(service.isComplete(5, '11')).toBe(true);
  });

  it('saves the position of a video file and reports when the server completed it', async () => {
    const saved = service.savePosition(course, '101', 95, 100);
    const request = http.expectOne(`${BASE}/progress/videos/101/position`);
    expect(request.request.body).toEqual({ positionSeconds: 95, durationSeconds: 100 });
    request.flush(ok(progress({ videos: [{ videoId: 101, positionSeconds: 95, durationSeconds: 100, isCompleted: true }] })));

    expect(await saved).toBe(true);
    expect(service.position(5, '101')).toBe(95);
  });

  it('sends one report when the player reports twice at once (pause and ended)', async () => {
    const first = service.savePosition(course, '101', 100, 100);
    const second = service.savePosition(course, '101', 100, 100);
    http.expectOne(`${BASE}/progress/videos/101/position`).flush(
      ok(progress({ videos: [{ videoId: 101, positionSeconds: 100, durationSeconds: 100, isCompleted: true }] })),
    );

    expect([await first, await second]).toEqual([true, false]);
  });

  it('ignores a failed position report', async () => {
    const saved = service.savePosition(course, '101', 30, 100);
    http.expectOne(`${BASE}/progress/videos/101/position`).flush(
      { success: false, data: null, message: 'Server error', errors: [] },
      { status: 500, statusText: 'Server Error' },
    );

    expect(await saved).toBe(false);
  });
});
