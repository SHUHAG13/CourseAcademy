import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DemoOutcome, DemoPaymentGateway } from '../../core/payments/demo-payment-gateway';
import { paymentMethod } from '../../core/payments/payment.model';
import { PaymentService } from '../../core/payments/payment.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';

/**
 * Stands in for the hosted page a real gateway would show. With a real provider
 * the buyer is redirected away from this site instead, so this page is removed.
 * It takes no card, wallet or PIN input on purpose: only fixed test details appear.
 */
@Component({
  selector: 'app-demo-gateway',
  imports: [RouterLink, BdtPipe, EmptyState],
  templateUrl: './demo-gateway.html',
  styleUrl: './demo-gateway.scss',
})
export class DemoGateway {
  private readonly gateway = inject(DemoPaymentGateway);
  private readonly payments = inject(PaymentService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Bound from the `:id` route param. */
  readonly id = input.required<string>();

  protected readonly processing = signal<DemoOutcome | null>(null);

  /** Only the buyer can see or complete their own payment. */
  protected readonly transaction = computed(() => {
    const t = this.payments.find(this.id());
    return t && t.userId === this.auth.user()?.id ? t : undefined;
  });
  protected readonly method = computed(() => {
    const t = this.transaction();
    return t ? paymentMethod(t.method) : undefined;
  });

  constructor() {
    // Going back to this page after finishing shows the result, not a second payment.
    effect(() => {
      const t = this.transaction();
      if (t && t.status !== 'pending' && !this.processing()) {
        this.router.navigate(['/checkout/result', t.id], { replaceUrl: true });
      }
    });
  }

  protected async finish(outcome: DemoOutcome): Promise<void> {
    if (this.processing()) return;
    this.processing.set(outcome);
    try {
      await new Promise((r) => setTimeout(r, outcome === 'cancelled' ? 300 : 1200));
      await this.gateway.simulate(this.id(), outcome);
      await this.payments.confirm(this.id());
      await this.router.navigate(['/checkout/result', this.id()], { replaceUrl: true });
    } catch {
      this.processing.set(null);
      this.toast.error('The payment could not be recorded. Browser storage may be full or blocked.');
    }
  }
}
