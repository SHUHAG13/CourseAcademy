# Learning Area, Progress & Student Dashboard

## 1. Purpose
Gives enrolled users a place to watch lessons, open documents and materials, join live classes, track completion per section, and see their courses, certificates, upcoming classes and payments on one dashboard.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/dashboard` | `Dashboard` | Guards: auth + `dashboard.view`. Sections: My courses (progress bar, revoked tag, Continue learning), My certificates (download PDF, verify), Upcoming live classes (max 5), Payment history (View receipt). |
| `/learn/:slug` | `Learn` | Guards: auth + `learn.view`. Without access: "enrolled students only" message. With access: progress (x of y sections), outline sidebar (sections + nested topics, ✓ when complete), player, breadcrumb trail, video switcher, documents per video and per section, topic shortcuts, "Mark as complete" toggle, certificate box, live classes with links, materials, link to community. |

## 3. Entities & Fields
**Progress**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | `userId:courseId` | — | PK; one per user+course |
| userId | string | yes | FK User | — | |
| courseId | number | yes | FK Course | — | |
| completedSectionIds | string[] | yes | each must be a section/topic of the course | [] | |
| updatedAt | ISO datetime | yes | — | now | |

## 4. Business Rules
- **BR-LRN-01** The learning page content requires course access (BR-ENR-03). An unpublished course is still learnable by enrolled users.
- **BR-LRN-02** Marking a section complete requires: signed in, `learn.view`, and course access; the section id must exist in the course ("Section not found.").
- **BR-LRN-03** Completion is a toggle (complete / undo). After marking complete, the next section in reading order is selected automatically.
- **BR-LRN-04** Reading order = for each module → submodule → section, the section then its topics depth-first. Each topic counts as one section.
- **BR-LRN-05** Only completed ids that still exist in the course count (removed sections don't count).
- **BR-LRN-06** Reaching 100% shows "You have completed every section of {course}!".
- **BR-LRN-07** After each completion, a completion-mode certificate is evaluated and issued if the rule is met (BR-CERT-04).
- **BR-LRN-08** For **Live** format courses the player and "Mark as complete" are not shown; the outline is static (see Open Questions).
- **BR-LRN-09** Documents attached to a video show under that video; documents without `videoId` show for the whole section.
- **BR-LRN-10** Live sessions are "past" when `startsAt + durationMinutes` < now.
- **BR-LRN-11** Dashboard "Upcoming live classes" = sessions of courses with **active** enrollments that have not ended, sorted by start, max 5.
- **BR-LRN-12** Dashboard hides enrollments whose course was deleted.
- **BR-LRN-13** If an admin edits the curriculum while a student is on the page, the current section/video is kept if it still exists, otherwise the first is selected.
- **BR-LRN-14** Dashboard payment history = own transactions, newest first.

## 5. Workflows & Status Transitions
Learn: Dashboard → Continue learning → pick section → watch → Mark as complete → auto-advance → … → 100% → certificate (completion mode) or exam hand-in (exam mode).
Section state per user: `Not completed ⇄ Completed` (student toggles).

## 6. Permissions
| Action | Student | Instructor | Admin |
|---|---|---|---|
| Open dashboard (`dashboard.view`) | ✓ | ✓ | ✓ |
| Open learning area (`learn.view` + access) | if enrolled | if enrolled or grant `courses.view` | ✓ |
| Mark complete | if enrolled | if access | ✓ |

## 7. Calculations
- `percent = total > 0 ? round(completedStillInCourse / totalSections × 100) : 0` (totalSections includes topics).
- Session end = `startsAt + durationMinutes × 60 000 ms`.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| `ProgressService` (`ca-progress`) | `GET /api/me/courses/{courseId}/progress` |
| `setComplete(course, sectionId, bool)` | `PUT /api/me/courses/{courseId}/progress/sections/{sectionId}` `{completed}` |
| `percentFor(userId, course)` | server-side (certificate checks) |
| Dashboard aggregate | `GET /api/me/dashboard` (or the individual `/api/me/*` endpoints) |
| `CourseService.bySlug` (learn) | `GET /api/learn/{slug}` (full curriculum incl. non-preview URLs, access-checked) |

## 9. Open Questions / Assumptions
- **Live courses cannot record progress** (no "Mark as complete" UI). Demo course 2 (Live) has an exam rule needing 100% progress, so its certificate is unreachable. How should Live-course progress work (attendance? instructor marks?)
- Is completion manual only, or should watching a video auto-complete?
- Should there be per-video progress / resume position?
- Live meeting links are placeholders. Which provider (Zoom/Meet)? Attendance tracking?
- `startsAt` has no timezone. Assume Asia/Dhaka?

## 10. Source Files
- `src/app/core/services/progress.service.ts`
- `src/app/core/models/progress.model.ts`
- `src/app/pages/learn/learn.ts` / `.html`
- `src/app/pages/dashboard/dashboard.ts` / `.html`
- `src/app/shared/components/session-list/session-list.ts`, `document-list/document-list.ts`
