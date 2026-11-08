import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from './auth.service';
import { CertificateService } from './certificate.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';
import { ProgressService } from './progress.service';

describe('CertificateService', () => {
  let auth: AuthService;
  let certificates: CertificateService;
  let courses: CourseService;

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: PaymentGateway, useExisting: DemoPaymentGateway }],
    });
    auth = TestBed.inject(AuthService);
    certificates = TestBed.inject(CertificateService);
    courses = TestBed.inject(CourseService);
    await Promise.all([
      auth.ready,
      certificates.ready,
      courses.ready,
      TestBed.inject(PermissionService).ready,
      TestBed.inject(EnrollmentService).ready,
      TestBed.inject(ProgressService).ready,
    ]);
  });

  async function logIn(email: string, password = 'Student@123'): Promise<void> {
    vi.useFakeTimers({ toFake: ['setTimeout'] });
    const login = auth.login(email, password);
    await vi.advanceTimersByTimeAsync(1000);
    await login;
    vi.useRealTimers();
  }

  it('verifies demo certificates by code, ignoring case and spaces', () => {
    expect(certificates.findByCode('  ca-2026-k7q2xm ')?.userName).toBe('Sample Learner');
    expect(certificates.findByCode('CA-2026-NOPE00')).toBeUndefined();
  });

  it('opens the central exam only after the course is completed', async () => {
    await logIn('student@courseacademy.demo');
    // The demo student finished the software course but has only started the video course.
    expect(certificates.status(courses.byId(4)!)!.canSubmit).toBe(true);
    const video = certificates.status(courses.byId(1)!)!;
    expect(video.progressMet).toBe(false);
    await expect(certificates.submitExam(courses.byId(1)!, 'https://example.com/reel', '')).rejects.toThrow('Complete the course');
  });

  it('issues the certificate when an instructor passes the exam, and only then', async () => {
    await logIn('student@courseacademy.demo');
    const course = courses.byId(4)!;
    await expect(certificates.submitExam(course, 'ftp://bad-link', '')).rejects.toThrow('https://');
    const submission = await certificates.submitExam(course, 'https://github.com/demo/capstone', 'Live demo inside');
    expect(certificates.status(course)!.canSubmit).toBe(false);
    await expect(certificates.review(submission.id, 90, '')).rejects.toBeInstanceOf(PermissionDeniedError);

    await logIn('sabbir@courseacademy.demo', 'Instructor@123');
    await certificates.review(submission.id, 85, 'Great work');
    const issued = certificates.activeCertificate('user-student', 4)!;
    expect(issued.scorePercent).toBe(85);
    expect(issued.code).toMatch(/^CA-\d{4}-[A-Z2-9]{6}$/);
    expect(certificates.findByCode(issued.code)?.id).toBe(issued.id);
  });

  it('lets a failed student retry until the attempts run out', async () => {
    await logIn('student@courseacademy.demo');
    const course = courses.byId(4)!;
    const first = await certificates.submitExam(course, 'https://github.com/demo/v1', '');

    await logIn('admin@courseacademy.demo', 'Admin@123');
    await certificates.review(first.id, 40, 'Missing the API');

    await logIn('student@courseacademy.demo');
    expect(certificates.status(course)!.attemptsLeft).toBe(1);
    const second = await certificates.submitExam(course, 'https://github.com/demo/v2', '');

    await logIn('admin@courseacademy.demo', 'Admin@123');
    await certificates.review(second.id, 50, '');

    await logIn('student@courseacademy.demo');
    const status = certificates.status(course)!;
    expect(status.attemptsLeft).toBe(0);
    expect(status.canSubmit).toBe(false);
    expect(status.certificate).toBeUndefined();
  });

  it('issues completion certificates automatically', async () => {
    await logIn('admin@courseacademy.demo', 'Admin@123');
    await certificates.saveRule({ ...certificates.rule(4), mode: 'completion' });
    await expect(certificates.saveRule({ ...certificates.rule(4), mode: 'exam', passingScore: 200 })).rejects.toThrow('pass mark');

    await logIn('student@courseacademy.demo');
    const certificate = await certificates.evaluate(courses.byId(4)!);
    expect(certificate?.mode).toBe('completion');
    expect(await certificates.evaluate(courses.byId(4)!)).toEqual(certificate);
  });

  it('shows revoked certificates as revoked and lets instructors manage only their courses', async () => {
    await logIn('sabbir@courseacademy.demo', 'Instructor@123');
    await expect(certificates.setRevoked('certificate-1', true, 'Plagiarism')).rejects.toBeInstanceOf(PermissionDeniedError);

    await logIn('admin@courseacademy.demo', 'Admin@123');
    await certificates.setRevoked('certificate-1', true, 'Plagiarism');
    expect(certificates.findByCode('CA-2026-K7Q2XM')).toMatchObject({ revoked: true, revokedReason: 'Plagiarism' });
  });
});
