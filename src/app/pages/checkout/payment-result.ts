import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PaymentStatus, paymentMethod } from '../../core/payments/payment.model';
import { PaymentService } from '../../core/payments/payment.service';
import { AuthService } from '../../core/services/auth.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';

const OUTCOMES: Record<PaymentStatus, { icon: string; tone: string; title: string; message: string }> = {
  success: {
    icon: 'bi-check-circle-fill',
    tone: 'success',
    title: 'Payment successful',
    message: 'Your course is ready. You can start learning right away.',
  },
  failed: {
    icon: 'bi-x-circle-fill',
    tone: 'failed',
    title: 'Payment failed',
    message: 'The payment did not go through and you have not been charged. You can try again.',
  },
  cancelled: {
    icon: 'bi-slash-circle-fill',
    tone: 'cancelled',
    title: 'Payment cancelled',
    message: 'You cancelled this payment and have not been charged. You can try again whenever you like.',
  },
  pending: {
    icon: 'bi-hourglass-split',
    tone: 'pending',
    title: 'Payment not finished',
    message: 'This payment was started but not completed.',
  },
  refunded: {
    icon: 'bi-arrow-counterclockwise',
    tone: 'cancelled',
    title: 'Payment refunded',
    message: 'This demo payment was refunded by an admin, and course access has ended.',
  },
};

/** Final step of checkout, and the receipt reopened from the dashboard. */
@Component({
  selector: 'app-payment-result',
  imports: [DatePipe, RouterLink, BdtPipe, EmptyState, StatusPill],
  templateUrl: './payment-result.html',
  styleUrl: './payment-result.scss',
})
export class PaymentResult {
  private readonly payments = inject(PaymentService);
  protected readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly toast = inject(ToastService);

  /** Bound from the `:id` route param. */
  readonly id = input.required<string>();

  protected readonly loading = signal(true);

  /** Visible to the buyer and to admins only. */
  protected readonly transaction = computed(() => {
    const t = this.payments.find(this.id());
    return t && (t.userId === this.auth.user()?.id || this.auth.isAdmin()) ? t : undefined;
  });
  protected readonly outcome = computed(() => {
    const t = this.transaction();
    return t ? OUTCOMES[t.status] : undefined;
  });
  protected readonly methodName = computed(() => {
    const t = this.transaction();
    return t ? paymentMethod(t.method).name : '';
  });
  /** False if an admin has since revoked access, so the page doesn't offer a dead link. */
  protected readonly canOpenCourse = computed(() => {
    const t = this.transaction();
    return !!t && t.status === 'success' && this.enrollments.hasAccess(t.courseId);
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => this.load(id));
    });
  }

  private async load(id: string): Promise<void> {
    this.loading.set(true);
    try {
      await this.payments.confirm(id);
    } catch {
      this.toast.error('We could not update your enrollment. Browser storage may be full or blocked.');
    } finally {
      this.loading.set(false);
    }
  }
}
