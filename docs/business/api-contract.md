# API Contract — ASP.NET Core Web API

Exact contract for the backend. Business rules (BR-IDs) are in `modules/*.md`; tables in `data-model.md`.

## 1. Conventions
| Topic | Rule |
|---|---|
| Base URL | `/api/v1` |
| Format | JSON, camelCase property names, UTF-8 |
| Auth | JWT access token in `Authorization: Bearer <token>`; refresh token to renew (section 3) |
| Public ids in URLs | course `slug`, payment `transactionNo`, certificate `code`; admin/internal endpoints use numeric `id` |
| Dates | ISO 8601 UTC strings, e.g. `2026-11-12T08:30:00.000Z` |
| Money | number with 2 decimals (decimal(18,2)), currency `BDT` |
| Enums | sent as lookup `code` strings (e.g. `"pending"`, `"Pre-recorded"`) |
| Soft delete | `DELETE` endpoints soft-delete; deleted rows never appear in responses |
| Permissions | 🔒 `perm` = required permission; `(course)` = global OR course grant on that course. Every write re-checks on the server (BR-ACL-16) |
| Text | inputs trimmed by the server before validation and save |

## 2. Response wrapper
Every response (success and error) uses:
```json
{ "success": true, "data": { }, "message": null, "errors": [] }
```
| Field | Type | Notes |
|---|---|---|
| success | bool | false on any error |
| data | object \| array \| null | payload; null on error |
| message | string \| null | human-readable summary (English key; UI translates) |
| errors | `ErrorItem[]` | validation / business errors; empty on success |

`ErrorItem` = `{ field: string | null, message: string }` (field = camelCase property name, null for non-field errors).

**List responses** — `data` is a page:
```json
{ "items": [ ], "total": 42, "page": 1, "pageSize": 20 }
```
List query params (all list endpoints): `page` (int ≥ 1, default 1), `pageSize` (int 1–100, default 20), plus the filters listed per endpoint.

**HTTP status codes**
| Code | When | message example |
|---|---|---|
| 200 | OK | |
| 201 | Created | |
| 400 | Validation failed (errors filled) | "Please fix the highlighted fields." |
| 401 | Not signed in / token expired | "Please log in to continue." |
| 403 | Permission denied | "You do not have permission to do this." |
| 404 | Not found (or not visible to caller) | |
| 409 | Business conflict (duplicate email/slug/role name, invalid status change, already reviewed, etc.) | rule's message |

## 3. Shared DTOs
```text
UserSummaryDto      { id:int, name:string, email:string }
PermissionCode      string  (e.g. "courses.edit")
CourseGrantDto      { courseId:int, courseTitle:string, permissions:PermissionCode[] }
MeDto               { id, name, email, phone:string|null, roleCode:string, roleName:string, instructorId:int|null,
                      permissions:PermissionCode[], courseGrants:CourseGrantDto[], hasAdminPanel:bool, homeUrl:string }
CategoryDto         { code, name, shortName, description, icon, imageUrl, skills:string[], publishedCourseCount:int }
InstructorDto       { id, name, title, bio, expertise:string[], experienceYears:int, students:int, rating:number,
                      publishedCourseCount:int }
CourseCardDto       { id, slug, title, subtitle, categoryCode, formatCode, levelCode, badgeCode:string|null,
                      price:number, originalPrice:number, discountPercent:int, rating:number, ratingCount:int,
                      students:int, durationHours:int, coverLabel, coverIcon, imageUrl:string|null,
                      instructorName:string, sectionCount:int, contentUpdatedMonth:"YYYY-MM" }
VideoDto            { id, title, minutes:int, isPreview:bool, url:string|null }   // url null when caller has no access and !isPreview
DocumentDto         { id, title, typeCode, videoId:int|null, url:string|null, isLocked:bool }
SectionDto          { id, title, depth:int, minutes:int, videos:VideoDto[], documents:DocumentDto[], topics:SectionDto[] }
SubmoduleDto        { id, title, sections:SectionDto[] }
ModuleDto           { id, title, minutes:int, sectionCount:int, submodules:SubmoduleDto[] }
LiveSessionDto      { id, title, startsAt:datetime, durationMinutes:int, meetingUrl:string|null, isPast:bool }
MaterialDto         { id, title, typeCode, url:string|null, isLocked:bool }
TransactionDto      { id, transactionNo, gateway, userId, userName, userEmail, courseId, courseSlug, courseTitle,
                      methodCode, maskedAccount, originalAmount, discountAmount, amount, currency:"BDT",
                      statusCode, failureReason:string|null, fulfilledAt:datetime|null, createdAt, updatedAt,
                      allowedActions:string[] }          // admin only: next statuses (BR-PAY-10)
CertificateDto      { id, code, userId, courseId, userName, courseTitle, instructorName, modeCode,
                      examTypeCode:string|null, scorePercent:int|null, issuedAt, isRevoked:bool, revokedReason:string|null }
RuleDto             { courseId, isEnabled:bool, modeCode, minProgress:int, examTypeCode, examTitle, examInstructions,
                      maxScore:int, passingScore:int, maxAttempts:int }
SubmissionDto       { id, userId, userName, courseId, courseTitle, examTypeCode, examTitle, url, notes, submittedAt,
                      statusCode, score:number|null, feedback:string|null, reviewedBy:UserSummaryDto|null, reviewedAt }
PostDto             { id, courseId, title, body, author:{id:int|null, name:string, badge:"admin"|"instructor"|"student"},
                      createdAt, editedAt, isPinned, isLocked, isHidden, commentCount:int, canEdit:bool, canDelete:bool }
CommentDto          { id, postId, parentId:int|null, body, author:{...as PostDto}, createdAt, editedAt, isHidden,
                      canEdit:bool, canDelete:bool, replies:CommentDto[] }
```
Deleted author → `author.id = null`, `name = "Former member"` (BR-COM-13).

## 4. Auth
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| POST | `/auth/register` | `{ name (2–100), email (≤256), password (≥8, letter+digit), phone? (BD pattern), acceptTerms:true }` | `{ accessToken, accessTokenExpiresAt, refreshToken, me:MeDto }` (201) | `Register` | BR-AUTH-02,03,04,08 |
| POST | `/auth/login` | `{ email, password, remember:bool }` | same as register | `Login` | BR-AUTH-01,05 |
| POST | `/auth/refresh` | `{ refreshToken }` | `{ accessToken, accessTokenExpiresAt, refreshToken }` | interceptor | BR-AUTH-05 |
| POST | `/auth/logout` | `{ refreshToken }` | null | `Navbar` | BR-AUTH-10 |
| GET | `/auth/me` 🔒 signed in | — | `MeDto` | guards, `PermissionService` | BR-ACL-01,02,09 |

## 5. Access control
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/screens` 🔒 roles.view or access.view | — | `[{ code, label, area, path, icon, isCourseScoped, section, permissions:PermissionCode[] }]` | `AccessMatrix` | BR-ACL-12 |
| GET | `/users` 🔒 users.view or access.view | `search?, page, pageSize` | page of `{ id, name, email, roleCode, roleName, accessType:"full"\|"custom"\|"role", activeCourseCount }` | `AdminUsers`, `AdminAccess` | — |
| GET | `/users/{id}/access` 🔒 access.view | — | `{ user:UserSummaryDto, roleCode, isAdmin:bool, isCustom:bool, permissions:PermissionCode[], courseGrants:CourseGrantDto[], canEdit:bool, lockReason:string\|null }` | `UserAccess` | BR-ACL-01,08,10 |
| PUT | `/users/{id}/role` 🔒 roles.edit | `{ roleCode }` | `{ id, roleCode, roleName }` | `AdminAccess` | BR-ACL-08,17 |
| PUT | `/users/{id}/access` 🔒 access.edit | `{ screens: string[] }` (screen codes) | user access (as GET) | `UserAccess` | BR-ACL-08,10,12,19 |
| DELETE | `/users/{id}/access` 🔒 access.edit | — | user access | `UserAccess` | BR-ACL-11 |
| PUT | `/users/{id}/course-grants/{courseId}` 🔒 access.edit | `{ permissions: PermissionCode[] }` (course-grantable only; empty = remove) | `CourseGrantDto` \| null | `UserAccess` | BR-ACL-13 |
| GET | `/roles` 🔒 roles.view | — | `[{ id, code, name, description, isSystem, screens:string[], screenCount:int, memberCount:int }]` | `AdminRoles` | — |
| POST | `/roles` 🔒 roles.edit | `{ name (≤200, unique), description? (≤500), screens:string[] }` | role (201) | `AdminRoles` | BR-ACL-05,12,19 |
| PUT | `/roles/{id}` 🔒 roles.edit | same | role | `AdminRoles` | BR-ACL-05 |
| DELETE | `/roles/{id}` 🔒 roles.edit | — | null (409 if system or has users) | `AdminRoles` | BR-ACL-06,07 |

## 6. Catalogue (public)
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/categories` | — | `CategoryDto[]` | `Categories`, `CourseList`, `Navbar` | — |
| GET | `/courses` | `search?, category?, price? (all\|under-2500\|2500-4500\|over-4500), levels? (csv), formats? (csv), featured? (bool), sort? (popular\|rating\|newest\|price-asc\|price-desc), page, pageSize (default 6)` | page of `CourseCardDto` + `facets:{ categories:{code,count}[], levels:{code,count}[], formats:{code,count}[] }` | `CourseList`, `Courses`, `Sports` | BR-CAT-01,03–10 |
| GET | `/courses/{slug}` | — | `CourseCardDto` + `{ description, language, outcomes:string[], requirements:string[], instructor:InstructorDto, modules:ModuleDto[], schedule:LiveSessionDto[], materials:MaterialDto[], hasAccess:bool, isEnrolled:bool, isPublished:bool }` | `CourseDetail` | BR-CAT-02,13–20 |
| GET | `/courses/{slug}/related` | `limit? (default 4)` | `CourseCardDto[]` | `CourseDetail` | BR-CAT-11 |
| GET | `/courses/{slug}/reviews` | — | `{ reviews:{name,rating,date,text}[], breakdown:int[5] }` | `CourseDetail` | BR-CAT-12 (demo data, see Open Questions) |

## 7. Course management (admin)
`CourseUpsertDto` = `{ title (5–120), slug (pattern, unique), subtitle (≤200), description, categoryCode, formatCode, levelCode, instructorId, language (≤50), durationHours (≥1), originalPrice (≥0), price (≥0, ≤ originalPrice), coverLabel (≤30), coverIcon (≤50), imageUrl? (≤2048, http(s) or /path), badgeCode?, isFeatured, isPublished, outcomes:string[] (≥1), requirements:string[], schedule:{ id?:int, title, startsAt (Asia/Dhaka local "YYYY-MM-DDTHH:mm"), durationMinutes (≥1), meetingUrl }[], materials:{ id?:int, title, typeCode, url }[] }` — array order = SortOrder; rows without `id` are created, missing rows are soft-deleted.

`CurriculumDto` = `{ modules: { id?, title, submodules: { id?, title, sections: SectionInput[] (2–3) }[] }[] }`; `SectionInput` = `{ id?, title, videos: { id?, tempKey?, title, url, minutes (≥1), isPreview }[], documents: { id?, title, typeCode, url, videoId?:int, videoTempKey?:string }[], topics: SectionInput[] (depth ≤ 3) }`. `tempKey` lets a new document point to a new video in the same save.

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/admin/courses` 🔒 courses.view (course) | `search?, format?, page, pageSize` | page of `{ id, slug, title, instructorName, formatCode, price, originalPrice, discountPercent, sectionCount, sessionCount, materialCount, activeEnrollmentCount, isPublished, canEdit, canDelete, canManageCertificates }` | `AdminCourses` | BR-CMS-16 |
| GET | `/admin/courses/{id}` 🔒 courses.edit (course) | — | `CourseUpsertDto` + `{ id, sectionCount, modules:ModuleDto[] }` | `CourseForm`, `CurriculumEditor` | BR-CMS-01 |
| GET | `/admin/courses/slug-available` 🔒 courses.create or courses.edit | `slug, exceptId?` | `{ available:bool }` | `CourseForm` | BR-CMS-02 |
| POST | `/admin/courses` 🔒 courses.create | `CourseUpsertDto` | `{ id, slug }` (201) | `CourseForm` | BR-CMS-01–05,11,17 |
| PUT | `/admin/courses/{id}` 🔒 courses.edit (course) | `CourseUpsertDto` | `{ id, slug }` | `CourseForm` | BR-CMS-03–05,11,17 |
| PATCH | `/admin/courses/{id}/publish` 🔒 courses.edit (course) | `{ isPublished:bool }` | `{ id, isPublished }` | `AdminCourses` | BR-CMS-12 |
| PUT | `/admin/courses/{id}/curriculum` 🔒 courses.edit (course) | `CurriculumDto` | `{ modules:ModuleDto[], sectionCount }` | `CurriculumEditor` | BR-CMS-06–10,18 |
| DELETE | `/admin/courses/{id}` 🔒 courses.delete (course) | — | null (soft-deletes course + enrollments + posts/comments + grants) | `AdminCourses` | BR-CMS-13 |
| GET | `/admin/instructors` 🔒 instructors.view | — | `InstructorDto[]` + `assignedCourseCount` | `AdminInstructors` | — |
| GET | `/instructors/options` 🔒 courses.create or courses.edit | — | `{ id, name }[]` | `CourseForm` | — |
| POST | `/admin/instructors` 🔒 instructors.create | `{ name (2–100), title (≤200), bio, expertise:string[], experienceYears (≥0), students (≥0) }` | `InstructorDto` (201) | `AdminInstructors` | BR-CMS-15 |
| PUT | `/admin/instructors/{id}` 🔒 instructors.edit | same | `InstructorDto` | `AdminInstructors` | BR-CMS-15 |
| DELETE | `/admin/instructors/{id}` 🔒 instructors.delete | — | null (409 if assigned to any course) | `AdminInstructors` | BR-CMS-14 |

## 8. Checkout & payments
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/payment-methods` | — | `{ code, name, description, logoUrl, icon }[]` | `Checkout`, `PaymentMethods` | — |
| GET | `/checkout/{slug}` 🔒 checkout.view | — | `{ course:CourseCardDto, instructorName, originalPrice, discountAmount, amount, discountPercent, isFree:bool, alreadyEnrolled:bool }` | `Checkout` | BR-PAY-01,02 |
| POST | `/checkout/{slug}/enroll-free` 🔒 checkout.view | — | `{ enrollmentId, courseSlug }` (201; 409 if price > 0 or already enrolled) | `Checkout` | BR-PAY-03 |
| POST | `/payments` 🔒 checkout.view | `{ courseSlug, methodCode }` (amount computed by server) | `{ transactionNo, redirectUrl }` (201) | `Checkout` | BR-PAY-04,05 |
| POST | `/payments/callback/{gateway}` (gateway → server, signature-verified, no JWT) | gateway payload | gateway-specific | real gateway | BR-PAY-06–08 |
| GET | `/payments/{transactionNo}` 🔒 buyer or transactions.view | — | `TransactionDto` (without `allowedActions`) + `{ canOpenCourse:bool }` | `PaymentResult`, `DemoGateway` | BR-PAY-08,09,18 |
| GET | `/me/payments` | `page, pageSize` | page of `TransactionDto` (newest first) | `Dashboard` | BR-LRN-14 |
| GET | `/admin/transactions` 🔒 transactions.view | `search?, status?, method?, courseId?, from? (YYYY-MM-DD), to? (YYYY-MM-DD), page, pageSize` | page of `TransactionDto` + `totals:{ collected, refunded, successful:int, unsuccessful:int }` + `courseOptions:{ id, title }[]` | `AdminTransactions` | BR-PAY-13,14 |
| PATCH | `/admin/transactions/{id}/status` 🔒 transactions.edit | `{ statusCode: "success"\|"cancelled"\|"refunded" }` | `TransactionDto` (409 if transition not allowed) | `AdminTransactions` | BR-PAY-10,11 |
| DELETE | `/admin/transactions/{id}` 🔒 transactions.delete | — | null (409 if status = success) | `AdminTransactions` | BR-PAY-12 |
| POST | `/dev/payments/{transactionNo}/simulate` (development only) | `{ outcome: "success"\|"failed"\|"cancelled" }` | `TransactionDto` | `DemoGateway` | BR-PAY-06,07 |

`from`/`to` are the admin's local days; server converts Asia/Dhaka day bounds to UTC (BR-PAY-13).

## 9. Enrollments
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/me/enrollments` | — | `{ enrollmentId, statusCode, enrolledAt, course:CourseCardDto, progressPercent:int, completedCount:int, sessionCount:int }[]` (courses that still exist) | `Dashboard` | BR-ENR-10, BR-LRN-12 |
| GET | `/admin/enrollments` 🔒 enrollments.view | `search?, status? (active\|revoked), page, pageSize` | page of `{ id, statusCode, enrolledAt, userId, studentName, email, courseId, courseTitle, transactionNo:string\|null }` (newest first) | `AdminEnrollments` | BR-ENR-09 |
| PATCH | `/admin/enrollments/{id}/status` 🔒 enrollments.edit | `{ statusCode: "active"\|"revoked" }` | `{ id, statusCode }` | `AdminEnrollments` | BR-ENR-05 |

## 10. Learning & progress
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/learn/{slug}` 🔒 learn.view + course access | — | `{ course:CourseCardDto, instructor:InstructorDto, modules:ModuleDto[] (all urls), schedule:LiveSessionDto[] (with meetingUrl), materials:MaterialDto[] (with url), canReadCommunity:bool }` | `Learn` | BR-LRN-01, BR-ENR-03 |
| GET | `/me/courses/{courseId}/progress` | — | `{ completedSectionIds:int[], completedCount, totalSections, percent }` | `Learn` | BR-LRN-04,05 |
| PUT | `/me/courses/{courseId}/progress/sections/{sectionId}` 🔒 learn.view + course access | `{ completed:bool }` | `{ completedCount, totalSections, percent, certificateIssued:CertificateDto\|null }` | `Learn` | BR-LRN-02,03,06,07 |
| GET | `/me/upcoming-sessions` | `limit? (default 5)` | `{ session:LiveSessionDto, courseSlug, courseTitle }[]` | `Dashboard` | BR-LRN-11 |

## 11. Certification
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/admin/courses/{id}/certification-rule` 🔒 certificates.manage (course) | — | `RuleDto` (default if none) | `CertificationSettings` | BR-CERT-01 |
| PUT | `/admin/courses/{id}/certification-rule` 🔒 certificates.manage (course) | `RuleDto` without courseId | `RuleDto` | `CertificationSettings` | BR-CERT-02 |
| GET | `/me/courses/{courseId}/certification` | — | `{ rule:RuleDto, progress:int, progressMet:bool, submissions:SubmissionDto[], attemptsLeft:int\|null, canSubmit:bool, certificate:CertificateDto\|null }` | `CourseCertificate` | BR-CERT-06,07,16 |
| POST | `/me/courses/{courseId}/certificate` | — | `CertificateDto` \| null | `CourseCertificate` | BR-CERT-03,04 |
| POST | `/me/courses/{courseId}/exam-submissions` 🔒 learn.view + active enrollment | `{ url (https, ≤2048), notes? (≤1000) }` | `SubmissionDto` (201) | `CourseCertificate` | BR-CERT-06,08 |
| GET | `/me/certificates` | — | `CertificateDto[]` (newest first) | `Dashboard` | — |
| GET | `/admin/exam-submissions` 🔒 certificates.manage (course) | `status? (submitted\|passed\|failed; default submitted), courseId?, page, pageSize` | page of `SubmissionDto` | `AdminCertificates` | — |
| POST | `/admin/exam-submissions/{id}/review` 🔒 certificates.manage (course) | `{ score (0–maxScore), feedback? (≤1000) }` | `{ submission:SubmissionDto, certificate:CertificateDto\|null }` (409 if not `submitted`) | `AdminCertificates` | BR-CERT-09,10 |
| GET | `/admin/certificates` 🔒 certificates.manage (course) | `search?, page, pageSize` | page of `CertificateDto` | `AdminCertificates` | — |
| POST | `/admin/certificates/{id}/revoke` 🔒 certificates.manage (course) | `{ reason? (≤200) }` | `CertificateDto` | `AdminCertificates` | BR-CERT-11 |
| POST | `/admin/certificates/{id}/restore` 🔒 certificates.manage (course) | — | `CertificateDto` (409 if another valid one exists) | `AdminCertificates` | BR-CERT-11 |
| GET | `/certificates/verify/{code}` (public) | — | `{ code, isRevoked, revokedReason, userName, courseTitle, modeCode, examTypeCode, scorePercent, instructorName, issuedAt }` (404 if unknown) | `Verify` | BR-CERT-13 |

## 12. Community
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/courses/{slug}/community` | — | `{ canRead, canPost, canModerate, publicPostCount:int }` | `CourseCommunity`, `Learn` | BR-COM-01–03,14 |
| GET | `/courses/{slug}/posts` 🔒 can read | `page, pageSize` | page of `PostDto` (pinned first, then newest; hidden only for moderators) | `CourseCommunity` | BR-COM-05 |
| POST | `/courses/{slug}/posts` 🔒 can post | `{ title (1–120), body (1–2000) }` | `PostDto` (201) | `CourseCommunity` | BR-COM-03,11 |
| PUT | `/posts/{id}` 🔒 author | `{ title, body }` | `PostDto` | `CourseCommunity` | BR-COM-08 |
| DELETE | `/posts/{id}` 🔒 author or moderator | — | null (soft-deletes its comments) | `CourseCommunity`, `AdminCommunity` | BR-COM-09 |
| PATCH | `/posts/{id}/flags` 🔒 moderator | `{ isPinned?, isLocked?, isHidden? }` | `PostDto` | `CourseCommunity`, `AdminCommunity` | BR-COM-10 |
| GET | `/posts/{id}/comments` 🔒 can read | — | `CommentDto[]` (top-level oldest first, with replies) | `CourseCommunity` | BR-COM-06,07 |
| POST | `/posts/{id}/comments` 🔒 can comment | `{ body (1–1000), parentId? }` | `CommentDto` (201; 409 "This discussion is locked.") | `CourseCommunity` | BR-COM-04,07,11 |
| PUT | `/comments/{id}` 🔒 author | `{ body }` | `CommentDto` | `CourseCommunity` | BR-COM-08 |
| DELETE | `/comments/{id}` 🔒 author or moderator | — | null (soft-deletes replies) | `CourseCommunity`, `AdminCommunity` | BR-COM-09 |
| PATCH | `/comments/{id}/hidden` 🔒 moderator | `{ isHidden:bool }` | `CommentDto` | `CourseCommunity`, `AdminCommunity` | BR-COM-10 |
| GET | `/admin/community/posts` 🔒 community.moderate (course) | `courseId?, status? (all\|visible\|hidden\|pinned\|locked), search?, page, pageSize` | page of `PostDto` + `courseTitle, courseSlug` | `AdminCommunity` | BR-COM-16 |
| GET | `/admin/community/comments` 🔒 community.moderate (course) | `courseId?, status? (all\|visible\|hidden), search?, limit? (default 20)` | `CommentDto` + `postTitle, courseTitle, courseSlug` | `AdminCommunity` | BR-COM-16 |

## 13. Admin overview
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/admin/overview` 🔒 reports.view | — | `{ courseCount, publishedCourseCount, studentCount, activeEnrollmentCount, collectedAmount, recentTransactions:TransactionDto[5] }` | `AdminOverview` | BR-ADM-01–05 |
| POST | `/dev/reset` 🔒 system.reset (development only) | — | null | `AdminOverview` | BR-ADM-06 |

## 14. Site content
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| — | Contact / career forms | — | — | `Contact`, `Career` | Not designed: storage is an Open Question (BR-SITE-01,02) |

Static content (stories, opportunities, FAQ, testimonials, contact info) stays in the frontend for now.

## 15. Open questions (API)
- Access/refresh token lifetimes and refresh-token rotation (also in data-model).
- Is `ErrorItem { field, message }` the shape you want, or should it carry an error `code` too?
- Should validation messages be returned in English only (frontend translates) or in the request language (`Accept-Language`)?
- Contact/career form endpoints wait for the storage decision.
- Reviews endpoint returns demo data until the Review table is decided.
