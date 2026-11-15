import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { Course } from '../models/course.model';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { PaymentService } from '../payments/payment.service';
import { AuthService } from '../services/auth.service';
import { API_BASE_URL, API_MODE } from './api-mode';
import { MeDto } from './auth.models';
import { TransactionDto } from './commerce.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

const me: MeDto = {
  id: 2, name: 'Demo Student', email: 'student@courseacademy.demo', phone: null, roleCode: 'user', roleName: 'Student',
  permissions: ['dashboard.view', 'learn.view', 'checkout.view'], courseGrants: [], hasAdminPanel: false, homeUrl: '/dashboard',
  createdAt: '2026-11-01T00:00:00Z',
};

const transaction = (status: TransactionDto['statusCode']): TransactionDto => ({
  transactionNo: 'DEMO-20261115-ABC123', gateway: 'demo', userId: 2, userName: 'Demo Student', userEmail: me.email,
  courseId: 2, courseSlug: 'marketing', courseTitle: 'Marketing', methodCode: 'nagad', maskedAccount: '018•••••000',
  originalAmount: 9000, discountAmount: 3001, amount: 5999, currency: 'BDT', statusCode: status, failureReason: null,
  fulfilledAt: status === 'success' ? '2026-11-15T10:00:00Z' : null, expiresAt: inOneHour(), createdAt: '2026-11-15T09:50:00Z',
  updatedAt: null, allowedActions: [], canOpenCourse: status === 'success',
});

describe('Payments in API mode', () => {
  let http: HttpTestingController;
  let payments: PaymentService;

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
    payments = TestBed.inject(PaymentService);
    await settle();
    // Start-up loads of the catalogue and the user's lists.
    for (const r of http.match(() => true)) r.flush(ok([]));
    await settle();
    for (const r of http.match(() => true)) r.flush(ok([]));
  });

  afterEach(() => http.verify());

  it('sends only the course and the method, never an amount, and follows external pages', async () => {
    const course = { slug: 'marketing', price: 1 } as Course;

    const started = payments.checkout(course, 'nagad');
    const request = http.expectOne(`${BASE}/payments`);
    expect(request.request.body).toEqual({ courseSlug: 'marketing', methodCode: 'nagad' });
    request.flush(ok({ transactionNo: 'SSL-1', redirectUrl: 'https://pay.example.com/s/1', isExternal: true }));
    await settle();
    http.expectOne(`${BASE}/payments/mine`).flush(ok([transaction('pending')]));

    expect(await started).toEqual({ url: 'https://pay.example.com/s/1', isExternal: true });
    expect(payments.mine()[0].id).toBe('DEMO-20261115-ABC123');
  });

  it('reports the demo outcome to the API and refreshes access after a success', async () => {
    const done = payments.completeDemoPayment('DEMO-20261115-ABC123', 'success');

    const request = http.expectOne(`${BASE}/payments/DEMO-20261115-ABC123/demo-outcome`);
    expect(request.request.body).toEqual({ outcome: 'success' });
    request.flush(ok(transaction('success')));

    const result = await done;
    expect(result?.status).toBe('success');
    expect(payments.mine()[0].status).toBe('success');
    await settle();
    // The new enrollment opens the course, so enrollments and the catalogue are reloaded.
    expect(http.match(`${BASE}/enrollments/mine`).length).toBe(1);
    expect(http.match(`${BASE}/courses/all`).length).toBe(1);
    for (const r of http.match(() => true)) r.flush(ok([]));
  });
});
