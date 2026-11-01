import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  DEFAULT_TRANSACTION_FILTER,
  PAYMENT_METHODS,
  PAYMENT_STATUSES,
  PaymentMethodId,
  PaymentStatus,
  PaymentTransaction,
  TransactionFilter,
  filterTransactions,
  paymentMethod,
} from '../../core/payments/payment.model';
import { PaymentService } from '../../core/payments/payment.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { ToastService } from '../../core/services/toast.service';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';

const ACTIONS: Partial<Record<PaymentStatus, { label: string; title: string; message: string; danger: boolean }>> = {
  success: {
    label: 'Mark as paid',
    title: 'Mark this payment as paid?',
    message: 'The student gets access to the course straight away.',
    danger: false,
  },
  cancelled: {
    label: 'Cancel payment',
    title: 'Cancel this payment?',
    message: 'The record stays in the list as cancelled.',
    danger: true,
  },
  refunded: {
    label: 'Refund',
    title: 'Refund this payment?',
    message: 'This is simulated, so no money moves. The student loses access to the course.',
    danger: true,
  },
};

@Component({
  selector: 'app-admin-transactions',
  imports: [DatePipe, FormsModule, RouterLink, BdtPipe, StatusPill],
  templateUrl: './admin-transactions.html',
})
export class AdminTransactions {
  protected readonly payments = inject(PaymentService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly methods = PAYMENT_METHODS;
  protected readonly statuses = PAYMENT_STATUSES;
  protected readonly filter = signal<TransactionFilter>(DEFAULT_TRANSACTION_FILTER);
  protected readonly detailId = signal<string | null>(null);

  /** Courses that appear in records, including deleted ones. */
  protected readonly courseOptions = computed(() => {
    const options = new Map<number, string>();
    for (const t of this.payments.transactions()) options.set(t.courseId, t.courseTitle);
    return [...options].map(([id, title]) => ({ id, title }));
  });
  protected readonly results = computed(() => filterTransactions(this.payments.transactions(), this.filter()));
  protected readonly isFiltered = computed(() => this.filter() !== DEFAULT_TRANSACTION_FILTER);

  /** Totals follow the active filters, so a narrowed view reports on itself. */
  protected readonly totals = computed(() => {
    const items = this.results();
    const sum = (status: PaymentStatus) =>
      items.filter((t) => t.status === status).reduce((total, t) => total + t.amount, 0);
    return {
      collected: sum('success'),
      refunded: sum('refunded'),
      successful: items.filter((t) => t.status === 'success').length,
      unsuccessful: items.filter((t) => t.status === 'failed' || t.status === 'cancelled').length,
    };
  });

  protected update(patch: Partial<TransactionFilter>): void {
    this.filter.update((f) => ({ ...f, ...patch }));
  }

  protected clearFilters(): void {
    this.filter.set(DEFAULT_TRANSACTION_FILTER);
  }

  protected methodName(id: PaymentMethodId): string {
    return paymentMethod(id).name;
  }

  protected actionLabel(status: PaymentStatus): string {
    return ACTIONS[status]?.label ?? status;
  }

  protected toggleDetails(id: string): void {
    this.detailId.update((current) => (current === id ? null : id));
  }

  protected async apply(transaction: PaymentTransaction, status: PaymentStatus): Promise<void> {
    const action = ACTIONS[status];
    if (!action) return;
    const confirmed = await this.confirm.ask({
      title: action.title,
      message: `${transaction.id}: ${action.message}`,
      confirmLabel: action.label,
      danger: action.danger,
    });
    if (!confirmed) return;

    try {
      await this.payments.setStatus(transaction.id, status);
      this.toast.success(`${transaction.id} is now ${status}.`);
    } catch (error) {
      this.toast.error((error as Error).message);
    }
  }

  protected async remove(transaction: PaymentTransaction): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete this record?',
      message: `${transaction.id} will be removed from the list. This cannot be undone, except by resetting all demo data.`,
      confirmLabel: 'Delete record',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await this.payments.remove(transaction.id);
      this.toast.success('The record was deleted.');
    } catch {
      this.toast.error('The record could not be deleted. Browser storage may be full or blocked.');
    }
  }
}
