import { Injectable, computed, inject } from '@angular/core';
import { TRANSACTION_REPOSITORY } from '../data/repositories';
import { EntityStore } from '../data/repository';
import { Course } from '../models/course.model';
import { AuthService } from '../services/auth.service';
import { EnrollmentService } from '../services/enrollment.service';
import { PaymentGateway } from './payment-gateway';
import { PaymentMethodId, PaymentStatus, PaymentTransaction } from './payment.model';

/** Status changes an admin may make. Anything else is rejected. */
const ADMIN_TRANSITIONS: Record<PaymentStatus, PaymentStatus[]> = {
  pending: ['success', 'cancelled'],
  success: ['refunded'],
  failed: [],
  cancelled: [],
  refunded: [],
};

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly store = new EntityStore(inject(TRANSACTION_REPOSITORY));
  private readonly gateway = inject(PaymentGateway);
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);

  readonly ready = this.store.ready;
  readonly recovered = this.store.recovered;
  readonly transactions = this.store.items;

  readonly mine = computed(() => {
    const id = this.auth.user()?.id;
    return this.transactions()
      .filter((t) => t.userId === id)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  });

  find(id: string): PaymentTransaction | undefined {
    return this.transactions().find((t) => t.id === id);
  }

  /** Starts a payment for the signed-in user and returns where to send them. */
  async checkout(course: Course, method: PaymentMethodId): Promise<string> {
    const user = this.auth.user();
    if (!user) throw new Error('Please log in to continue.');

    // A real API must work out the amount itself. Figures from the browser are only for display.
    const session = await this.gateway.initiate({
      userId: user.id,
      userName: user.name,
      userEmail: user.email,
      courseId: course.id,
      courseSlug: course.slug,
      courseTitle: course.title,
      method,
      originalAmount: course.originalPrice,
      discountAmount: course.originalPrice - course.price,
      amount: course.price,
    });
    await this.store.reload();
    return session.redirectUrl;
  }

  /**
   * Reads the latest status from the gateway and grants access if it succeeded.
   * Safe to repeat, e.g. when a receipt is reopened. In production the API would
   * grant access when it verifies the gateway callback, and this would only read.
   */
  async confirm(id: string): Promise<PaymentTransaction | undefined> {
    await this.store.reload();
    const transaction = await this.gateway.transaction(id);
    return transaction?.status === 'success' ? this.fulfil(transaction) : transaction;
  }

  /**
   * Grants access once per payment, so reopening a receipt can't undo an admin's
   * revocation or re-enroll someone in a course that has since been deleted.
   */
  private async fulfil(transaction: PaymentTransaction): Promise<PaymentTransaction> {
    if (transaction.fulfilledAt) return transaction;
    await this.enrollments.grant(transaction.userId, transaction.courseId, transaction.id);
    return this.store.save({ ...transaction, fulfilledAt: new Date().toISOString() });
  }

  allowedActions(transaction: PaymentTransaction): PaymentStatus[] {
    return ADMIN_TRANSITIONS[transaction.status];
  }

  /** Admin action. Keeps course access in step with the payment. */
  async setStatus(id: string, status: PaymentStatus): Promise<PaymentTransaction> {
    const transaction = this.find(id);
    if (!transaction) throw new Error('Transaction not found.');
    if (!ADMIN_TRANSITIONS[transaction.status].includes(status)) {
      throw new Error('This change is not allowed for this transaction.');
    }

    const updated = await this.store.save({ ...transaction, status, updatedAt: new Date().toISOString() });
    if (status === 'success') return this.fulfil(updated);
    if (status === 'refunded') await this.enrollments.revokeForTransaction(updated.id);
    return updated;
  }

  /** Successful payments are kept as a record: refund first if one must be removed. */
  canDelete(transaction: PaymentTransaction): boolean {
    return transaction.status !== 'success';
  }

  remove(id: string): Promise<void> {
    return this.store.remove(id);
  }

  reset(): Promise<void> {
    return this.store.reset();
  }
}
