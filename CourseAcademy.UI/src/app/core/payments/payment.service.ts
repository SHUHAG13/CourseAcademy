import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import { StartPaymentDto, TransactionDto, fromTransactionDto } from '../api/commerce.models';
import { TRANSACTION_REPOSITORY } from '../data/repositories';
import { EntityStore } from '../data/repository';
import { Course } from '../models/course.model';
import { AuthService } from '../services/auth.service';
import { CourseService } from '../services/course.service';
import { EnrollmentService } from '../services/enrollment.service';
import { PermissionService } from '../services/permission.service';
import { DemoOutcome, DemoPaymentGateway } from './demo-payment-gateway';
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

const PAGE_SIZE = 100;

/** Where to send the buyer after starting a payment. External URLs are the payment provider's own page. */
export interface PaymentRedirect {
  url: string;
  isExternal: boolean;
}

/**
 * Payments.
 * - API mode: the API takes the price from the course, talks to the configured gateway and grants access
 *   once the gateway confirms. This class only shows and forwards.
 * - Demo mode: payments live in browser storage and the demo gateway decides the outcome.
 */
@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly store = new EntityStore(inject(TRANSACTION_REPOSITORY));
  private readonly gateway = inject(PaymentGateway);
  private readonly demoGateway = inject(DemoPaymentGateway);
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);
  private readonly courses = inject(CourseService);
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);

  // API mode: the buyer's own payments, and every payment for people who manage them.
  private readonly apiMine = signal<PaymentTransaction[]>([]);
  private readonly apiAll = signal<PaymentTransaction[] | null>(null);

  readonly ready: Promise<unknown> = this.apiMode ? this.auth.ready.then(() => this.load()) : this.store.ready;
  readonly recovered = computed(() => !this.apiMode && this.store.recovered());
  /** Every payment this user may see: all of them for staff, otherwise their own. */
  readonly transactions = computed(() => (this.apiMode ? (this.apiAll() ?? this.apiMine()) : this.store.items()));

  readonly mine = computed(() => {
    if (this.apiMode) return this.apiMine();
    const id = this.auth.user()?.id;
    return this.transactions()
      .filter((t) => t.userId === id)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  });

  constructor() {
    if (this.apiMode) {
      let loadedFor: number | null | undefined;
      effect(() => {
        const id = this.auth.currentSession()?.id ?? null;
        untracked(() => {
          if (loadedFor !== undefined && loadedFor !== id) void this.load();
          loadedFor = id;
        });
      });
    }
  }

  find(id: string): PaymentTransaction | undefined {
    return this.transactions().find((t) => t.id === id) ?? this.apiMine().find((t) => t.id === id);
  }

  /** API mode: reloads the user's payments, and every payment for people who may see them. */
  async load(): Promise<void> {
    if (!this.apiMode) return;
    if (!this.auth.currentSession()) {
      this.apiMine.set([]);
      this.apiAll.set(null);
      return;
    }
    try {
      this.apiMine.set((await this.api.get<TransactionDto[]>('payments/mine')).map(fromTransactionDto));
      if (this.permissions.can('transactions.view')) {
        const all: PaymentTransaction[] = [];
        for (let page = 1; ; page++) {
          const result = await this.api.get<{ items: TransactionDto[]; total: number }>('admin/transactions', { page, pageSize: PAGE_SIZE });
          all.push(...result.items.map(fromTransactionDto));
          if (all.length >= result.total || result.items.length === 0) break;
        }
        this.apiAll.set(all);
      } else {
        this.apiAll.set(null);
      }
    } catch {
      // Leave the lists as they were; the next load tries again.
    }
  }

  /** Starts a payment for the signed-in user and returns where to send them. */
  async checkout(course: Course, method: PaymentMethodId): Promise<PaymentRedirect> {
    const user = this.auth.user();
    if (!user) throw new Error('Please log in to continue.');

    if (this.apiMode) {
      // The server reads the price itself; only the course and the method are sent.
      const started = await this.api.post<StartPaymentDto>('payments', { courseSlug: course.slug, methodCode: method });
      await this.load();
      return { url: started.redirectUrl, isExternal: started.isExternal };
    }

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
    return { url: session.redirectUrl, isExternal: false };
  }

  /**
   * Reads the latest status and, in demo mode, grants access if it succeeded. Safe to repeat, e.g. when a
   * receipt is reopened. In API mode the server has already granted access when it verified the payment.
   */
  async confirm(id: string): Promise<PaymentTransaction | undefined> {
    if (this.apiMode) {
      return this.storeApiTransaction(await this.api.get<TransactionDto>(`payments/${encodeURIComponent(id)}`));
    }
    await this.store.reload();
    const transaction = await this.gateway.transaction(id);
    return transaction?.status === 'success' ? this.fulfil(transaction) : transaction;
  }

  /** The outcome picked on the simulated payment page. */
  async completeDemoPayment(id: string, outcome: DemoOutcome): Promise<PaymentTransaction | undefined> {
    if (this.apiMode) {
      return this.storeApiTransaction(
        await this.api.post<TransactionDto>(`payments/${encodeURIComponent(id)}/demo-outcome`, { outcome }),
      );
    }
    await this.demoGateway.simulate(id, outcome);
    return this.confirm(id);
  }

  /**
   * Demo mode: grants access once per payment, so reopening a receipt can't undo an admin's
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
    this.permissions.assert('transactions.edit');
    const transaction = this.find(id);
    if (!transaction) throw new Error('Transaction not found.');
    if (!ADMIN_TRANSITIONS[transaction.status].includes(status)) {
      throw new Error('This change is not allowed for this transaction.');
    }

    if (this.apiMode) {
      const updated = this.storeApiTransaction(
        await this.api.patch<TransactionDto>(`admin/transactions/${encodeURIComponent(id)}/status`, { statusCode: status }),
      );
      // Marking paid or refunding changes who has access.
      await Promise.all([this.enrollments.load(), this.courses.loadCatalog()]);
      return updated;
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

  async remove(id: string): Promise<void> {
    this.permissions.assert('transactions.delete');
    if (this.apiMode) {
      await this.api.delete(`admin/transactions/${encodeURIComponent(id)}`);
      this.apiAll.update((list) => list?.filter((t) => t.id !== id) ?? null);
      this.apiMine.update((list) => list.filter((t) => t.id !== id));
      return;
    }
    return this.store.remove(id);
  }

  reset(): Promise<void> {
    // Payments in API mode live on the server; demo reset does not touch them.
    return this.apiMode ? Promise.resolve() : this.store.reset();
  }

  /** Puts a payment returned by the API into the lists; a fresh success also refreshes access. */
  private storeApiTransaction(dto: TransactionDto): PaymentTransaction {
    const transaction = fromTransactionDto(dto);
    const upsert = (list: PaymentTransaction[]) =>
      list.some((t) => t.id === transaction.id) ? list.map((t) => (t.id === transaction.id ? transaction : t)) : [transaction, ...list];
    if (transaction.userId === this.auth.user()?.id) this.apiMine.update(upsert);
    this.apiAll.update((list) => (list ? upsert(list) : null));
    if (transaction.status === 'success' && transaction.userId === this.auth.user()?.id && !this.enrollments.isEnrolled(transaction.courseId)) {
      void Promise.all([this.enrollments.load(), this.courses.loadCatalog()]);
    }
    return transaction;
  }
}
