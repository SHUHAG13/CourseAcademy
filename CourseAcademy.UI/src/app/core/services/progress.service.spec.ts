import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from './auth.service';
import { CourseService, allSections } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';
import { ProgressService } from './progress.service';

describe('ProgressService', () => {
  let progress: ProgressService;
  let courses: CourseService;

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: PaymentGateway, useExisting: DemoPaymentGateway }],
    });
    const auth = TestBed.inject(AuthService);
    progress = TestBed.inject(ProgressService);
    courses = TestBed.inject(CourseService);
    await Promise.all([
      auth.ready,
      progress.ready,
      courses.ready,
      TestBed.inject(PermissionService).ready,
      TestBed.inject(EnrollmentService).ready,
    ]);

    vi.useFakeTimers({ toFake: ['setTimeout'] });
    const login = auth.login('student@courseacademy.demo', 'Student@123');
    await vi.advanceTimersByTimeAsync(1000);
    await login;
    vi.useRealTimers();
  });

  it('starts from the demo progress in the enrolled course', () => {
    const course = courses.byId(1)!;
    expect(progress.completedCount(course)).toBe(2);
    expect(progress.percent(course)).toBe(Math.round((2 / allSections(course).length) * 100));
  });

  it('marks sections complete and undoes it', async () => {
    const course = courses.byId(1)!;
    const third = allSections(course)[2];

    await progress.setComplete(course, third.id, true);
    expect(progress.isComplete(course.id, third.id)).toBe(true);
    expect(progress.completedCount(course)).toBe(3);

    await progress.setComplete(course, third.id, false);
    expect(progress.completedCount(course)).toBe(2);
  });

  it('refuses progress in a course the student is not enrolled in', async () => {
    const other = courses.byId(2)!;
    await expect(progress.setComplete(other, allSections(other)[0].id, true)).rejects.toBeInstanceOf(PermissionDeniedError);
  });
});
