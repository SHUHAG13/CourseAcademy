import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { Course } from '../models/course.model';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from '../services/auth.service';
import { CertificateService } from '../services/certificate.service';
import { API_BASE_URL, API_MODE } from './api-mode';
import { MeDto } from './auth.models';
import { CertificateDto, CertificationStatusDto, RuleDto, SubmissionDto } from './certification.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

const me: MeDto = {
  id: 2, name: 'Demo Student', email: 'student@courseacademy.demo', phone: null, roleCode: 'user', roleName: 'Student',
  permissions: ['dashboard.view', 'learn.view', 'checkout.view'], courseGrants: [], hasAdminPanel: false, homeUrl: '/dashboard',
  createdAt: '2026-11-01T00:00:00Z',
};

const course = { id: 5, slug: 'photo' } as unknown as Course;

const rule: RuleDto = {
  courseId: 5, isEnabled: true, modeCode: 'exam', minProgress: 100, examTypeCode: 'project', examTitle: 'Final edit',
  examInstructions: '', maxScore: 100, passingScore: 60, maxAttempts: 2,
};

const certificate = (over: Partial<CertificateDto> = {}): CertificateDto => ({
  id: 9, code: 'CA-2026-K7Q2XM', userId: 2, courseId: 5, userName: 'Demo Student', courseTitle: 'Photography',
  instructorName: 'Tanvir Hasan', modeCode: 'exam', examTypeCode: 'project', scorePercent: 82, issuedAt: '2026-11-17T09:00:00Z',
  isRevoked: false, revokedReason: null, ...over,
});

const submission: SubmissionDto = {
  id: 3, userId: 2, userName: 'Demo Student', courseId: 5, courseTitle: 'Photography', examTypeCode: 'project', examTitle: 'Final edit',
  maxScore: 100, passingScore: 60, url: 'https://example.com/work', notes: '', submittedAt: '2026-11-18T09:00:00Z',
  statusCode: 'submitted', score: null, feedback: null, reviewedBy: null, reviewedAt: null,
};

describe('Certificates in API mode', () => {
  let http: HttpTestingController;
  let service: CertificateService;

  const settle = () => new Promise((r) => setTimeout(r, 0));

  /** Answers whatever the services load in the background. */
  async function flushBackground(): Promise<void> {
    for (let round = 0; round < 3; round++) {
      await settle();
      for (const r of http.match(() => true)) r.flush(ok([]));
    }
  }

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
    service = TestBed.inject(CertificateService);
    await flushBackground();
  });

  afterEach(() => http.verify());

  it('shows the learner status from the server, including a revoked certificate', async () => {
    const loading = service.loadStatus(course);
    const status: CertificationStatusDto = {
      rule, progress: 100, progressMet: true, submissions: [{ ...submission, statusCode: 'passed', score: 82 }], attemptsLeft: 1,
      canSubmit: false, certificate: null, revokedCertificate: certificate({ isRevoked: true, revokedReason: 'Copied work' }),
    };
    http.expectOne(`${BASE}/me/courses/5/certification`).flush(ok(status));
    await loading;

    const shown = service.status(course)!;
    expect(shown.rule).toMatchObject({ id: 5, mode: 'exam', examTitle: 'Final edit', maxAttempts: 2 });
    expect(shown.submissions[0]).toMatchObject({ id: '3', status: 'passed', score: 82, exam: { maxScore: 100, passingScore: 60 } });
    expect(shown.certificate).toBeUndefined();
    expect(shown.revokedCertificate).toMatchObject({ code: 'CA-2026-K7Q2XM', revoked: true, revokedReason: 'Copied work' });
    expect(service.rule(5).examTitle).toBe('Final edit');
  });

  it('verifies a code on the server and treats an unknown code as not found', async () => {
    const found = service.verify('  ca-2026-k7q2xm ');
    http.expectOne(`${BASE}/certificates/verify/CA-2026-K7Q2XM`).flush(ok({
      code: 'CA-2026-K7Q2XM', isRevoked: false, revokedReason: null, userName: 'Sample Learner', courseTitle: 'Photography',
      modeCode: 'exam', examTypeCode: 'project', scorePercent: 82, instructorName: 'Tanvir Hasan', issuedAt: '2026-11-17T09:00:00Z',
    }));
    expect(await found).toMatchObject({ userName: 'Sample Learner', scorePercent: 82, revoked: false });

    const missing = service.verify('CA-2026-NOPE00');
    http.expectOne(`${BASE}/certificates/verify/CA-2026-NOPE00`)
      .flush({ success: false, data: null, message: 'Certificate not found.', errors: [] }, { status: 404, statusText: 'Not Found' });
    expect(await missing).toBeUndefined();
  });

  it('claims a completion certificate and then reloads the status and the dashboard list', async () => {
    const claiming = service.evaluate(course);
    http.expectOne((r) => r.method === 'POST' && r.url === `${BASE}/me/courses/5/certificate`).flush(ok(certificate({ modeCode: 'completion' })));
    await settle();
    http.expectOne(`${BASE}/me/courses/5/certification`).flush(ok({
      rule: { ...rule, modeCode: 'completion' }, progress: 100, progressMet: true, submissions: [], attemptsLeft: 2, canSubmit: false,
      certificate: certificate({ modeCode: 'completion' }), revokedCertificate: null,
    }));
    http.expectOne(`${BASE}/me/certificates`).flush(ok([certificate({ modeCode: 'completion' })]));

    expect((await claiming)?.mode).toBe('completion');
    expect(service.mine().map((c) => c.code)).toEqual(['CA-2026-K7Q2XM']);
  });
});
