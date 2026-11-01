import { TestBed } from '@angular/core/testing';
import { localStore, sessionStore } from '../data/browser-storage';
import { COURSES } from '../data/courses.data';
import { AuthService } from '../services/auth.service';
import { EnrollmentService } from '../services/enrollment.service';
import { DemoPaymentGateway } from './demo-payment-gateway';
import { PaymentGateway } from './payment-gateway';
import { DEFAULT_TRANSACTION_FILTER, PaymentTransaction, filterTransactions } from './payment.model';
import { PaymentService } from './payment.service';

describe('PaymentService', () => {
  let payments: PaymentService;
  let gateway: DemoPaymentGateway;
  let enrollments: EnrollmentService;
  const course = COURSES[1];

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [{ provide: PaymentGateway, useExisting: DemoPaymentGateway }],
    });
    payments = TestBed.inject(PaymentService);
    gateway = TestBed.inject(DemoPaymentGateway);
    enrollments = TestBed.inject(EnrollmentService);
    const auth = TestBed.inject(AuthService);
    await Promise.all([payments.ready, enrollments.ready, auth.ready]);
    vi.useFakeTimers({ toFake: ['setTimeout'] });
    const login = auth.login('student@courseacademy.demo', 'Student@123');
    await vi.advanceTimersByTimeAsync(600);
    await login;
    vi.useRealTimers();
  });

  async function startPayment(): Promise<string> {
    const url = await payments.checkout(course, 'bkash');
    return url.split('/').pop()!;
  }

  it('records a pending transaction with the price and masked test account', async () => {
    const id = await startPayment();
    const t = payments.find(id)!;
    expect(t.status).toBe('pending');
    expect(t.amount).toBe(course.price);
    expect(t.discountAmount).toBe(course.originalPrice - course.price);
    expect(t.maskedAccount).toContain('•');
    expect(enrollments.isEnrolled(course.id)).toBe(false);
  });

  it('activates the course after a successful payment', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'success');
    await payments.confirm(id);
    expect(payments.find(id)?.status).toBe('success');
    expect(enrollments.isEnrolled(course.id)).toBe(true);
  });

  it.each(['failed', 'cancelled'] as const)('does not grant access after a %s payment', async (outcome) => {
    const id = await startPayment();
    await gateway.simulate(id, outcome);
    await payments.confirm(id);
    expect(payments.find(id)?.status).toBe(outcome);
    expect(enrollments.isEnrolled(course.id)).toBe(false);
  });

  it('cannot change a payment that has already finished', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'failed');
    await gateway.simulate(id, 'success');
    expect((await gateway.transaction(id))?.status).toBe('failed');
  });

  it('removes access on refund and does not restore it when the receipt is reopened', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'success');
    await payments.confirm(id);

    await payments.setStatus(id, 'refunded');
    await payments.confirm(id);
    expect(enrollments.isEnrolled(course.id)).toBe(false);
  });

  it('does not undo an admin revocation when the receipt is reopened', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'success');
    await payments.confirm(id);
    const enrollment = enrollments.mine().find((e) => e.courseId === course.id)!;

    await enrollments.setStatus(enrollment.id, 'revoked');
    await payments.confirm(id);
    expect(enrollments.isEnrolled(course.id)).toBe(false);
  });

  it('does not re-enroll anyone in a deleted course when the receipt is reopened', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'success');
    await payments.confirm(id);

    await enrollments.removeForCourse(course.id);
    await payments.confirm(id);
    expect(enrollments.countAllForCourse(course.id)).toBe(0);
  });

  it('rejects status changes an admin is not allowed to make', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'failed');
    await payments.confirm(id);
    await expect(payments.setStatus(id, 'refunded')).rejects.toThrow();
  });

  it('keeps transactions after a refresh', async () => {
    const id = await startPayment();
    await gateway.simulate(id, 'success');
    const raw = localStore.getItem('ca-transactions')!;
    expect(raw).toContain(id);
    expect(raw).not.toContain('4242 4242 4242 4242');
  });
});

describe('filterTransactions', () => {
  const base: PaymentTransaction = {
    id: 'DEMO-A',
    gateway: 'demo',
    userId: 'u1',
    userName: 'Rahim',
    userEmail: 'rahim@example.com',
    courseId: 1,
    courseSlug: 'one',
    courseTitle: 'Course One',
    method: 'bkash',
    maskedAccount: '',
    originalAmount: 100,
    discountAmount: 0,
    amount: 100,
    currency: 'BDT',
    status: 'success',
    createdAt: '2026-10-01T06:00:00.000Z',
    updatedAt: '2026-10-01T06:00:00.000Z',
  };
  const items: PaymentTransaction[] = [
    base,
    { ...base, id: 'DEMO-B', method: 'card', status: 'failed', courseId: 2, courseTitle: 'Course Two', userName: 'Karim', createdAt: '2026-10-10T06:00:00.000Z' },
  ];
  const run = (patch: object) => filterTransactions(items, { ...DEFAULT_TRANSACTION_FILTER, ...patch }).map((t) => t.id);

  it('returns everything, newest first, by default', () => {
    expect(run({})).toEqual(['DEMO-B', 'DEMO-A']);
  });

  it('filters by status, method and course', () => {
    expect(run({ status: 'failed' })).toEqual(['DEMO-B']);
    expect(run({ method: 'bkash' })).toEqual(['DEMO-A']);
    expect(run({ courseId: 2 })).toEqual(['DEMO-B']);
  });

  it('searches id, student and course', () => {
    expect(run({ search: 'karim' })).toEqual(['DEMO-B']);
    expect(run({ search: 'course one' })).toEqual(['DEMO-A']);
    expect(run({ search: 'demo-a' })).toEqual(['DEMO-A']);
  });

  it('applies inclusive date bounds', () => {
    expect(run({ from: '2026-10-05' })).toEqual(['DEMO-B']);
    expect(run({ to: '2026-10-05' })).toEqual(['DEMO-A']);
  });
});
