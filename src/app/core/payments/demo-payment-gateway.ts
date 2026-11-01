import { Injectable, inject } from '@angular/core';
import { TRANSACTION_REPOSITORY } from '../data/repositories';
import { PaymentGateway, PaymentRequest, PaymentSession } from './payment-gateway';
import { PaymentStatus, PaymentTransaction, paymentMethod } from './payment.model';

export type DemoOutcome = Extract<PaymentStatus, 'success' | 'failed' | 'cancelled'>;

/** Stands in for a real gateway. No money moves and no provider is contacted. */
@Injectable({ providedIn: 'root' })
export class DemoPaymentGateway extends PaymentGateway {
  private readonly repository = inject(TRANSACTION_REPOSITORY);

  readonly name = 'demo';

  async initiate(request: PaymentRequest): Promise<PaymentSession> {
    const now = new Date().toISOString();
    const transaction = await this.repository.save({
      ...request,
      id: newTransactionId(),
      gateway: this.name,
      maskedAccount: paymentMethod(request.method).maskedAccount,
      currency: 'BDT',
      status: 'pending',
      createdAt: now,
      updatedAt: now,
    });
    return { transaction, redirectUrl: `/checkout/pay/${transaction.id}` };
  }

  async transaction(id: string): Promise<PaymentTransaction | undefined> {
    return (await this.repository.list()).find((t) => t.id === id);
  }

  /** The decision a real gateway would make, chosen here on the simulated payment page. */
  async simulate(id: string, outcome: DemoOutcome): Promise<PaymentTransaction> {
    const transaction = await this.transaction(id);
    if (!transaction) throw new Error('Transaction not found.');
    if (transaction.status !== 'pending') return transaction;

    return this.repository.save({
      ...transaction,
      status: outcome,
      failureReason: outcome === 'failed' ? paymentMethod(transaction.method).failureReason : undefined,
      updatedAt: new Date().toISOString(),
    });
  }
}

function newTransactionId(): string {
  const date = new Date().toISOString().slice(0, 10).replaceAll('-', '');
  const suffix = Math.random().toString(36).slice(2, 8).toUpperCase();
  return `DEMO-${date}-${suffix}`;
}
