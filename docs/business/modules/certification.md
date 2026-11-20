# Certification (Rules, Central Exam, Certificates, Verification)

## 1. Purpose
Awards each course a certificate either on completion or after passing a "central exam" (a project or presentation scored by an instructor), and lets anyone verify a certificate by its public code.
Served by the API (backend Step 8, tables in the `Certification` schema); the browser-storage demo mode follows the same rules.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/admin/courses/:id/certification` | `CertificationSettings` | Per-course rule: enabled, mode (completion / exam), required progress %, and for exam: type, title, instructions, max score, pass mark, max attempts. Guard `certificates.manage` on course. |
| `/admin/certificates` | `AdminCertificates` | (1) Exam submissions to review (filter status, default "submitted"): student, course, exam title/type, link, notes; Review → score + feedback. (2) Issued certificates (search code/name/course): download PDF, Revoke (with reason ≤ 200), Restore. Only courses the user can manage. |
| `/learn/:slug` (box) | `CourseCertificate` | Student status: requirement text, progress step, exam step (pass mark, attempts left), past submissions with result/feedback, hand-in form (link + notes), "Get my certificate" (completion), earned certificate (download PDF, verification page), or "Your certificate for this course was revoked." with code and reason (BR-CERT-17). |
| `/dashboard` (section) | `Dashboard` | My certificates: download, verify, revoked tag. |
| `/verify`, `/verify/:code` | `Verify` | Public: enter code → valid / revoked (+ reason) / not found; shows name, course, type, score, instructor, issue date, code. API mode asks the server ("Checking the certificate…"; network error → "The certificate could not be checked. Please try again."). |

## 3. Entities & Fields
**CertificationRule** (one per course; id = courseId)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | number | yes | = Course.id | — | PK + FK Course (1-1) |
| enabled | bool | yes | — | true | |
| mode | enum | yes | `completion` / `exam` | completion | |
| minProgress | int % | yes | integer 1–100 | 100 | % of sections (demo) / of lessons (API, BR-LRN-15) |
| examType | enum | yes (exam) | `project` / `presentation` | project | |
| examTitle | string | yes (exam) | trimmed non-empty | `Final project` | |
| examInstructions | string | no | trimmed; API ≤ 8000 | '' | |
| maxScore | int | yes (exam) | integer ≥ 1 | 100 | |
| passingScore | int | yes (exam) | integer 1..maxScore | 60 | |
| maxAttempts | int | yes (exam) | integer ≥ 0; 0 = unlimited | 0 | |

**ExamSubmission**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `submission-…` | PK |
| userId | string | yes | FK User | — | |
| courseId | number | yes | FK Course | — | |
| examType | enum | yes | from rule at submit time | — | snapshot |
| url | string | yes | `^https:\/\/\S+\.\S+$` (https only), trimmed | — | Repo / slides / video link |
| notes | string | no | ≤ 1000 chars after trimming | '' | Trimmed |
| submittedAt | ISO datetime | yes | — | now | |
| status | enum | yes | `submitted` / `passed` / `failed` | submitted | |
| score | number | no | 0..maxScore, up to 2 decimals | — | Set on review |
| feedback | string | no | trimmed; UI max 1000 | — | |
| reviewedBy | string | no | FK User | — | |
| reviewedAt | ISO datetime | no | — | — | |

**Certificate**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `certificate-…` | PK |
| code | string | yes | unique; `CA-<YYYY>-<6 chars>` from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` | generated | Public verification code |
| userId | string | yes | FK User | — | |
| courseId | number | yes | FK Course | — | |
| userName | string | yes | snapshot | user name or "Student" | |
| courseTitle | string | yes | snapshot | — | |
| instructorName | string | yes | snapshot | instructor name or '' | |
| mode | enum | yes | completion / exam | — | |
| examType | enum | no | — | — | exam certificates |
| scorePercent | int | no | — | — | exam certificates |
| issuedAt | ISO datetime | yes | — | now | |
| revoked | bool | yes | — | false | |
| revokedReason | string | no | trimmed; UI max 200 | — | Shown publicly |

## 4. Business Rules
- **BR-CERT-01** A course without a saved rule uses the default rule (enabled, completion, 100%).
- **BR-CERT-02** Rule save requires `certificates.manage` on the course. minProgress integer 1–100 always; exam fields validated only when mode = exam (title required; maxScore ≥ 1; 1 ≤ passingScore ≤ maxScore; maxAttempts ≥ 0; all integers).
- **BR-CERT-03** A user has at most one **valid** (non-revoked) certificate per course; issuing returns the existing valid one.
- **BR-CERT-04** Completion mode: when an **active-enrolled** user's progress ≥ minProgress, a certificate is issued (automatically after marking a section, or via "Get my certificate"). API mode: progress is the lesson % (BR-LRN-15); the server issues it when a lesson completion (button or 90% watched) meets the rule, and the learning page then shows "Congratulations! Your certificate is ready."
- **BR-CERT-05** Disabled rule: no certificate box; nothing issued.
- **BR-CERT-06** Exam hand-in allowed only if: rule enabled, mode exam, progress ≥ minProgress, no valid certificate, no submission that is `submitted` or `passed`, and attempts left ≠ 0. Also requires `learn.view` and active enrollment.
- **BR-CERT-07** Attempts left = maxAttempts = 0 → unlimited; else max(0, maxAttempts − number of submissions).
- **BR-CERT-08** Hand-in error messages: progress not met → "Complete the course before the central exam."; no attempts → "You have used all your attempts."; otherwise "You cannot hand in the exam right now."; bad link → "Enter a link that starts with https://."; notes over 1000 characters after trimming → "This is too long. Please shorten it."
- **BR-CERT-09** Review requires `certificates.manage` on the course; only `submitted` submissions can be reviewed ("This submission has already been reviewed."; API: also when two reviewers save at once); score must be 0..maxScore with up to 2 decimals ("Enter a score between 0 and the maximum score.").
- **BR-CERT-10** Pass = score ≥ passingScore → status `passed` + certificate issued immediately (mode exam, the hand-in's examType, scorePercent). Otherwise `failed`.
- **BR-CERT-11** Revoke/restore requires `certificates.manage` on the course. Restore is refused if the student already has another valid certificate for that course.
- **BR-CERT-12** Certificate names/titles are snapshots; later edits don't change them.
- **BR-CERT-13** Verification is public; code lookup is trimmed and case-insensitive (upper-cased).
- **BR-CERT-14** Certificate codes are unique (regenerate on collision); year = issue year (UTC).
- **BR-CERT-15** PDF: A4 landscape; "Certificate of Completion" or "Certificate of Achievement" (exam, with exam type and score %); instructor name (or "Course Academy"); issue date `d MMMM yyyy` (en-GB); code; verify link `<origin>/verify/<code>`; file `certificate-<code>.pdf`. English/Latin fonts only.
- **BR-CERT-16** Students see "out of attempts" message only when attempts left = 0 and nothing is waiting for review.
- **BR-CERT-17** A revoked certificate blocks every new certificate for that user and course: progress, "Get my certificate", a passed review and a new hand-in issue nothing (hand-in shows "You cannot hand in the exam right now."). Only **Restore** makes it valid again.
- **BR-CERT-18** Rule save (API): mode and exam type must be valid codes ("Choose how the certificate is earned." / "Choose the type of central exam."); title ≤ 200 and instructions ≤ 8000 characters ("This is too long. Please shorten it."). In completion mode an exam field that is not valid keeps its stored value.
- **BR-CERT-19** Deleting a course keeps its rule, hand-ins and certificates; its certificates still verify (snapshots).

## 5. Workflows & Status Transitions
**Completion mode**: Student completes sections → progress ≥ min → Certificate issued.
**Exam mode**: Student reaches min progress → hands in link → `submitted` → Instructor/Admin reviews → `passed` (certificate issued) or `failed` → (if attempts left) student hands in again.

ExamSubmission: `submitted → passed` | `submitted → failed` (Instructor with grant / Admin).
Certificate: `valid → revoked` (Revoke, manager) → `valid` (Restore, manager; only if no other valid certificate). While revoked, nothing new is issued (BR-CERT-17).

## 6. Permissions
| Action | Guest | Student | Instructor (granted course) | Admin |
|---|---|---|---|---|
| Verify certificate | ✓ | ✓ | ✓ | ✓ |
| Claim / hand in exam | — | if enrolled | if enrolled | if enrolled |
| Download own certificate | — | ✓ | ✓ | ✓ |
| Configure rule | — | — | ✓ | ✓ |
| Review submissions | — | — | ✓ | ✓ |
| Revoke / restore / download any | — | — | ✓ | ✓ |

## 7. Calculations
- `scorePercent = round(score / maxScore × 100)` (halves round up).
- Progress % — see BR-LRN calculations (`percentFor`).
- Attempts left — BR-CERT-07.

## 8. Data Source
API mode (default) uses `/api/v1` (api-contract §11); demo mode keeps browser storage (`ca-certification-rules`, `ca-exam-submissions`, `ca-certificates`).
| Service method | API endpoint |
|---|---|
| `rule` / `loadRule(courseId)` | `GET /admin/courses/{id}/certification-rule` |
| `saveRule` | `PUT /admin/courses/{id}/certification-rule` |
| `status` / `loadStatus(course)` | `GET /me/courses/{id}/certification` |
| `evaluate(course)` | `POST /me/courses/{id}/certificate` (claim; the server also issues on progress) |
| `submitExam` | `POST /me/courses/{id}/exam-submissions` |
| `submissions` / `loadAdmin` | `GET /admin/exam-submissions?status=all&page=` |
| `review` | `POST /admin/exam-submissions/{id}/review` |
| `certificates` / `loadAdmin` | `GET /admin/certificates?search=&courseId=&page=` |
| `mine` / `loadMine` | `GET /me/certificates` |
| `setRevoked` | `POST /admin/certificates/{id}/revoke`, `POST …/restore` |
| `verify(code)` (demo: `findByCode`) | `GET /certificates/verify/{code}` (public) |
| `downloadCertificatePdf` | stays client-side (jsPDF) |

## 9. Open Questions / Assumptions
- ~~Revoking a completion certificate is undone automatically~~ — decided 2026-11-20: revocation blocks re-issue; only Restore (BR-CERT-17).
- `evaluate` requires an active enrollment; instructors/admins with only a grant can't earn certificates. Intended.
- Changing a rule from exam → completion leaves pending submissions; changing min progress doesn't affect issued certificates. OK?
- ~~Should the review score be an integer?~~ — decided 2026-11-20: decimals allowed (up to 2; `decimal(9,2)`).
- ~~Notes length is checked before trimming~~ — fixed 2026-11-20: checked after trimming.
- ~~What happens to certification data when a course is deleted?~~ — decided 2026-11-20: kept (BR-CERT-19).
- Can a reviewer change a result after review? Not supported.
- Should the student be notified (email) after review?
- Bangla names don't render in the PDF (Latin fonts). Need Bangla font support?
- Certificate code modulo bias (`byte % 32`) is fine (256 divisible by 32).

## 10. Source Files
- `src/app/core/services/certificate.service.ts`, `certificate-pdf.ts`
- `src/app/core/models/certificate.model.ts`, `src/app/core/api/certification.models.ts`
- Backend: `Controllers/CertificationControllers.cs`, `Services/CertificationService.cs`, `Repositories/CertificationRepositories.cs`, `Entities/Certification.cs`, `Data/Configurations/CertificationConfigurations.cs`, `Data/Seed/DemoCertificationCommunitySeeder.cs`
- `src/app/pages/admin/certification-settings.ts`, `admin-certificates.ts`
- `src/app/pages/learn/course-certificate/course-certificate.ts`
- `src/app/pages/verify/verify.ts`
