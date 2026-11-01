import { AppUser } from '../models/user.model';
import { Enrollment } from '../models/enrollment.model';
import { PaymentTransaction } from '../payments/payment.model';

// Sample accounts and records so the dashboards have something to show on first run.

const DAY_MS = 86_400_000;
const daysAgo = (days: number) => new Date(Date.now() - days * DAY_MS).toISOString();

export const DEMO_ADMIN_CREDENTIALS = { email: 'admin@courseacademy.demo', password: 'Admin@123' };
export const DEMO_STUDENT_CREDENTIALS = { email: 'student@courseacademy.demo', password: 'Student@123' };

export const DEMO_USERS: AppUser[] = [
  {
    id: 'user-admin',
    name: 'Academy Admin',
    ...DEMO_ADMIN_CREDENTIALS,
    role: 'admin',
    createdAt: daysAgo(90),
  },
  {
    id: 'user-student',
    name: 'Demo Student',
    ...DEMO_STUDENT_CREDENTIALS,
    role: 'user',
    createdAt: daysAgo(30),
  },
  {
    id: 'user-sample',
    name: 'Sample Learner',
    email: 'learner@courseacademy.demo',
    password: 'Learner@123',
    role: 'user',
    createdAt: daysAgo(14),
  },
];

function sampleTransaction(
  id: string,
  days: number,
  user: AppUser,
  course: Pick<PaymentTransaction, 'courseId' | 'courseSlug' | 'courseTitle' | 'originalAmount' | 'amount'>,
  rest: Pick<PaymentTransaction, 'method' | 'maskedAccount' | 'status' | 'failureReason'>,
): PaymentTransaction {
  return {
    id,
    gateway: 'demo',
    userId: user.id,
    userName: user.name,
    userEmail: user.email,
    ...course,
    ...rest,
    discountAmount: course.originalAmount - course.amount,
    currency: 'BDT',
    fulfilledAt: rest.status === 'success' ? daysAgo(days) : undefined,
    createdAt: daysAgo(days),
    updatedAt: daysAgo(days),
  };
}

const VIDEO_COURSE = {
  courseId: 1,
  courseSlug: 'video-editing-post-production',
  courseTitle: 'Video Editing & Post-Production',
  originalAmount: 8000,
  amount: 4999,
};
const MARKETING_COURSE = {
  courseId: 2,
  courseSlug: 'digital-marketing-growth-strategy',
  courseTitle: 'Digital Marketing & Growth Strategy',
  originalAmount: 9000,
  amount: 5999,
};
const SPORTS_COURSE = {
  courseId: 3,
  courseSlug: 'sports-journalism-sports-media',
  courseTitle: 'Sports Journalism & Sports Media',
  originalAmount: 7000,
  amount: 3999,
};

export const DEMO_TRANSACTIONS: PaymentTransaction[] = [
  sampleTransaction('DEMO-SAMPLE-0001', 12, DEMO_USERS[1], VIDEO_COURSE, {
    method: 'bkash',
    maskedAccount: '017•••••000',
    status: 'success',
  }),
  sampleTransaction('DEMO-SAMPLE-0002', 9, DEMO_USERS[2], SPORTS_COURSE, {
    method: 'card',
    maskedAccount: '•••• •••• •••• 4242',
    status: 'failed',
    failureReason: 'Card declined by issuer (simulated)',
  }),
  sampleTransaction('DEMO-SAMPLE-0003', 8, DEMO_USERS[2], SPORTS_COURSE, {
    method: 'nagad',
    maskedAccount: '018•••••000',
    status: 'success',
  }),
  sampleTransaction('DEMO-SAMPLE-0004', 3, DEMO_USERS[2], MARKETING_COURSE, {
    method: 'rocket',
    maskedAccount: '019••••••00-0',
    status: 'cancelled',
  }),
];

export const DEMO_ENROLLMENTS: Enrollment[] = [
  {
    id: 'enrollment-1',
    userId: 'user-student',
    courseId: 1,
    status: 'active',
    enrolledAt: daysAgo(12),
    transactionId: 'DEMO-SAMPLE-0001',
  },
  {
    id: 'enrollment-2',
    userId: 'user-sample',
    courseId: 3,
    status: 'active',
    enrolledAt: daysAgo(8),
    transactionId: 'DEMO-SAMPLE-0003',
  },
];
