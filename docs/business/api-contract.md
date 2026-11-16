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
| 429 | Too many sign-in attempts | "Too many attempts. Please wait a minute and try again." |
| 409 | Business conflict (duplicate email/slug/role name, invalid status change, already reviewed, etc.) | rule's message |

## 3. Shared DTOs
```text
UserSummaryDto      { id:int, name:string, email:string }
PermissionCode      string  (e.g. "courses.edit")
CourseGrantDto      { courseId:int, permissions:PermissionCode[] }
MeDto               { id:int, name, email, phone:string|null, roleCode:string, roleName:string,
                      permissions:PermissionCode[], courseGrants:CourseGrantDto[], hasAdminPanel:bool, homeUrl:string, createdAt }
CategoryDto         { code, name, shortName, description, icon, imageUrl, skills:string[], publishedCourseCount:int }
InstructorDto       { id, name, title, bio, expertise:string[], experienceYears:int, students:int, rating:number,
                      publishedCourseCount:int }
CourseCardDto       { id, slug, title, subtitle, categoryCode, formatCode, levelCode, badgeCode:string|null,
                      price:number, originalPrice:number, discountPercent:int, rating:number, ratingCount:int,
                      students:int, durationHours:int, coverLabel, coverIcon, imageUrl:string|null,
                      instructorName:string, sectionCount:int, contentUpdatedMonth:"YYYY-MM" }
VideoDto            { id, title, minutes:int, isPreview:bool, sourceType, url:string|null, hasMedia, isLocked, media:MediaAssetDto|null }   // url null when locked or an upload (section 7a)
DocumentDto         { id, title, typeCode, videoId:int|null, sourceType, url:string|null, hasMedia, isLocked:bool, media:MediaAssetDto|null }
SectionDto          { id, title, depth:int, minutes:int, videos:VideoDto[], documents:DocumentDto[], topics:SectionDto[] }
SubmoduleDto        { id, title, sections:SectionDto[] }
ModuleDto           { id, title, minutes:int, sectionCount:int, submodules:SubmoduleDto[] }
LiveSessionDto      { id, title, startsAt:datetime, durationMinutes:int, meetingUrl:string|null, isPast:bool }
MaterialDto         { id, title, typeCode, sourceType, url:string|null, hasMedia, isLocked:bool, media:MediaAssetDto|null }
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

## 3a. Lookups
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/lookups` (public) | — | `{ categories:CategoryDto[], courseFormats, courseLevels, courseBadges, documentTypes:{code,name,fileExtension}[], materialTypes, enrollmentStatuses, paymentStatuses, paymentMethods:{code,name,description,logoUrl,icon}[], currencies }` (each plain list = `{code,name}[]`, in display order) | forms and filters | — |
| GET | `/health` (no `/api/v1` prefix) | — | `{ status, checks:{ database } }` | monitoring | — |

## 4. Auth (built in backend Step 2)
Tokens: access token (JWT, HS256, 15 min, claims `sub` = user id and `jti` only; permissions are read from the database on every request). Refresh token: 64 random bytes, stored as a SHA-256 hash, single use (rotation). Lifetime 30 days with "Remember me", otherwise 24 hours. Reusing an already-used refresh token revokes every session of that user. `register`, `login` and `refresh` are rate-limited per client address (20 per minute → `429`).

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| POST | `/auth/register` | `{ name (2–100), email (≤256), password (8–100, letter+digit), phone? (BD pattern), acceptTerms:true }` | `AuthResultDto` = `{ accessToken, accessTokenExpiresAt, refreshToken, me:MeDto }` (201; 409 duplicate email) | `Register` | BR-AUTH-02,03,04,08 |
| POST | `/auth/login` | `{ email, password, remember:bool }` | `AuthResultDto` (401 "Incorrect email or password." for unknown email or wrong password) | `Login` | BR-AUTH-01,05 |
| POST | `/auth/refresh` | `{ refreshToken }` | `{ accessToken, accessTokenExpiresAt, refreshToken }` (401 "Your session has expired. Please log in again.") | `authInterceptor` | BR-AUTH-05 |
| POST | `/auth/logout` | `{ refreshToken? }` | null (always 200; works with an expired access token) | `Navbar` | BR-AUTH-10 |
| GET | `/auth/me` 🔒 signed in | — | `MeDto` | app start, guards | BR-ACL-01,02,09 |

## 5. Access control (built in backend Step 2)
Access is stored per **screen** (codes = frontend screen ids). A screen gives all of its permissions (BR-ACL-12). Roles are addressed by `code`.

`RoleDto` = `{ code, name, description, isSystem, screens:string[], permissions:PermissionCode[], memberCount }` (admin role: every screen).
`UserListItemDto` = `{ id, name, email, phone, roleCode, roleName, accessType:"full"|"custom"|"role", customScreens:string[]|null, courseGrants:CourseGrantDto[], createdAt }`.
`UserAccessDto` = `{ user:UserSummaryDto, roleCode, accessType, screens:string[], permissions:PermissionCode[], courseGrants:CourseGrantDto[], canEdit:bool, lockReason:string|null }`.

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/screens` 🔒 roles.view or access.view | — | `[{ code, label, area, path, icon, isCourseScoped, section, permissions:PermissionCode[] }]` | (reference) | BR-ACL-12 |
| GET | `/users` 🔒 users.view or access.view | `search?, page, pageSize` | page of `UserListItemDto` (oldest first) | `AdminUsers`, `AdminAccess`, `UserAccess` | — |
| GET | `/users/{id}/access` 🔒 access.view | — | `UserAccessDto` | (reference) | BR-ACL-01,08,10 |
| PUT | `/users/{id}/role` 🔒 roles.edit | `{ roleCode }` | `UserAccessDto` (409 own role) | `AdminAccess` | BR-ACL-08,17 |
| PUT | `/users/{id}/access` 🔒 access.edit | `{ screens:string[] }` | `UserAccessDto` (409 own access / admin user; 400 unknown screen) | `UserAccess` | BR-ACL-08,10,12,19 |
| DELETE | `/users/{id}/access` 🔒 access.edit | — | `UserAccessDto` | `UserAccess` | BR-ACL-11 |
| PUT | `/users/{id}/course-grants/{courseId}` 🔒 access.edit | `{ permissions:PermissionCode[] }` (course-grantable only; empty = remove) | `UserAccessDto` (400 non-grantable permission) | `UserAccess` | BR-ACL-13 |
| GET | `/roles` 🔒 roles.view, access.view or users.view | — | `RoleDto[]` | `AdminRoles`, `AdminAccess`, `AdminUsers` | — |
| POST | `/roles` 🔒 roles.edit | `{ name (≤200, unique), description? (≤500), screens:string[] }` | `RoleDto` (201; 409 duplicate name) | `AdminRoles` | BR-ACL-05,12 |
| PUT | `/roles/{code}` 🔒 roles.edit | same | `RoleDto` (admin role: screens ignored) | `AdminRoles` | BR-ACL-05 |
| DELETE | `/roles/{code}` 🔒 roles.edit | — | null (409 built-in role or role in use) | `AdminRoles` | BR-ACL-06,07 |

## 6. Catalogue (built in backend Step 3)
Public endpoints. When a token is sent, people who manage a course (`courses.view` on it) also get its drafts, items without a file and every link.

`CourseDetailDto` = `{ id, slug, title, subtitle, description, categoryCode, formatCode, levelCode, badgeCode, isPublished, isFeatured, instructorId, language, durationHours, price, originalPrice, discountPercent, coverLabel, coverIcon, imageUrl, rating, ratingCount, students, contentUpdatedMonth, sectionCount, outcomes[], requirements[], modules:ModuleDto[], schedule:{ id, title, startsAt (Asia/Dhaka "YYYY-MM-DDTHH:mm"), durationMinutes, meetingUrl|null }[], materials:{ id, title, typeCode, sourceType, url|null, hasMedia, isLocked, media:MediaAssetDto|null }[], hasContentAccess }`
`ModuleDto` = `{ id, title, submodules:{ id, title, sections:SectionDto[] }[] }`; `SectionDto` = `{ id, title, depth, videos:{ id, title, minutes, isPreview, sourceType:"external"|"upload"|null, url|null, hasMedia, isLocked, media:MediaAssetDto|null }[], documents:{ id, title, typeCode, videoId|null, sourceType, url|null, hasMedia, isLocked, media:MediaAssetDto|null }[], topics:SectionDto[] }`
`CourseCardDto` = `{ id, slug, title, subtitle, categoryCode, formatCode, levelCode, badgeCode, isPublished, isFeatured, instructorId, instructorName, price, originalPrice, discountPercent, rating, ratingCount, students, durationHours, language, coverLabel, coverIcon, imageUrl, contentUpdatedMonth, sectionCount }`
`url` is null when the item is locked for the caller (BR-CAT-13 to 15) and always for uploads (`sourceType: "upload"`), which are opened through the media endpoints (section 7a, BR-CAT-22). `media` (the upload's details and status) is only sent to managers. Items with no link or no `ready` file are left out for everyone but managers (BR-CAT-21).

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/courses` | `search?, category?, price? (all\|under-2500\|2500-4500\|over-4500), levels? (csv), formats? (csv), featured?, sort? (popular\|rating\|newest\|price-asc\|price-desc), page, pageSize` | page of `CourseCardDto` (published only) | (paged catalogue; not used by the current frontend) | BR-CAT-01,03–06 |
| GET | `/courses/all` | — | `CourseDetailDto[]` — every course the caller may see | `CourseService` (keeps the catalogue in memory) | BR-CAT-01,02,13–15,21 |
| GET | `/courses/{slug}` | — | `CourseDetailDto` (404 for drafts the caller may not manage) | (reference) | BR-CAT-02 |
| GET | `/courses/{slug}/related` | `limit? (default 4, max 12)` | `CourseCardDto[]` | (reference) | BR-CAT-11 |
| GET | `/instructors` | — | `{ id, name, title, bio, expertise[], experienceYears, students, rating }[]` | `CourseService` | BR-CAT-18 |
| — | Reviews | — | not built: reviews and rating breakdown stay demo data in the frontend (see Open Questions) | `CourseDetail` | BR-CAT-12 |

## 7. Course management (built in backend Step 3)
`CourseUpsertRequest` = `{ title (5–120), slug (pattern, unique), subtitle (≤200), description (≤8000), categoryCode, formatCode, levelCode, badgeCode?, instructorId, language (≤50), durationHours (≥1), originalPrice (≥0), price (0..originalPrice), coverLabel (≤30), coverIcon (≤50), imageUrl? (http(s) or /path), isFeatured, isPublished, outcomes[] (≥1), requirements[], schedule:{ id?, title, startsAt (Asia/Dhaka "YYYY-MM-DDTHH:mm"), durationMinutes (≥1), meetingUrl }[], materials:{ id?, title, typeCode, url? (required for Link), mediaAssetId? (uploaded document of this course; not for Link; not with url; not on a new course) }[] }`. Rows without `id` are created, rows left out are removed; list order = display order.
`CurriculumRequest` = `{ modules:{ id?, title, submodules:{ id?, title, sections:SectionInput[] (2–3) }[] }[] }`; `SectionInput` = `{ id?, title, videos:{ id?, tempKey?, title, url?, minutes (≥1), isPreview, mediaAssetId? }[], documents:{ id?, title, typeCode, url?, videoId?, videoTempKey?, mediaAssetId? }[], topics:SectionInput[] (depth ≤ 3) }`. A new video needs `mediaAssetId` (an uploaded video of this course) and no `url`; an existing video may resend its unchanged `url`, or a `mediaAssetId` to replace its media; both null keep its media (BR-CMS-24). Documents send their full state: `url`, `mediaAssetId` or neither. Files are checked by BR-CMS-23; files the save releases are deleted (BR-CMS-25). `tempKey` lets a new document point to a new video. Ids from another course → 400 "Something in this course changed in the meantime…".

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| POST | `/admin/courses` 🔒 courses.create | `CourseUpsertRequest` (must be a draft) | `CourseDetailDto` (201) | `CourseForm` | BR-CMS-01–05,11,17 |
| PUT | `/admin/courses/{id}` 🔒 courses.edit (course) | `CourseUpsertRequest` | `CourseDetailDto` | `CourseForm` | BR-CMS-01–05,11,17 |
| PATCH | `/admin/courses/{id}/publish` 🔒 courses.edit (course) | `{ isPublished }` | `CourseDetailDto` (400 if BR-CMS-04/05 fail) | `AdminCourses` | BR-CMS-12 |
| PUT | `/admin/courses/{id}/curriculum` 🔒 courses.edit (course) | `CurriculumRequest` | `CourseDetailDto` | `CurriculumEditor` | BR-CMS-06–10,18,19 |
| DELETE | `/admin/courses/{id}` 🔒 courses.delete (course) | — | null (soft-deletes the course, removes its grants) | `AdminCourses` | BR-CMS-13 |
| GET | `/admin/courses/slug-available` 🔒 courses.create or courses.edit | `slug, exceptId?` | `{ available }` | (reference) | BR-CMS-02 |
| POST | `/instructors` 🔒 instructors.create | `{ name (2–100), title (≤200), bio (≤4000), expertise[], experienceYears (≥0), students (≥0) }` | instructor (201) | `AdminInstructors` | BR-CMS-15 |
| PUT | `/instructors/{id}` 🔒 instructors.edit | same | instructor | `AdminInstructors` | BR-CMS-15 |
| DELETE | `/instructors/{id}` 🔒 instructors.delete | — | null (409 while it teaches any course) | `AdminInstructors` | BR-CMS-14 |

## 7a. Media (built in backend Step 6)
Follows `docs/integration/streaming-integration.md`. The API checks permissions and course access before every call to the Streaming service; 503 "The file service is not available right now…" when Streaming cannot be reached (retries, timeouts and a circuit breaker are applied first).
```text
MediaAssetDto  { id, courseId, kindCode:"video"|"document", statusCode, fileName, mimeType, sizeBytes, durationSeconds|null, errorMessage|null }
MediaUploadDto { mediaAssetId, uploadUrl, uploadMethod:"PUT", headers:{}, expiresAt }   // the browser sends the bytes to uploadUrl directly
PlaybackDto    { url, format:"hls"|"file", expiresAt, durationSeconds|null }           // 10-minute link
DownloadDto    { url, expiresAt }                                                       // 10-minute link
```
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| POST | `/media/uploads` 🔒 courses.edit (course) | `{ courseId, kind ("video"\|"document"), fileName (≤255), mimeType, sizeBytes }` | `MediaUploadDto` (201); 400 wrong type/size | `MediaService.upload` | BR-CMS-21 |
| POST | `/media/uploads/{id}/complete` 🔒 courses.edit (course) | — | `MediaAssetDto` (409 link expired / bytes not received) | `MediaService.upload` | BR-CMS-21,22 |
| GET | `/media/assets/{id}` 🔒 courses.edit (course) | — | `MediaAssetDto` (asks Streaming when stuck in progress > 30 s) | editor polling | BR-CMS-22 |
| DELETE | `/media/assets/{id}` 🔒 courses.edit (course) | — | null (409 "This file is still used in the course…") | editor (unsaved upload replaced) | BR-CMS-25 |
| POST | `/media/videos/{id}/playback` (anonymous allowed) | — | `PlaybackDto`; 401/403 without access; 409 still processing; 400 for link videos | `VideoEmbed` | BR-CAT-13,22 |
| POST | `/media/documents/{id}/download` (anonymous allowed) | — | `DownloadDto` (same checks; documents of preview videos are open) | `DocumentList` | BR-CAT-13,14,22 |
| POST | `/media/materials/{id}/download` (anonymous allowed) | — | `DownloadDto` (course access) | `Learn` | BR-CAT-14,22 |
| POST | `/internal/streaming/webhooks` (Streaming only) | raw JSON event; headers `X-Streaming-Event-Id`, `X-Streaming-Timestamp`, `X-Streaming-Signature` | null; 401 bad/old signature; 400 unreadable; 5xx = Streaming retries | — | streaming spec §6 |
| PUT/GET | `/dev/fake-streaming/uploads/{assetId}`, `/dev/fake-streaming/files/{assetId}` (Development, `Streaming:Mode = Fake` only; signed, expiring links) | bytes / — | stand-in for Streaming storage | — | streaming spec §8 |

## 8. Checkout & payments (built in backend Step 4)
`TransactionDto` = `{ transactionNo, gateway, userId, userName, userEmail, courseId, courseSlug, courseTitle, methodCode, maskedAccount, originalAmount, discountAmount, amount, currency, statusCode, failureReason, fulfilledAt, expiresAt, createdAt, updatedAt, allowedActions[] (admin only), canOpenCourse }`. Payments are addressed by their public `transactionNo`.

Gateways are plug-ins (`IPaymentGateway`: initiate, verify callback, query status). `Payments:Gateway` picks the one new payments use; the demo gateway is refused outside Development. All payment rules live in `PaymentService`, the same for every gateway.

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/payment-methods` | — | via `/lookups` (`paymentMethods`) | `Checkout`, `PaymentMethods` | — |
| GET | `/payments/checkout/{slug}` 🔒 checkout.view | — | `{ course:CourseCardDto, originalPrice, discountAmount, amount, discountPercent, isFree, alreadyEnrolled, methods[] }` | (reference) | BR-PAY-01,02 |
| POST | `/enrollments/free/{slug}` 🔒 checkout.view | — | `EnrollmentDto` (201; 409 if not free or already enrolled) | `Checkout` | BR-PAY-03 |
| POST | `/payments` 🔒 checkout.view | `{ courseSlug, methodCode }` — no amount | `{ transactionNo, redirectUrl, isExternal }` (201; 409 free course / already enrolled) | `Checkout` | BR-PAY-04,05,19,22 |
| POST | `/payments/callback/{gateway}` (anonymous; the gateway verifies signature + status) | provider payload (raw body, headers, query, form) | null (400 if not verified) | payment provider | BR-PAY-06–08,21 |
| GET | `/payments/{transactionNo}` 🔒 buyer or transactions.view | — | `TransactionDto` (expires an overdue pending payment; may ask the provider for its status) | `PaymentResult`, `DemoGateway` | BR-PAY-08,09,18,20 |
| POST | `/payments/{transactionNo}/demo-outcome` 🔒 buyer, demo gateway only | `{ outcome: "success"\|"failed"\|"cancelled" }` | `TransactionDto` (only a pending, unexpired payment changes) | `DemoGateway` | BR-PAY-06,07,21 |
| GET | `/payments/mine` | — | `TransactionDto[]` (newest first) | `Dashboard` | BR-LRN-14 |
| GET | `/admin/transactions` 🔒 transactions.view | `search?, status?, method?, courseId?, from? (YYYY-MM-DD), to? (YYYY-MM-DD), page, pageSize` | `{ items:TransactionDto[], total, page, pageSize, totals:{ collected, refunded, successful, unsuccessful }, courseOptions:{ id, title }[] }` | `AdminTransactions` | BR-PAY-13,14 |
| PATCH | `/admin/transactions/{transactionNo}/status` 🔒 transactions.edit | `{ statusCode: "success"\|"cancelled"\|"refunded" }` | `TransactionDto` (409 if not allowed) | `AdminTransactions` | BR-PAY-10,11 |
| DELETE | `/admin/transactions/{transactionNo}` 🔒 transactions.delete | — | null (409 if status = success; soft delete) | `AdminTransactions` | BR-PAY-12 |

`from`/`to` are Bangladesh days; the server converts them to UTC (BR-PAY-13).

## 9. Enrollments (built in backend Step 4)
`EnrollmentDto` = `{ id, userId, studentName, studentEmail, courseId, courseTitle, statusCode, enrolledAt, transactionNo }`. Course access (locked lessons, links) is worked out by the API from active enrollments (BR-ENR-03).

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/enrollments/mine` | — | `EnrollmentDto[]` (newest first, revoked included) | `Dashboard`, `EnrollmentService` | BR-ENR-10 |
| GET | `/admin/enrollments` 🔒 enrollments.view | `search?, status? (active\|revoked), page, pageSize` | page of `EnrollmentDto` (newest first) | `AdminEnrollments` | BR-ENR-09 |
| PATCH | `/admin/enrollments/{id}/status` 🔒 enrollments.edit | `{ statusCode: "active"\|"revoked" }` | `EnrollmentDto` | `AdminEnrollments` | BR-ENR-05 |

## 10. Learning & progress (built in backend Step 5)
The learning page reads the course from `GET /courses/{slug}` (section 6): with course access it carries all lesson links (BR-ENR-03).
A lesson is a video, or a section without videos (BR-LRN-15).
```text
VideoProgressDto          { videoId, positionSeconds:int, durationSeconds:int|null, isCompleted:bool }
CourseProgressDto         { courseId, completedLessons:int, totalLessons:int, percent:int,
                            completedSectionIds:int[],          // sections whose lessons are all done
                            videos:VideoProgressDto[] }         // started videos only
CourseProgressSummaryDto  { courseId, completedLessons, totalLessons, percent }
```
🔒 below = `learn.view` + course access (active enrollment, or `courses.view` on the course); otherwise 403 "Enroll in this course to track your progress." (404 for an unpublished course on the GET).

| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/progress/mine` | — | `CourseProgressSummaryDto[]` (every course the user has an enrollment for, revoked included) | `Dashboard` | BR-LRN-16 |
| GET | `/progress/courses/{slug}` 🔒 | — | `CourseProgressDto` | `Learn` | BR-LRN-05,15,16 |
| PUT | `/progress/videos/{videoId}/complete` 🔒 | `{ completed:bool }` | `CourseProgressDto` (404 "Video not found.") | `Learn` | BR-LRN-02,03 |
| PUT | `/progress/videos/{videoId}/position` 🔒 | `{ positionSeconds (0–86400), durationSeconds (1–86400) }` | `CourseProgressDto`; 400 "This video's position can't be saved. Use Mark as complete." for YouTube/Vimeo | `Learn` (player, every 15 s, on pause and at the end) | BR-LRN-17,18 |
| PUT | `/progress/sections/{sectionId}/complete` 🔒 | `{ completed:bool }` | `CourseProgressDto`; 400 "Mark the videos of this section as complete instead." if the section has videos | `Learn` | BR-LRN-02,03,15 |
| GET | `/me/upcoming-sessions` (not built) | `limit? (default 5)` | `{ session:LiveSessionDto, courseSlug, courseTitle }[]` | `Dashboard` (works it out from the catalogue for now) | BR-LRN-11 |

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

## 13. Admin overview (overview built in backend Step 5)
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| GET | `/admin/overview` 🔒 reports.view | — | `{ courseCount, publishedCourseCount, studentCount, activeEnrollmentCount, collectedAmount, recentTransactions:{ transactionNo, createdAt, userName, courseTitle, amount, statusCode }[5] }` | `AdminOverview` | BR-ADM-01–05 |
| POST | `/dev/reset` 🔒 system.reset (development only) | — | null | `AdminOverview` | BR-ADM-06 |

## 14. Site content
| Method | Endpoint | Request | Response `data` | Used by | Rules |
|---|---|---|---|---|---|
| — | Contact / career forms | — | — | `Contact`, `Career` | Not designed: storage is an Open Question (BR-SITE-01,02) |

Static content (stories, opportunities, FAQ, testimonials, contact info) stays in the frontend for now.

## 15. Open questions (API)
- Is `ErrorItem { field, message }` the shape you want, or should it carry an error `code` too?
- Should validation messages be returned in English only (frontend translates) or in the request language (`Accept-Language`)?
- Contact/career form endpoints wait for the storage decision.
- Reviews endpoint returns demo data until the Review table is decided.
