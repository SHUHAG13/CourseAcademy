# Checkout & Payments

## 1. Purpose
Lets a signed-in user buy a course with bKash, Nagad, Rocket or card, records each payment attempt as a transaction, grants course access on success, and lets admins manage transactions (mark paid, cancel, refund, delete). Currently uses a simulated gateway.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/payment` | `PaymentMethods` | Public "How to enroll & pay" page: 4 steps, method list, WhatsApp link (pre-filled with course title + price if `?course=<slug>`). |
| `/checkout/:slug` | `Checkout` | Order summary (course, instructor, original price, discount %, total), method choice (default bKash), Pay button. "Already enrolled" state. Free courses enroll directly. Guards: auth + `checkout.view`. |
| `/checkout/pay/:id` | `DemoGateway` | Simulated gateway page: shows method test account + amount, buttons Success / Fail / Cancel. Only the buyer can see it. Revisiting a finished payment redirects to the result. |
| `/checkout/result/:id` | `PaymentResult` | Receipt: status (success/failed/cancelled/pending/refunded) message, transaction details, "Go to course" if access exists. Visible to buyer or `transactions.view`. Also opened from dashboard. |
| `/admin/transactions` | `AdminTransactions` | Filters (search, status, method, course, from/to date), totals, table, detail row, actions per allowed transition, delete. |

## 3. Entities & Fields
**PaymentTransaction**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `DEMO-YYYYMMDD-XXXXXX` (6 upper-case base36) | PK |
| gateway | string | yes | — | `demo` | Which processor |
| userId | string | yes | FK User | — | |
| userName | string | yes | — | snapshot | Copied at purchase |
| userEmail | string | yes | — | snapshot | |
| courseId | number | yes | FK Course (may be deleted later) | — | |
| courseSlug | string | yes | — | snapshot | |
| courseTitle | string | yes | — | snapshot | |
| method | PaymentMethodId | yes | bkash / nagad / rocket / card | bkash | |
| maskedAccount | string | yes | — | from method | Never full card/wallet |
| originalAmount | number (BDT) | yes | — | course.originalPrice | |
| discountAmount | number (BDT) | yes | — | originalPrice − price | |
| amount | number (BDT) | yes | — | course.price | Charged amount |
| currency | 'BDT' | yes | — | BDT | |
| status | PaymentStatus | yes | pending/success/failed/cancelled/refunded | pending | |
| failureReason | string | no | — | method's simulated reason on fail | |
| fulfilledAt | ISO datetime | no | — | set when access granted | Grants access only once |
| createdAt | ISO datetime | yes | — | now | |
| updatedAt | ISO datetime | yes | — | now | |

**PaymentMethod** (static config)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | PaymentMethodId | yes | — | — | bkash, nagad, rocket, card |
| name | string | yes | — | — | bKash, Nagad, Rocket, Visa / Mastercard |
| description | string | yes | — | — | |
| logo / icon | string | no / yes | — | — | |
| testAccountLabel, testAccount, maskedAccount | string | yes | — | — | Demo only |
| failureReason | string | yes | — | — | Simulated decline text |

**TransactionFilter** (UI): search, status|all, method|all, courseId|all, from (YYYY-MM-DD), to (YYYY-MM-DD).

## 4. Business Rules
- **BR-PAY-01** Checkout is only possible for a **published** course; otherwise "not found".
- **BR-PAY-02** If the user already has an active enrollment, checkout shows "You're already enrolled" and no payment is possible.
- **BR-PAY-03** Free course (`price = 0`): no transaction; enrollment granted immediately; redirect to `/learn/:slug`.
- **BR-PAY-04** Paid course: a `pending` transaction is created with amounts copied from the course; user is redirected to the gateway page.
- **BR-PAY-05** The server must recompute the amount from its own course price; amounts from the browser are display-only.
- **BR-PAY-06** Gateway outcome can only change a `pending` transaction (success / failed / cancelled). A non-pending transaction is returned unchanged.
- **BR-PAY-07** On `failed`, `failureReason` = method's reason (e.g. "Insufficient wallet balance (simulated)").
- **BR-PAY-08** Confirm: after gateway result, if status = success and not yet `fulfilledAt`, grant enrollment (with transactionId) and set `fulfilledAt`. Repeatable/idempotent (reopening the receipt never re-grants).
- **BR-PAY-09** Only the buyer can see the gateway page; buyer or `transactions.view` can see the receipt.
- **BR-PAY-10** Admin status transitions allowed only: `pending → success`, `pending → cancelled`, `success → refunded`. Others rejected: "This change is not allowed for this transaction."
- **BR-PAY-11** Admin marks `success` → fulfil (grant access once). Admin `refunded` → enrollment linked to this transaction set to `revoked`.
- **BR-PAY-12** A `success` transaction cannot be deleted (refund first). Delete requires `transactions.delete`. *(Enforced in UI only — see Open Questions.)*
- **BR-PAY-13** Transaction filter: date bounds inclusive, using the admin's local day (from 00:00:00 to 23:59:59.999); search matches id, userName, userEmail, courseTitle (contains, case-insensitive); result sorted newest first.
- **BR-PAY-14** Admin course filter options = distinct courses appearing in transactions (includes deleted courses, by snapshot title).
- **BR-PAY-15** Transactions keep a snapshot of user and course data so they survive course edits/deletion.
- **BR-PAY-16** Card/wallet credentials are never collected or stored; only masked test account.
- **BR-PAY-17** Admins buying a course see a note that the test payment is recorded under their account.
- **BR-PAY-18** Receipt "Go to course" shows only if status = success and user still has course access.

## 5. Workflows & Status Transitions
**Purchase (Student)**: Course detail → Enroll Now → (login) → Checkout → choose method → Pay → `pending` created → Gateway page → choose outcome → status set → confirm → Result page.

Transitions:
| From → To | Triggered by |
|---|---|
| (new) → `pending` | Buyer (Pay) |
| `pending` → `success` | Gateway (buyer simulates) / Admin "Mark as paid" |
| `pending` → `failed` | Gateway |
| `pending` → `cancelled` | Gateway (buyer cancels) / Admin "Cancel payment" |
| `success` → `refunded` | Admin "Refund" (revokes access) |
`failed`, `cancelled`, `refunded` are final.

## 6. Permissions
| Action | Guest | Student | Instructor | Admin |
|---|---|---|---|---|
| View `/payment` info | ✓ | ✓ | ✓ | ✓ |
| Checkout / pay (`checkout.view`) | — (login) | ✓ | ✓ | ✓ |
| View own receipt | — | ✓ | ✓ | ✓ |
| View all transactions (`transactions.view`) | — | — | — | ✓ |
| Change status (`transactions.edit`) | — | — | — | ✓ |
| Delete non-success (`transactions.delete`) | — | — | — | ✓ |

## 7. Calculations
- `discountAmount = originalPrice − price`; `amount = price`.
- Checkout discount % = `round((1 − price/originalPrice) × 100)` if originalPrice > 0.
- Admin totals (over the **filtered** list): collected = Σ amount where success; refunded = Σ amount where refunded; successful = count success; unsuccessful = count failed + cancelled.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| `PaymentService.checkout` → `DemoPaymentGateway.initiate` | `POST /api/payments` `{courseId, method}` → `{transactionId, redirectUrl}` |
| `DemoPaymentGateway.simulate` | Replaced by real gateway hosted page + `POST /api/payments/callback/{gateway}` (IPN) |
| `PaymentService.confirm` / gateway `transaction()` | `GET /api/payments/{id}` (verified status) |
| `PaymentService.mine` | `GET /api/me/payments` |
| `PaymentService.transactions` + `filterTransactions` | `GET /api/admin/transactions?search=&status=&method=&courseId=&from=&to=&page=&pageSize=` (+ totals) |
| `PaymentService.setStatus` | `PATCH /api/admin/transactions/{id}/status` |
| `PaymentService.remove` | `DELETE /api/admin/transactions/{id}` |
| Free enroll `EnrollmentService.grant` | `POST /api/courses/{id}/enroll-free` |

## 9. Open Questions / Assumptions
- `PaymentService.remove()` does not itself block deleting a `success` transaction (only the button is disabled). Backend must enforce BR-PAY-12.
- Should transactions ever be hard-deleted? Consider soft delete for audit.
- Pending transactions never expire. Should they time out (→ cancelled/expired)?
- A user can create many pending transactions for the same course. Limit to one?
- Refund is full only; partial refunds not supported.
- Which real gateway (SSLCOMMERZ, aamarPay, bKash direct)? Gateway reference/transaction id field needed (inferred: `GatewayReference`).
- No coupon/promo code; discount is only `originalPrice − price`. Coupons needed?
- No VAT/tax. Required for invoices?
- Free-course enroll requires `checkout.view` route permission but `grant()` itself doesn't check it.
- Receipt/invoice PDF or email not implemented.

## 10. Source Files
- `src/app/core/payments/payment.service.ts`, `payment.model.ts`, `payment-gateway.ts`, `demo-payment-gateway.ts`
- `src/app/pages/checkout/checkout.ts`, `demo-gateway.ts`, `payment-result.ts`
- `src/app/pages/admin/admin-transactions.ts` / `.html`
- `src/app/components/payment-methods/payment-methods.ts`
