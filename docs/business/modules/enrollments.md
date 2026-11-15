# Enrollments

## 1. Purpose
Records which user has access to which course, links access to the payment that granted it, and lets admins revoke or restore access.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/admin/enrollments` | `AdminEnrollments` | All enrollments (newest first): student name, email, course title, status, date. Search (student/email/course), status filter (all/active/revoked). Revoke (with confirm) / Restore. |
| `/dashboard` | `Dashboard` | "My courses" list incl. revoked ones (tagged). See learning-progress. |

## 3. Entities & Fields
**Enrollment**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `enrollment-…` | PK |
| userId | string | yes | FK User | — | unique with courseId (one record per user+course) |
| courseId | number | yes | FK Course | — | |
| status | EnrollmentStatus | yes | `active` / `revoked` | active | |
| enrolledAt | ISO datetime | yes | — | now | Overwritten on re-grant |
| transactionId | string | no | FK PaymentTransaction | — | Absent for free courses |

## 4. Business Rules
- **BR-ENR-01** One enrollment record per (user, course). Granting again reuses the existing record id, sets status `active`, resets `enrolledAt` to now and replaces `transactionId`.
- **BR-ENR-02** "Enrolled" = enrollment exists with status `active`.
- **BR-ENR-03** Course access (learning area, locked docs, meeting links, community read) = enrolled OR has `courses.view` on the course (global or grant).
- **BR-ENR-04** A user may grant an enrollment to themselves (via checkout). Granting for someone else requires `enrollments.edit` or `transactions.edit`.
- **BR-ENR-05** Admin revoke/restore requires `enrollments.edit`. Revoking does not change the payment record.
- **BR-ENR-06** Refunding a transaction revokes the enrollment whose `transactionId` matches (requires `transactions.edit`).
- **BR-ENR-07** Deleting a course hard-deletes all its enrollments.
- **BR-ENR-08** Enrollment counts: admin courses list and overview count only `active`; delete-course confirmation counts all.
- **BR-ENR-09** Rows whose user or course was deleted show "Deleted user" / "Deleted course".
- **BR-ENR-10** A revoked enrollment still appears on the student dashboard (tagged revoked), without "Continue learning".
- **BR-ENR-11** (API) Enrollments are created only by the server: free enrollment, a verified payment, or an admin marking a payment as paid. The browser never grants access itself.

## 5. Workflows & Status Transitions
| Transition | Triggered by |
|---|---|
| (none) → `active` | Student (free checkout / successful payment fulfilment), Admin (mark payment as paid) |
| `active` → `revoked` | Admin (Revoke, `enrollments.edit`), Admin refund of the linked transaction |
| `revoked` → `active` | Admin (Restore, `enrollments.edit`); or a new successful purchase |

## 6. Permissions
| Action | Student | Instructor | Admin |
|---|---|---|---|
| See own enrollments | ✓ | ✓ | ✓ |
| View all enrollments (`enrollments.view`) | — | — | ✓ |
| Revoke / restore (`enrollments.edit`) | — | — | ✓ |

## 7. Calculations
- Active count per course = count(status = active).
- Dashboard active count = own enrollments (for existing courses) with status active.

## 8. Data Source
**Done in API mode (backend Step 4).** Demo mode keeps browser storage (`ca-enrollments`).
| Current | Endpoint |
|---|---|
| `EnrollmentService.mine` | `GET /api/v1/enrollments/mine` |
| `EnrollmentService.enrollments` (admin) | `GET /api/v1/admin/enrollments` |
| `EnrollmentService.setStatus` | `PATCH /api/v1/admin/enrollments/{id}/status` |
| `EnrollmentService.enrollFree` | `POST /api/v1/enrollments/free/{slug}` |
| `grant` / `revokeForTransaction` | server only (payment fulfilment, refund) |
| enrollment counts on admin course list | `activeEnrollmentCount`, `enrollmentCount` in `GET /api/v1/courses/all` (managers only) |

## 9. Open Questions / Assumptions
- Admin cannot manually create an enrollment (e.g. cash/offline payment via WhatsApp). Needed? The WhatsApp flow suggests offline payments exist.
- Should enrollments expire (time-limited access)?
- Re-granting overwrites `enrolledAt` and `transactionId`, losing history of the previous purchase. Keep history instead?
- Re-buying after admin revoke is possible (checkout shows only for non-active). Intended?
- When an enrollment is revoked, progress and certificates are kept. Correct?

## 10. Source Files
- `src/app/core/services/enrollment.service.ts`
- `src/app/core/models/enrollment.model.ts`
- `src/app/pages/admin/admin-enrollments.ts`
