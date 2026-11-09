# Admin Overview & Reports

## 1. Purpose
Gives admins a summary of the academy (courses, students, enrollments, revenue, recent payments) and a demo-only "Reset demo data" action.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/admin` | `AdminOverview` | 4 stat cards (link to their screens), 5 most recent transactions, "Reset demo data" button (if `system.reset`). Guard `reports.view`. |

## 3. Entities & Fields
No own entity. Reads Course, User, Enrollment, PaymentTransaction.
**Stat card** (computed)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| label | string | yes | — | — | |
| value | string | yes | — | — | formatted number / BDT |
| link | string | yes | — | — | admin route |

## 4. Business Rules
- **BR-ADM-01** "Courses" = all courses (incl. drafts); label shows published count.
- **BR-ADM-02** "Student accounts" = users with role `user` (Student) only.
- **BR-ADM-03** "Active enrollments" = enrollments with status `active`.
- **BR-ADM-04** "Demo payments collected" = Σ amount of transactions with status `success` (refunded excluded).
- **BR-ADM-05** Recent payments = 5 newest transactions by createdAt (any status).
- **BR-ADM-06** Reset demo data (confirm) restores all collections to seed data; requires `system.reset`. Demo-only, must not exist in production.
- **BR-ADM-07** If stored data can't be read on start-up, defaults are restored and a toast says so (demo-only).

## 5. Workflows & Status Transitions
None.

## 6. Permissions
| Action | Permission | Admin | Others |
|---|---|---|---|
| View overview | `reports.view` | ✓ | if granted |
| Reset demo data | `system.reset` | ✓ | if granted |

## 7. Calculations
- Collected = Σ `amount` where status = success.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| Client aggregation over all stores | `GET /api/admin/overview` → `{courseCount, publishedCount, studentCount, activeEnrollments, collectedBdt, recentTransactions[5]}` |
| `DemoDataService.reset()` | none (dev-only seed script) |

## 9. Open Questions / Assumptions
- Which reports are needed in production (revenue by period/course/method, enrollments over time, completion rate)?
- Should "Student accounts" include instructors who are also learners?

## 10. Source Files
- `src/app/pages/admin/admin-overview.ts`
- `src/app/core/services/demo-data.service.ts`
