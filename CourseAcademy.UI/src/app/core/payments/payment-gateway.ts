import { PaymentMethodId, PaymentTransaction } from './payment.model';

export interface PaymentRequest {
  userId: string;
  userName: string;
  userEmail: string;
  courseId: number;
  courseSlug: string;
  courseTitle: string;
  method: PaymentMethodId;
  originalAmount: number;
  discountAmount: number;
  amount: number;
}

export interface PaymentSession {
  transaction: PaymentTransaction;
  /** Where to send the buyer next: a hosted gateway page in production. */
  redirectUrl: string;
}

/**
 * The only part of the payment module that knows who processes a payment.
 *
 * To go live, provide an implementation that calls the ASP.NET Core Web API:
 *
 *   initiate()    POST /api/payments        The API re-reads the price from its own
 *                                           database, starts an SSLCOMMERZ, aamarPay or
 *                                           bKash session and returns the hosted page URL.
 *   transaction() GET  /api/payments/{id}   Returns the status the API has verified.
 *
 * Secret keys, gateway signatures and IPN/callback verification belong on the server
 * and must never be added to this frontend. Anything sent from the browser, including
 * amounts and statuses, can be altered by the user, so it must not be trusted.
 */
export abstract class PaymentGateway {
  abstract readonly name: string;
  abstract initiate(request: PaymentRequest): Promise<PaymentSession>;
  abstract transaction(id: string): Promise<PaymentTransaction | undefined>;
}
