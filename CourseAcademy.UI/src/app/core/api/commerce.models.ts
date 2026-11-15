import { Enrollment, EnrollmentStatus } from '../models/enrollment.model';
import { PaymentMethodId, PaymentStatus, PaymentTransaction } from '../payments/payment.model';

// TypeScript shapes of the API's payment and enrollment DTOs, and their conversion to the app's models.
// A payment's public transaction number becomes its `id` in the app, as in the demo data.

export interface TransactionDto {
  transactionNo: string;
  gateway: string;
  userId: number;
  userName: string;
  userEmail: string;
  courseId: number;
  courseSlug: string;
  courseTitle: string;
  methodCode: PaymentMethodId;
  maskedAccount: string | null;
  originalAmount: number;
  discountAmount: number;
  amount: number;
  currency: 'BDT';
  statusCode: PaymentStatus;
  failureReason: string | null;
  fulfilledAt: string | null;
  expiresAt: string;
  createdAt: string;
  updatedAt: string | null;
  /** Admin lists only: the statuses an admin may move this payment to. */
  allowedActions: PaymentStatus[];
  canOpenCourse: boolean;
}

/** Where to send the buyer: the provider's page (external) or a page in this app (demo gateway). */
export interface StartPaymentDto {
  transactionNo: string;
  redirectUrl: string;
  isExternal: boolean;
}

export interface EnrollmentDto {
  id: number;
  userId: number;
  studentName: string;
  studentEmail: string;
  courseId: number;
  courseTitle: string;
  statusCode: EnrollmentStatus;
  enrolledAt: string;
  transactionNo: string | null;
}

export function fromTransactionDto(t: TransactionDto): PaymentTransaction {
  return {
    id: t.transactionNo,
    gateway: t.gateway,
    userId: String(t.userId),
    userName: t.userName,
    userEmail: t.userEmail,
    courseId: t.courseId,
    courseSlug: t.courseSlug,
    courseTitle: t.courseTitle,
    method: t.methodCode,
    maskedAccount: t.maskedAccount ?? '',
    originalAmount: t.originalAmount,
    discountAmount: t.discountAmount,
    amount: t.amount,
    currency: 'BDT',
    status: t.statusCode,
    failureReason: t.failureReason ?? undefined,
    fulfilledAt: t.fulfilledAt ?? undefined,
    createdAt: t.createdAt,
    updatedAt: t.updatedAt ?? t.createdAt,
  };
}

export function fromEnrollmentDto(e: EnrollmentDto): Enrollment {
  return {
    id: String(e.id),
    userId: String(e.userId),
    courseId: e.courseId,
    status: e.statusCode,
    enrolledAt: e.enrolledAt,
    transactionId: e.transactionNo ?? undefined,
    studentName: e.studentName,
    studentEmail: e.studentEmail,
  };
}
