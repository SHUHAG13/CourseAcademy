# Learning Area, Progress & Student Dashboard

## 1. Purpose
Gives enrolled users a place to watch lessons, open documents and materials, join live classes, track completion per section, and see their courses, certificates, upcoming classes and payments on one dashboard.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/dashboard` | `Dashboard` | Guards: auth + `dashboard.view`. Sections: My courses (progress bar, revoked tag, Continue learning), My certificates (download PDF, verify), Upcoming live classes (max 5), Payment history (View receipt). |
| `/learn/:slug` | `Learn` | Guards: auth + `learn.view`. Without access: "enrolled students only" message. With access: progress (x of y sections; API mode: x of y lessons), outline sidebar (sections + nested topics, ✓ when complete), player, breadcrumb trail, video switcher (API mode: ✓ per finished video), documents per video and per section, topic shortcuts, "Mark as complete" toggle (API mode: for the current video, or for the section when it has no videos), certificate box, live classes with links, materials, link to community. Same for every course format, Live included (BR-LRN-08). |

## 3. Entities & Fields
**LessonProgress** (API mode, backend Step 5) — one per user and lesson; see data-model.md
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| userId, courseId, sectionId | int | yes | FK | — | |
| sectionVideoId | int | no | FK SectionVideo | null | null = a section without videos |
| positionSeconds | int | yes | 0–86400 | 0 | resume point (video files only) |
| maxWatchedSeconds | int | yes | ≥ 0 | 0 | furthest point reached |
| durationSeconds | int | no | 1–86400 | — | reported by the player |
| isCompleted / completedAt | bool / datetime | yes / no | — | false / null | |

**Progress** (demo mode, browser storage)
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
- **BR-LRN-08** **Live** format courses work like the others: player, "Mark as complete" and a clickable outline, so their progress can move. (Changed 2026-11-16; before, Live courses showed a static outline only.)
- **BR-LRN-09** Documents attached to a video show under that video; documents without `videoId` show for the whole section.
- **BR-LRN-10** Live sessions are "past" when `startsAt + durationMinutes` < now.
- **BR-LRN-11** Dashboard "Upcoming live classes" = sessions of courses with **active** enrollments that have not ended, sorted by start, max 5.
- **BR-LRN-12** Dashboard hides enrollments whose course was deleted.
- **BR-LRN-13** If an admin edits the curriculum while a student is on the page, the current section/video is kept if it still exists, otherwise the first is selected.
- **BR-LRN-14** Dashboard payment history = own transactions, newest first.
- **BR-LRN-15** (API) Progress counts **lessons**: each video students can see is one lesson (a video with no file yet, or an upload that is not ready, does not count), and a section with no such videos (only documents, or empty) is one lesson. Topics are sections of their own. A section is ✓ when all its lessons are done. Demo mode keeps counting sections (BR-LRN-04).
- **BR-LRN-16** (API) Progress is stored on the server per user and lesson, so it is the same on every device. The dashboard shows it for every enrolled course. A revoked learner keeps their progress but cannot change it ("Enroll in this course to track your progress."); people who manage the course (`courses.view`) can record progress without an enrollment.
- **BR-LRN-17** (API) Only video files (direct https `.mp4`/`.webm` links, and uploaded videos) can report where the learner is: every 15 seconds while playing, on pause and at the end (a pause and an end at the same moment send one report). Opening the video again resumes there, unless it was within the last 5 seconds. YouTube/Vimeo embeds cannot report, so they are completed with the button only. For uploaded videos the length measured by Streaming counts, not the length the browser reports.
- **BR-LRN-18** (API) A video file completes itself the first time the furthest point watched reaches 90% of its length; the button stays available. If the learner later marks it as not done, watching it again does not complete it automatically.
- **BR-LRN-19** (API) After marking a video complete, the next video of the section is selected; after the last one (or a section without videos), the next section (extends BR-LRN-03).

## 5. Workflows & Status Transitions
Learn: Dashboard → Continue learning → pick section → watch → Mark as complete → auto-advance → … → 100% → certificate (completion mode) or exam hand-in (exam mode).
Section state per user (demo): `Not completed ⇄ Completed` (student toggles).
Lesson state per user (API): `Not completed ⇄ Completed` (student toggles); a video file also moves to `Completed` by itself at 90% watched (once).

## 6. Permissions
| Action | Student | Instructor | Admin |
|---|---|---|---|
| Open dashboard (`dashboard.view`) | ✓ | ✓ | ✓ |
| Open learning area (`learn.view` + access) | if enrolled | if enrolled or grant `courses.view` | ✓ |
| Mark complete | if enrolled | if access | ✓ |

## 7. Calculations
- Demo: `percent = total > 0 ? round(completedStillInCourse / totalSections × 100) : 0` (totalSections includes topics).
- API: `totalLessons = Σ over sections and topics of max(1, videoCount)`; `percent = totalLessons > 0 ? round(completedLessons / totalLessons × 100) : 0` (half rounds up). Only lessons still in the course count.
- Automatic completion: `maxWatchedSeconds ≥ ceil(durationSeconds × 0.9)`; a reported position past the end is stored as the end.
- Session end = `startsAt + durationMinutes × 60 000 ms`.

## 8. Data Source
**Done in API mode (backend Step 5).** Demo mode keeps browser storage (`ca-progress`, section-based).
| Current | Endpoint |
|---|---|
| `ProgressService.loadMine` (dashboard %) | `GET /api/v1/progress/mine` |
| `ProgressService.loadCourse` (learning page) | `GET /api/v1/progress/courses/{slug}` |
| `setVideoComplete(course, videoId, bool)` | `PUT /api/v1/progress/videos/{id}/complete` `{completed}` |
| `setComplete(course, sectionId, bool)` (section without videos) | `PUT /api/v1/progress/sections/{id}/complete` `{completed}` |
| `savePosition(...)` (player) | `PUT /api/v1/progress/videos/{id}/position` `{positionSeconds, durationSeconds}` |
| `percentFor(userId, course)` | demo only (certificate checks); certificates move to the API later |
| `CourseService.bySlug` (learn) | `GET /api/v1/courses/{slug}` (lesson links included with course access) |

## 9. Open Questions / Assumptions
- Answered 2026-11-16: Live courses show their lessons like other courses; progress is per video with resume and 90% auto-complete for video files; sections without videos count as one lesson.
- Seeking to the end of a video file counts as watched (no check that every part was played). Is that acceptable?
- Direct .mp4/.webm links still use the length the browser reports; uploads use Streaming's measured length (when Streaming reports one; the development fake does not).
- When certificates move to the API, should the completion rule's minimum progress use the lesson percentage? (Not decided; not built yet.)
- Demo mode still counts sections, so the same course can show a different % in demo and API mode.
- Live meeting links are placeholders. Which provider (Zoom/Meet)? Attendance tracking?
- `startsAt` has no timezone. Assume Asia/Dhaka?

## 10. Source Files
- `src/app/core/services/progress.service.ts`, `src/app/core/api/learning.models.ts`
- `src/app/core/models/progress.model.ts`
- `src/app/shared/components/video-embed/video-embed.ts` (resume + position reports)
- API: `Services/ProgressService.cs`, `Controllers/LearningControllers.cs`, `Entities/Learning.cs`, `Repositories/LearningRepositories.cs`
- `src/app/pages/learn/learn.ts` / `.html`
- `src/app/pages/dashboard/dashboard.ts` / `.html`
- `src/app/shared/components/session-list/session-list.ts`, `document-list/document-list.ts`
