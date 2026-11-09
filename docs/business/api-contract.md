# API Contract (draft for ASP.NET Core Web API)

Conventions:
- Base path `/api`. JSON. Auth via bearer token or cookie (TBD). `🔒 perm` = required permission; `(course)` = global or course grant.
- `/api/me/*` = signed-in user's own data. `/api/admin/*` = staff screens.
- List endpoints accept `page` (1-based), `pageSize` (default 20), `sort`; response `{ items, total, page, pageSize }`. (Frontend has no server pagination yet — inferred.)
- Errors: `400` validation `{ errors: { field: [message] } }`, `401` not signed in, `403` permission denied ("You do not have permission to do this."), `404`, `409` conflict (duplicate slug/email/role name, invalid transition).
- Every write re-checks permissions server-side (BR-ACL-16).

## Accounts & Auth
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | `{name, email, password, phone?}` | `{user, token}` | `Register` | BR-AUTH-02,03,04,08 |
| POST | `/api/auth/login` | `{email, password, remember}` | `{user, token}` | `Login` | BR-AUTH-01,05 |
| POST | `/api/auth/logout` | — | 204 | `Navbar` | BR-AUTH-10 |
| GET | `/api/auth/me` | — | `{user, role, permissions[], courseGrants[], homeUrl}` | guards, `PermissionService` | BR-ACL-01,02,09 |
| POST | `/api/auth/forgot-password` | `{email}` | 204 | `Login` (planned) | BR-AUTH-12 |

## Access Control
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/users?search=` 🔒 users.view / access.view | — | `[{id,name,email,roleId,roleName,hasCustomAccess,activeCourses}]` | `AdminUsers`, `AdminAccess` | — |
| GET | `/api/users/{id}/access` 🔒 access.view | — | `{user, permissions[], isCustom, courseGrants[]}` | `UserAccess` | BR-ACL-01 |
| PUT | `/api/users/{id}/role` 🔒 roles.edit | `{roleId}` | user | `AdminAccess` | BR-ACL-08,17 |
| PUT | `/api/users/{id}/access` 🔒 access.edit | `{permissions[]}` | user | `UserAccess` | BR-ACL-08,10 |
| DELETE | `/api/users/{id}/access` 🔒 access.edit | — | user | `UserAccess` | BR-ACL-11 |
| PUT | `/api/users/{id}/course-grants/{courseId}` 🔒 access.edit | `{permissions[]}` (empty = delete) | grant \| 204 | `UserAccess` | BR-ACL-13 |
| GET | `/api/roles` 🔒 roles.view | — | `[{id,name,description,permissions[],system,memberCount}]` | `AdminRoles`, `AdminAccess` | — |
| POST | `/api/roles` 🔒 roles.edit | `{name, description, permissions[]}` | role | `AdminRoles` | BR-ACL-05 |
| PUT | `/api/roles/{id}` 🔒 roles.edit | same | role | `AdminRoles` | BR-ACL-05 |
| DELETE | `/api/roles/{id}` 🔒 roles.edit | — | 204 | `AdminRoles` | BR-ACL-06,07 |
| GET | `/api/permissions` | — | permission groups + screens | `AccessMatrix` (or keep static) | — |

## Course Catalog (public)
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/courses` | `?search=&category=&price=all\|under-2500\|2500-4500\|over-4500&levels=&formats=&sort=popular\|rating\|newest\|price-asc\|price-desc&featured=&page=&pageSize=6` | course cards + `total` + facet counts (category, level, format) | `CourseList`, `Courses` (home), `Sports` | BR-CAT-01,03–10 |
| GET | `/api/courses/{slug}` | — | course detail (curriculum with preview URLs only, sessions w/o meeting links unless access, materials w/o URL unless access, instructor, `hasAccess`, `isEnrolled`) | `CourseDetail` | BR-CAT-02,13–16,18 |
| GET | `/api/courses/{id}/related?limit=4` | — | course cards | `CourseDetail` | BR-CAT-11 |
| GET | `/api/courses/{id}/reviews` | — | `{reviews[], breakdown[5]}` | `CourseDetail` | BR-CAT-12 |
| GET | `/api/categories` | — | categories + published counts | `Categories`, `CourseList`, `Navbar` | — |

## Course Management
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/admin/courses?search=&format=` 🔒 courses.view (course) | — | courses user can view + counts (sections, sessions, materials, activeEnrollments, allEnrollments) | `AdminCourses` | BR-CMS-16 |
| GET | `/api/admin/courses/{id}` 🔒 courses.edit (course) | — | full course | `CourseForm`, `CurriculumEditor` | BR-CMS-01 |
| GET | `/api/admin/courses/slug-available?slug=&exceptId=` | — | `{available}` | `CourseForm` | BR-CMS-02 |
| POST | `/api/admin/courses` 🔒 courses.create | course details + schedule[] + materials[] | course | `CourseForm` | BR-CMS-01–05,11,17 |
| PUT | `/api/admin/courses/{id}` 🔒 courses.edit (course) | same | course | `CourseForm` | BR-CMS-03–05,11 |
| PATCH | `/api/admin/courses/{id}/publish` 🔒 courses.edit (course) | `{published}` | course | `AdminCourses` | BR-CMS-04,05,12 |
| PUT | `/api/admin/courses/{id}/curriculum` 🔒 courses.edit (course) | `{modules[]}` (nested tree) | course | `CurriculumEditor` | BR-CMS-06–10,18 |
| DELETE | `/api/admin/courses/{id}` 🔒 courses.delete (course) | — | 204 (cascades enrollments, community, grants) | `AdminCourses` | BR-CMS-13 |
| GET | `/api/instructors` 🔒 instructors.view (list also used by course form) | — | instructors + assignedCourseCount | `AdminInstructors`, `CourseForm` | — |
| POST | `/api/instructors` 🔒 instructors.create | `{name,title,bio,expertise[],experienceYears,students}` | instructor | `AdminInstructors` | BR-CMS-15 |
| PUT | `/api/instructors/{id}` 🔒 instructors.edit | same | instructor | `AdminInstructors` | BR-CMS-15 |
| DELETE | `/api/instructors/{id}` 🔒 instructors.delete | — | 204 / 409 if assigned | `AdminInstructors` | BR-CMS-14 |

## Checkout & Payments
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/checkout/{slug}` 🔒 checkout.view | — | `{course summary, price, originalPrice, discountPercent, alreadyEnrolled, methods[]}` | `Checkout` | BR-PAY-01,02 |
| POST | `/api/courses/{id}/enroll-free` 🔒 checkout.view | — | enrollment | `Checkout` | BR-PAY-03 |
| POST | `/api/payments` 🔒 checkout.view | `{courseId, method}` (server computes amount) | `{transactionId, redirectUrl}` | `Checkout` | BR-PAY-04,05 |
| POST | `/api/payments/callback/{gateway}` (gateway IPN, signed) | gateway payload | 200 | gateway | BR-PAY-06–08 |
| GET | `/api/payments/{id}` | — | transaction (buyer or transactions.view) | `PaymentResult`, `DemoGateway` | BR-PAY-08,09,18 |
| GET | `/api/me/payments` | — | own transactions newest first | `Dashboard` | BR-LRN-14 |
| GET | `/api/admin/transactions` 🔒 transactions.view | `?search=&status=&method=&courseId=&from=YYYY-MM-DD&to=YYYY-MM-DD&page=&pageSize=` | `{items, total, totals:{collected, refunded, successful, unsuccessful}, courseOptions[]}` | `AdminTransactions` | BR-PAY-13,14 |
| PATCH | `/api/admin/transactions/{id}/status` 🔒 transactions.edit | `{status}` | transaction (+ side effects) | `AdminTransactions` | BR-PAY-10,11 |
| DELETE | `/api/admin/transactions/{id}` 🔒 transactions.delete | — | 204 / 409 if success | `AdminTransactions` | BR-PAY-12 |
| POST | `/api/demo/payments/{id}/simulate` (dev only) | `{outcome: success\|failed\|cancelled}` | transaction | `DemoGateway` | BR-PAY-06,07 |

## Enrollments
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/me/enrollments` | — | `[{enrollment, course card, progressPercent}]` | `Dashboard` | BR-ENR-10, BR-LRN-12 |
| GET | `/api/admin/enrollments` 🔒 enrollments.view | `?search=&status=all\|active\|revoked&page=&pageSize=` | rows `{enrollment, studentName, email, courseTitle}` newest first | `AdminEnrollments` | BR-ENR-09 |
| PATCH | `/api/admin/enrollments/{id}/status` 🔒 enrollments.edit | `{status}` | enrollment | `AdminEnrollments` | BR-ENR-05 |

## Learning & Progress
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/learn/{slug}` 🔒 learn.view + access | — | full curriculum (all URLs), sessions with links, materials with URLs | `Learn` | BR-LRN-01, BR-ENR-03 |
| GET | `/api/me/courses/{courseId}/progress` | — | `{completedSectionIds[], completedCount, total, percent}` | `Learn`, `Dashboard` | BR-LRN-04,05 |
| PUT | `/api/me/courses/{courseId}/progress/sections/{sectionId}` 🔒 learn.view + access | `{completed: bool}` | progress + `certificateIssued?` | `Learn` | BR-LRN-02,03,07 |
| GET | `/api/me/upcoming-sessions?limit=5` | — | `[{session, course}]` | `Dashboard` | BR-LRN-11 |

## Certification
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/admin/courses/{id}/certification-rule` 🔒 certificates.manage (course) | — | rule (default if none) | `CertificationSettings` | BR-CERT-01 |
| PUT | `/api/admin/courses/{id}/certification-rule` 🔒 certificates.manage (course) | rule fields | rule | `CertificationSettings` | BR-CERT-02 |
| GET | `/api/me/courses/{id}/certification` | — | `{rule, progress, progressMet, submissions[], attemptsLeft, canSubmit, certificate?}` | `CourseCertificate` | BR-CERT-06,07,16 |
| POST | `/api/me/courses/{id}/certificate` | — | certificate \| 204 | `CourseCertificate` (claim) | BR-CERT-03,04 |
| POST | `/api/me/courses/{id}/exam-submissions` 🔒 learn.view + enrolled | `{url, notes}` | submission | `CourseCertificate` | BR-CERT-06,08 |
| GET | `/api/me/certificates` | — | own certificates newest first | `Dashboard` | — |
| GET | `/api/admin/exam-submissions` 🔒 certificates.manage (course) | `?status=submitted\|passed\|failed\|all&courseId=&page=` | submissions + student name + course title + rule exam title | `AdminCertificates` | — |
| POST | `/api/admin/exam-submissions/{id}/review` 🔒 certificates.manage (course) | `{score, feedback}` | submission + certificate? | `AdminCertificates` | BR-CERT-09,10 |
| GET | `/api/admin/certificates` 🔒 certificates.manage (course) | `?search=&page=` | certificates newest first | `AdminCertificates` | — |
| POST | `/api/admin/certificates/{id}/revoke` 🔒 certificates.manage (course) | `{reason}` | certificate | `AdminCertificates` | BR-CERT-11 |
| POST | `/api/admin/certificates/{id}/restore` 🔒 certificates.manage (course) | — | certificate / 409 | `AdminCertificates` | BR-CERT-11 |
| GET | `/api/certificates/verify/{code}` (public) | — | `{valid, revoked, revokedReason, userName, courseTitle, mode, examType, scorePercent, instructorName, issuedAt, code}` / 404 | `Verify` | BR-CERT-13 |
| GET | `/api/certificates/{code}/pdf` (optional) | — | PDF | `Dashboard`, `CourseCertificate`, `AdminCertificates` | BR-CERT-15 |

## Community
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/courses/{id}/community` | — | `{canRead, canPost, canModerate, publicCount}` | `CourseCommunity`, `Learn` | BR-COM-01–03,14 |
| GET | `/api/courses/{id}/posts?page=` | — | posts (pinned first, newest) with author name, role badge, commentCount | `CourseCommunity` | BR-COM-05,12,13 |
| POST | `/api/courses/{id}/posts` | `{title, body}` | post | `CourseCommunity` | BR-COM-03,11 |
| PUT | `/api/posts/{id}` | `{title, body}` | post | `CourseCommunity` | BR-COM-08 |
| DELETE | `/api/posts/{id}` | — | 204 | `CourseCommunity`, `AdminCommunity` | BR-COM-09 |
| PATCH | `/api/posts/{id}/flags` (moderator) | `{pinned?, locked?, hidden?}` | post | `CourseCommunity`, `AdminCommunity` | BR-COM-10 |
| GET | `/api/posts/{id}/comments` | — | top-level comments oldest first, each with replies | `CourseCommunity` | BR-COM-06,07 |
| POST | `/api/posts/{id}/comments` | `{body, parentId?}` | comment | `CourseCommunity` | BR-COM-04,07,11 |
| PUT | `/api/comments/{id}` | `{body}` | comment | `CourseCommunity` | BR-COM-08 |
| DELETE | `/api/comments/{id}` | — | 204 | `CourseCommunity`, `AdminCommunity` | BR-COM-09 |
| PATCH | `/api/comments/{id}/hidden` (moderator) | `{hidden}` | comment | `CourseCommunity`, `AdminCommunity` | BR-COM-10 |
| GET | `/api/admin/community/posts` 🔒 community.moderate (course) | `?courseId=&status=all\|visible\|hidden\|pinned\|locked&search=` | posts | `AdminCommunity` | BR-COM-16 |
| GET | `/api/admin/community/comments` 🔒 community.moderate (course) | `?courseId=&status=&search=&limit=20` | comments + post title + course | `AdminCommunity` | BR-COM-16 |

## Admin Overview
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| GET | `/api/admin/overview` 🔒 reports.view | — | `{courseCount, publishedCount, studentCount, activeEnrollments, collected, recentTransactions[5]}` | `AdminOverview` | BR-ADM-01–05 |
| POST | `/api/demo/reset` 🔒 system.reset (dev only) | — | 204 | `AdminOverview` | BR-ADM-06 |

## Site Content
| Method | Endpoint | Request | Response | Used by (component) | Rule refs |
|---|---|---|---|---|---|
| POST | `/api/contact-messages` | `{name, email, phone?, topic, message}` | 201 | `Contact` | BR-SITE-01,03 |
| POST | `/api/career-applications` | `{name, email, phone, role, portfolio?, message?}` | 201 | `Career` | BR-SITE-02,04 |
| GET | `/api/career/opportunities?team=` | — | opportunities | `Career`, `CareerTeaser` | — |
| GET | `/api/sports/stories?topic=` | — | stories | `Sports`, `SportsTeaser` | BR-SITE-09 |
| GET | `/api/testimonials`, `/api/faqs` | — | lists | `Testimonials`, `Faq` | — |
