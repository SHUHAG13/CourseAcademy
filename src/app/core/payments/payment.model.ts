export type PaymentMethodId = 'bkash' | 'nagad' | 'rocket' | 'card';

export type PaymentStatus = 'pending' | 'success' | 'failed' | 'cancelled' | 'refunded';

export const PAYMENT_STATUSES: PaymentStatus[] = ['pending', 'success', 'failed', 'cancelled', 'refunded'];

export interface PaymentMethod {
  id: PaymentMethodId;
  name: string;
  description: string;
  /** Brand logo when the project has one, otherwise the icon is shown. */
  logo?: string;
  icon: string;
  /** Fixed test details shown by the simulated gateway. Never real credentials. */
  testAccountLabel: string;
  testAccount: string;
  maskedAccount: string;
  failureReason: string;
}

export const PAYMENT_METHODS: PaymentMethod[] = [
  {
    id: 'bkash',
    name: 'bKash',
    description: 'Pay from your bKash wallet.',
    logo: '/logos/bkash.jpg',
    icon: 'bi-wallet2',
    testAccountLabel: 'Test wallet',
    testAccount: '01700-000000',
    maskedAccount: '017•••••000',
    failureReason: 'Insufficient wallet balance (simulated)',
  },
  {
    id: 'nagad',
    name: 'Nagad',
    description: 'Pay from your Nagad wallet.',
    logo: '/logos/nagad.png',
    icon: 'bi-wallet2',
    testAccountLabel: 'Test wallet',
    testAccount: '01800-000000',
    maskedAccount: '018•••••000',
    failureReason: 'Insufficient wallet balance (simulated)',
  },
  {
    id: 'rocket',
    name: 'Rocket',
    description: 'Pay from your Rocket account.',
    icon: 'bi-rocket-takeoff',
    testAccountLabel: 'Test account',
    testAccount: '01900-000000-0',
    maskedAccount: '019••••••00-0',
    failureReason: 'Transaction limit exceeded (simulated)',
  },
  {
    id: 'card',
    name: 'Visa / Mastercard',
    description: 'Pay by debit or credit card.',
    icon: 'bi-credit-card-2-front',
    testAccountLabel: 'Test card',
    testAccount: '4242 4242 4242 4242',
    maskedAccount: '•••• •••• •••• 4242',
    failureReason: 'Card declined by issuer (simulated)',
  },
];

export function paymentMethod(id: PaymentMethodId): PaymentMethod {
  return PAYMENT_METHODS.find((m) => m.id === id)!;
}

export interface PaymentTransaction {
  id: string;
  /** Which gateway handled the payment, so real and demo records can coexist. */
  gateway: string;
  userId: string;
  userName: string;
  userEmail: string;
  // Course details are copied so the record outlives edits to, or deletion of, the course.
  courseId: number;
  courseSlug: string;
  courseTitle: string;
  method: PaymentMethodId;
  /** Masked test details only. Full card or wallet details are never stored. */
  maskedAccount: string;
  originalAmount: number;
  discountAmount: number;
  amount: number;
  currency: 'BDT';
  status: PaymentStatus;
  failureReason?: string;
  /** Set once course access has been granted, so it is only ever granted once. */
  fulfilledAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface TransactionFilter {
  search: string;
  status: PaymentStatus | 'all';
  method: PaymentMethodId | 'all';
  courseId: number | 'all';
  /** Inclusive `YYYY-MM-DD` bounds. Empty means no limit. */
  from: string;
  to: string;
}

export const DEFAULT_TRANSACTION_FILTER: TransactionFilter = {
  search: '',
  status: 'all',
  method: 'all',
  courseId: 'all',
  from: '',
  to: '',
};

/** Pure filter, newest first, kept separate from the service so it is easy to test. */
export function filterTransactions(items: PaymentTransaction[], filter: TransactionFilter): PaymentTransaction[] {
  const term = filter.search.trim().toLowerCase();
  // Timestamps are stored in UTC, so compare against the local day shown to the admin.
  const from = filter.from ? new Date(`${filter.from}T00:00:00`).getTime() : -Infinity;
  const to = filter.to ? new Date(`${filter.to}T23:59:59.999`).getTime() : Infinity;

  return items
    .filter((t) => {
      const created = new Date(t.createdAt).getTime();
      return (
        (filter.status === 'all' || t.status === filter.status) &&
        (filter.method === 'all' || t.method === filter.method) &&
        (filter.courseId === 'all' || t.courseId === filter.courseId) &&
        created >= from &&
        created <= to &&
        (!term ||
          t.id.toLowerCase().includes(term) ||
          t.userName.toLowerCase().includes(term) ||
          t.userEmail.toLowerCase().includes(term) ||
          t.courseTitle.toLowerCase().includes(term))
      );
    })
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}
