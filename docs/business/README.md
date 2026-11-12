# Course Academy — Business Documentation

> **How to use these docs**: read this README first, then open only the relevant `modules/<module>.md`.
> `APPLICATION-OVERVIEW.md` = everything in one file (for sharing/reading end to end).
> `data-model.md` = database design, `api-contract.md` = backend endpoints, `CHANGELOG.md` = history.
> Module "Entities & Fields" tables describe the **frontend demo** fields; **`data-model.md` is authoritative for database types, keys and lengths**, and `api-contract.md` for request/response DTOs.
> These docs describe what the Angular demo **actually does**. Doubts are listed under each module's "Open Questions".

## App purpose
Online academy (Bangladesh) selling skills courses — Video Editing, Digital Marketing, Sports Journalism, Software Development — in Pre-recorded, Live and Hybrid formats. Students buy with bKash/Nagad/Rocket/card, learn, discuss in course communities and earn verifiable certificates. Staff manage courses, payments, access and certification.

## User roles
| Role (id) | Can do |
|---|---|
| Guest | Browse published courses, watch free previews, verify certificates, read site pages, send contact/career forms, register/login. |
| Student (`user`) | Guest + checkout/buy, dashboard, learning area for enrolled courses, mark progress, community post/comment, hand in exams, download certificates. |
| Instructor (`instructor`) | Same global rights as Student, **plus course grants** on the courses they teach: view/edit course & curriculum, moderate community, manage certification (rules, reviews, revoke). Opens admin panel for those courses. |
| Admin (`admin`) | Everything (all permissions, cannot be reduced). |
| Custom roles | Admin can create roles from the permission list; a user can also get a custom permission set overriding their role. |

## Glossary
| Term | Meaning |
|---|---|
| Course format | Pre-recorded (self-paced), Live (scheduled classes), Hybrid (both) |
| Module → Submodule → Section → Topic | Curriculum tree. Submodule has 2–3 sections; sections can nest topics ≤ 3 levels; each section/topic counts for progress |
| Preview video | Video free to watch without enrolling |
| Material | Course-level resource (PDF, Slides, Worksheet, Link) |
| Document | Section-level file (PDF/DOCX/PPTX), optionally attached to one video |
| Live session | Scheduled class with meeting link |
| Price / Original price | Sale price charged / regular price shown struck through |
| Enrollment | User's access to a course (`active` / `revoked`) |
| Course access | Active enrollment OR `courses.view` on the course |
| Transaction | One payment attempt (pending/success/failed/cancelled/refunded) |
| Fulfilment | Granting the enrollment once for a successful transaction |
| Course grant | Per-course permissions for one user |
| Screen | Admin/student page in the access matrix. Access is given per whole screen (one tick = all its actions) |
| Certification rule | Per-course: completion or central exam |
| Central exam | Project or presentation link handed in and scored by an instructor |
| Certificate code | Public id `CA-YYYY-XXXXXX` for verification |
| Moderator | User with `community.moderate` on a course |

## Module map
| Module | File | Routes | Status | Summary |
|---|---|---|---|---|
| Accounts & Auth | [accounts-auth.md](modules/accounts-auth.md) | `/login`, `/register` | Partial | Register/login/logout; no reset/profile |
| Access Control | [access-control.md](modules/access-control.md) | `/admin/security/*`, `/admin/users` | Done | Roles, permissions, user overrides, course grants |
| Course Catalog | [course-catalog.md](modules/course-catalog.md) | `/`, `/courses`, `/courses/:slug` | Done (reviews fake) | Browse, filter, detail, previews |
| Course Management | [course-management.md](modules/course-management.md) | `/admin/courses/**`, `/admin/instructors` | Done | Course CRUD, curriculum, sessions, materials, instructors |
| Checkout & Payments | [checkout-payments.md](modules/checkout-payments.md) | `/payment`, `/checkout/**`, `/admin/transactions` | Partial (demo gateway) | Purchase flow, transactions, refunds |
| Enrollments | [enrollments.md](modules/enrollments.md) | `/admin/enrollments` | Done | Access records, revoke/restore |
| Learning & Progress | [learning-progress.md](modules/learning-progress.md) | `/dashboard`, `/learn/:slug` | Partial (Live progress missing) | Learning area, completion, dashboard |
| Certification | [certification.md](modules/certification.md) | `/admin/courses/:id/certification`, `/admin/certificates`, `/verify[/:code]` | Done | Rules, exams, certificates, verify |
| Community | [community.md](modules/community.md) | `/courses/:slug#community`, `/admin/community` | Done | Course discussions + moderation |
| Admin Overview | [admin-overview.md](modules/admin-overview.md) | `/admin` | Done | Stats, recent payments, demo reset |
| Site Content | [site-content.md](modules/site-content.md) | `/about`, `/sports`, `/career`, `/contact`, `/testimonials`, `/faq`, `/privacy`, `/terms`, `**` | Partial (forms not stored) | Static pages, contact & career forms, language |

## Global rules
- **Auth**: Route guards only control navigation; every service write re-checks permissions. **Backend must enforce all permission checks server-side.** Session: "remember me" = persistent, else browser-session only. Signed-out users hitting protected routes go to `/login?returnUrl=`.
- **Permissions**: names `area.action` (28 values, see data-model). Effective = Admin→ALL, else user override ?? role permissions, plus course grants. `admin.access` is derived, never stored.
- **Currency**: BDT only (frontend demo uses whole Taka; database uses `decimal(18,2)`), shown as `৳1,234` (Bangla digits in bn). No tax, no coupons.
- **Language**: Bangla default, English switch. UI strings translated by English key; admin-entered content not translated.
- **Dates**: stored as ISO 8601 UTC strings; displayed in viewer's locale/time zone. `LiveSession.startsAt` is local `YYYY-MM-DDTHH:mm` (no zone). `Course.updated` is `YYYY-MM`.
- **IDs / codes (frontend demo)**: browser-created ids `<prefix>-<base36 timestamp>-<6 random>`; Course id = numeric timestamp (never reused); Transaction id `DEMO-YYYYMMDD-XXXXXX`; Certificate code `CA-YYYY-XXXXXX` (alphabet without I, O, 0, 1); Progress id `userId:courseId`; CertificationRule id = courseId. Backend: `int IDENTITY` PKs; slug, `TransactionNo` and certificate `Code` stay unique public identifiers.
- **Snapshots**: Transactions copy user + course data; Certificates copy user, course and instructor names — later edits don't change them.
- **Deletion**: frontend demo hard-deletes; **backend soft-deletes every table** (`IsDeleted`). Business states also exist: "Soft" states instead: course `published=false`, enrollment `revoked`, certificate `revoked`, post/comment `hidden`. Course delete cascades enrollments, community, grants (not payments/progress/certificates).
- **Pagination**: API lists return `{items,total,page,pageSize}` (see api-contract). Frontend today: Client: catalogue "load more" 6 per page; admin community latest 20 comments; overview 5 recent payments; dashboard 5 upcoming classes.
- **Sorting defaults**: newest first for admin lists/transactions/enrollments/certificates; catalogue by popularity.
- **Text**: inputs trimmed before save; validation messages are English keys (translated in UI).
- **Phone**: Bangladeshi mobile `^(?:\+?88)?01[3-9]\d{8}$`. **URL**: `^https?://[^\s.]+\.[^\s]{2,}$` (exam links require https).
- **Demo-only (do not port)**: localStorage persistence, plain-text passwords, simulated gateway page, reset demo data, fake latency.

## Technical decisions (backend)
| Topic | Decision |
|---|---|
| Database | SQL Server + EF Core; PascalCase singular table names |
| Primary keys | `int IDENTITY`; public unique fields: `Course.Slug`, `PaymentTransaction.TransactionNo`, `Certificate.Code` |
| Delete | Soft delete on all tables (`IsDeleted`, `DeletedAt`, `DeletedBy`); FKs `NO ACTION`; filtered unique indexes |
| Audit | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` on all tables |
| Time | `datetime2` UTC; live session times entered in Asia/Dhaka |
| Money | `decimal(18,2)` BDT |
| Enums / permissions | Lookup tables (incl. `Screen` and `Permission`) |
| Text lengths | Standard defaults, see data-model §3 |
| Auth | JWT access token + refresh token (`RefreshToken` table) |
| API | `/api/v1`, wrapper `{success, data, message, errors}`, lists `data = {items,total,page,pageSize}` |
