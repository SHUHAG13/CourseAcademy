# Course Management (Admin: Courses, Curriculum, Instructors)

## 1. Purpose
Lets admins and granted instructors create, edit, publish and delete courses, build the curriculum (modules → submodules → sections → topics, with videos and documents), schedule live sessions, attach materials, and manage instructor profiles.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/admin/courses` | `AdminCourses` | Table of courses the user can view (search by title, filter by format): title, instructor, format, price + discount, sections/live/materials counts, active enrollments, Published/Draft. Actions: publish/unpublish, view page, certification, curriculum, edit, delete. "New course" button. |
| `/admin/courses/new` | `CourseForm` | Create course. Guard `courses.create`. |
| `/admin/courses/:id` | `CourseForm` | Edit course details, live sessions, materials. Guard `courses.edit` (global or on course). API mode: file materials have "Upload file / Replace file / Remove file" with progress and processing status (saved courses only: "Save the course first to upload files."). |
| `/admin/courses/:id/curriculum` | `CurriculumEditor` | Nested editor: add/remove/reorder modules, submodules, sections, topics, videos, documents; collapse modules. Guard `courses.edit` on course. API mode: each video has "Upload file / Replace file" (an older video shows its link read-only); each document has a link field or an uploaded file; upload progress %, then "Processing… / Ready / Processing failed". |
| `/admin/instructors` | `AdminInstructors` | Instructor cards (name, title, bio, years, assigned course count). Inline create/edit form, delete. |

## 3. Entities & Fields
**Course**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | number | yes | — | `Date.now()` on create | PK. Time-based so a deleted id is never reused |
| slug | string | yes | `^[a-z0-9]+(?:-[a-z0-9]+)*$`; unique | auto from title on blur if empty | "Another course already uses this slug." |
| title | string | yes | 5–120 chars, trimmed | — | |
| subtitle | string | yes | ≤ 200 chars | — | |
| description | string | yes | non-empty | — | |
| category | CategoryId | yes | one of 4 | `video-editing` | FK Category |
| format | CourseFormat | yes | Pre-recorded / Live / Hybrid | Pre-recorded | |
| instructorId | string | yes | FK Instructor | first instructor | |
| level | Level | yes | Beginner / Intermediate / Advanced | Beginner | |
| language | string | yes | free text | `Bangla` | |
| durationHours | number | yes | ≥ 1 | 1 | |
| originalPrice | number (BDT) | yes | ≥ 0 | 0 | Regular price |
| price | number (BDT) | yes | ≥ 0; ≤ originalPrice | 0 | Sale price actually charged. 0 = free |
| coverLabel | string | yes | ≤ 30 chars | — | Text on generated cover |
| coverIcon | string | yes | free text (icon class) | `bi-journal-text` | |
| image | string | no | `^(?:\/[^\s/]\|https?:\/\/)\S*$` | — | Optional photo; preset site images offered |
| badge | enum | no | Bestseller / New / Popular | none | |
| featured | bool | no | — | false | Shown on home |
| published | bool | yes | see BR-CMS-04/05 | false | |
| rating | number | yes | not editable | 0 (new) | Demo stat, kept on edit |
| ratingCount | number | yes | not editable | 0 | Demo stat |
| students | number | yes | not editable | 0 | Demo stat |
| updated | string `YYYY-MM` | yes | — | set on every save | |
| outcomes | string[] | yes | ≥ 1 line | — | Entered one per line |
| requirements | string[] | no | — | [] | One per line |
| modules | CourseModule[] | yes | see curriculum rules | [] | Edited on curriculum page only |
| schedule | LiveSession[] | yes | see below | [] | Ordered |
| materials | Material[] | yes | see below | [] | Ordered |

**CourseModule**: id (`module-…`), title (required, trimmed), submodules[] (ordered).
**Submodule**: id (`submodule-…`), title (required), sections[] (2–3).
**Section** (also used for Topics, self-nested)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `section-…` | |
| title | string | yes | trimmed, non-empty | — | "Every section/topic needs a title." |
| videos | SectionVideo[] | no | — | one empty video on create | Ordered |
| documents | SectionDocument[] | no | — | [] | |
| topics | Section[] | no | max nesting depth 3 | [] | Each topic counts as a section for progress |

**SectionVideo**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `video-…` | |
| title | string | yes | trimmed | — | |
| url | string | demo: yes; API: older videos only | `^https?:\/\/[^\s.]+\.[^\s]{2,}$` | — | See BR-CAT-20 for playable formats. API: read-only, cannot be changed (BR-CMS-24) |
| mediaId | number | API: new videos | uploaded video of this course (BR-CMS-23) | — | Replaces the link when set |
| minutes | number | yes | ≥ 1 | 10 | |
| preview | bool | no | — | false | Free preview for non-enrolled |

**SectionDocument**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `document-…` | |
| title | string | yes | — | — | |
| type | DocumentType | yes | PDF / DOCX / PPTX | PDF | |
| url | string | no | URL pattern; path must end in `.pdf`/`.docx`/`.pptx` matching type (ignoring `?query`/`#hash`) | — | Empty = no file yet (BR-CMS-19) |
| mediaId | number | no | API: uploaded document of this course whose file type matches `type` | — | Either `url` or `mediaId` |
| videoId | string | no | must reference a video in the same section | first video of section | Empty = document for whole section |

**LiveSession**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `session-…` | |
| title | string | yes | — | — | |
| startsAt | string `YYYY-MM-DDTHH:mm` | yes | — | — | Local time, no timezone |
| durationMinutes | number | yes | ≥ 1 | 60 | |
| meetingUrl | string | yes | URL pattern | `https://meet.example.com/your-class` | Placeholder |

**Material**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `material-…` | |
| title | string | yes | — | — | |
| type | MaterialType | yes | PDF / Slides / Worksheet / Link | PDF | |
| url | string | Link: yes | URL pattern | demo: `https://example.com/demo-materials/file`; API: empty | File materials may have no file yet |
| mediaId | number | no | API: uploaded document (PDF/DOCX/PPTX) of this course; not for Link | — | Either `url` or `mediaId` |

**MediaFile** (API mode, uploaded file; stored as `MediaAsset`, see data-model.md)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | number | yes | — | — | Main App id; the Streaming id is never sent to the browser |
| kind | video / document | yes | — | — | |
| status | pending_upload / uploaded / queued / processing / ready / failed / deleting / deleted | yes | — | pending_upload | Only `ready` files are shown to students |
| fileName, mimeType, sizeBytes | string, string, number | yes | BR-CMS-21 | — | |
| durationSeconds | number | no | — | — | Video length, from Streaming |
| errorMessage | string | no | — | — | When processing failed |

**Instructor**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `instructor-…` (seed uses name slug) | PK |
| name | string | yes | min 2, trimmed | — | |
| title | string | yes | trimmed | — | Professional title |
| bio | string | yes | trimmed | — | |
| expertise | string[] | no | comma-separated input, trimmed, empties removed | [] | |
| experienceYears | number | yes | ≥ 0 | 0 | |
| students | number | yes | ≥ 0 | 0 | Manually entered |
| rating | number | yes | not editable | 0 (new) | |

## 4. Business Rules
- **BR-CMS-01** Create requires `courses.create`; edit requires `courses.edit` globally or on that course; delete requires `courses.delete` (global or course).
- **BR-CMS-02** Slug must be unique among all courses (excluding itself). Auto-filled from title when empty: lower-case, `&` → "and", non-alphanumerics → `-`, trimmed hyphens.
- **BR-CMS-03** Sale `price` must not exceed `originalPrice`: "The sale price can't be above the regular price."
- **BR-CMS-04** A published course must have ≥ 1 section in its saved curriculum. (Drafts may be incomplete.)
- **BR-CMS-05** A published Live or Hybrid course must have ≥ 1 live session.
- **BR-CMS-06** Curriculum save (server-side check in service) fails with the first broken rule: every module/submodule/section/topic has a title; each submodule has 2–3 sections; topics nest ≤ 3 levels; document URL extension matches type; document `videoId` refers to a video in the same section.
- **BR-CMS-07** In the editor, a submodule's section delete button is disabled at 2 sections; "add section" is disabled at 3. New submodules start with 2 empty sections; new sections start with 1 empty video.
- **BR-CMS-08** Curriculum of a published course cannot be emptied: "A published course needs at least one section. Unpublish it first to remove them all."
- **BR-CMS-09** Removing a video detaches its documents (they become whole-section documents), not deletes them.
- **BR-CMS-10** New documents attach to the section's first video by default.
- **BR-CMS-11** Every save sets `updated` = current `YYYY-MM`. `rating`, `ratingCount`, `students` are preserved (0 for new).
- **BR-CMS-12** Publish/unpublish toggle from the list. (API) The server checks BR-CMS-04/05 on every save and on the toggle, so a course without sections, or a Live/Hybrid course without sessions, cannot be published from the list either.
- **BR-CMS-13** Delete course (after confirm) removes: all enrollments for it, all community posts/comments, all course grants, then the course. Payment transactions are kept (they hold a snapshot). (API, Step 3) The course is soft-deleted and its grants removed; enrollments and communities are removed by the API once they move there (Steps 4+); until then the browser removes them.
- **BR-CMS-14** Instructor delete is blocked while any course (published or not) has that `instructorId`: "{name} teaches {n} course(s). Reassign them before deleting."
- **BR-CMS-15** Instructor create/edit/delete require `instructors.create` / `.edit` / `.delete`.
- **BR-CMS-16** Admin course list shows only courses where the user has `courses.view` (global or grant).
- **BR-CMS-17** Text fields are trimmed on save; outcomes/requirements split by line, blank lines removed.
- **BR-CMS-18** Rows (sessions, materials, modules, submodules, sections, topics, videos, documents) can be reordered up/down; array order = display order.
- **BR-CMS-19** Documents and file materials may be saved without a file ("no file yet"); a Link material always needs a link. Seeded demo documents and file materials have no file. Demo mode: a new video needs a link. (API: see BR-CMS-24; changed 2026-11-16, before uploads a new video needed a link.)
- **BR-CMS-20** (API) Curriculum save keeps the ids of items it is sent and removes (soft-deletes) items it is not sent; ids that do not belong to the course are refused ("Something in this course changed in the meantime. Reload the page and try again.").
- **BR-CMS-21** (API) Files are uploaded for a saved course by people with `courses.edit` on it. Limits: video ≤ 2 GB (MP4, WebM, MOV); document ≤ 50 MB (PDF, DOCX, PPTX). The API checks the user and the file first, then asks the Streaming service for an upload link (valid 15 minutes); the browser sends the bytes straight to that link, then tells the API the upload is complete.
- **BR-CMS-22** (API) After an upload, Streaming processes the file: `uploaded → queued → processing → ready`, or `failed`. The editor checks the status every 3 seconds. A video, document or material whose file is not `ready` is hidden from students (media rule B); managers see it with its status.
- **BR-CMS-23** (API) A file can only be attached in the course it was uploaded for, to one item, and with the right kind: a video file for a video, a document for a document or file material; a section document's file must match its type (.pdf/.docx/.pptx). Files that failed, are not uploaded yet, or are being deleted are refused.
- **BR-CMS-24** (API) A new video must be an uploaded file ("Upload a file for every new video."; a link is refused: "New videos can't be added as links."). Older videos (such as the seeded demo videos) keep their link, which can't be changed ("A video link can no longer be changed. Upload the video file instead."); uploading a file replaces it.
- **BR-CMS-25** (API) A file that no item uses any more (its video, document or material was removed, or given another file, or the course was deleted) is deleted from the Streaming service straight away; a failed request is retried every minute. An upload that is replaced or removed in the editor before saving is deleted at once. A file still in use cannot be deleted ("This file is still used in the course.").
- **BR-CMS-26** (API) A document or file material holds an uploaded file, a link, or nothing yet; a Link material cannot hold a file. Save waits until running uploads have finished ("Wait until the uploads have finished, then save.").
- **BR-CMS-27** (API) After a curriculum save the editor reloads the saved curriculum, so new rows get their server ids and a second save updates them instead of adding them again. (Fixed 2026-11-16: before, a second save without reloading the page removed and re-created the new rows.)

## 5. Workflows & Status Transitions
Course publish state: `Draft (published=false) → Published (published=true) → Draft` — triggered by Admin or instructor with `courses.edit` on the course.
Typical flow: Create course as Draft (details) → Save → open Curriculum → add modules/sections → upload videos/documents (API) → Save → (Live/Hybrid) add sessions → tick Published → Save.

Uploaded file (API): `pending_upload` (browser sending) → `uploaded` (complete) → `queued` → `processing` → `ready` | `failed` — reported by Streaming webhooks. `ready | failed → deleting` (no longer used, BR-CMS-25) `→ deleted` (Streaming confirms).

## 6. Permissions
| Action | Student | Instructor (granted course) | Admin |
|---|---|---|---|
| View admin course list | — | own granted courses | all |
| Create course | — | — (unless `courses.create`) | ✓ |
| Edit details / curriculum / publish | — | ✓ | ✓ |
| Delete course | — | — (grant can't include delete) | ✓ |
| View instructors | — | — | ✓ |
| Create/edit/delete instructor | — | — | ✓ |

## 7. Calculations
- Discount preview in form = `originalPrice > 0 && price ≤ originalPrice ? round((1 − price/originalPrice) × 100) : 0`.
- Section count = every section + every nested topic.
- Module minutes = Σ video minutes in module (all depths).
- Admin list "Enrolled" = count of enrollments with status `active`.
- Instructor card "assigned courses" = all courses (incl. drafts) with that instructorId.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| `CourseService.courses` (`ca-courses`) | `GET /api/admin/courses?search=&format=` |
| `CourseService.saveCourse` (create) | `POST /api/admin/courses` |
| `CourseService.saveCourse` (edit / publish toggle) | `PUT /api/admin/courses/{id}`, `PATCH /api/admin/courses/{id}/publish` |
| Curriculum save | `PUT /api/admin/courses/{id}/curriculum` |
| `isSlugTaken` | `GET /api/admin/courses/slug-available?slug=&exceptId=` |
| `removeCourse` (+ cascades) | `DELETE /api/admin/courses/{id}` |
| `CourseService.instructors` (`ca-instructors`) | `GET /api/instructors` |
| `saveInstructor` | `POST /api/instructors`, `PUT /api/instructors/{id}` |
| `removeInstructor` | `DELETE /api/instructors/{id}` |
| `MediaService.upload` (API, Step 6) | `POST /api/v1/media/uploads` → PUT bytes to the returned link → `POST /api/v1/media/uploads/{id}/complete` |
| `MediaService.status` (editor polling) | `GET /api/v1/media/assets/{id}` |
| `MediaService.remove` (unsaved upload replaced/removed) | `DELETE /api/v1/media/assets/{id}` |

## 9. Open Questions / Assumptions
- Deleting a course does **not** remove progress, certification rule, exam submissions or certificates for it. Keep (as history) or delete?
- Should course delete be a soft delete (archive) given payments reference it?
- Publish toggle in list skips "needs sections / needs sessions" checks; a course with no sessions can be published from the list. Intended?
- `language` is free text; should it be an enum (Bangla/English)?
- `coverIcon` is free text; no validation it is a real icon.
- Answered 2026-11-16: files are uploaded to the separate Streaming service; new videos are upload-only; removed or replaced files are deleted at once; the spec's limits (2 GB / 50 MB, 15-minute upload link, 10-minute play/download link, 5-minute webhook window) apply.
- An upload that is never saved into the curriculum (e.g. the editor is closed before saving) stays at Streaming as an unused file. Should unused uploads be deleted automatically after some time (e.g. 24 hours)?
- A video up to 2 GB is sent in one request; a dropped connection means starting again. Are resumable (chunked) uploads needed?
- Documents and file materials can still be links. Should they become upload-only like videos?
- Sections may end up with zero videos (removable). Allowed?
- `durationHours` is entered manually, not computed from video minutes. OK?
- Instructor `students`/`rating` are manual/static. Should they be computed?
- Nothing links Instructor profile ↔ user account except demo seed (`User.instructorId`).

## 10. Source Files
- `src/app/core/services/course.service.ts` (`curriculumError`, `saveCourse`)
- `src/app/core/models/course.model.ts`
- `src/app/pages/admin/admin-courses.ts`, `admin-instructors.ts`
- `src/app/pages/admin/course-form/course-form.ts` / `.html`
- `src/app/pages/admin/curriculum-editor/curriculum-editor.ts` / `.html`
- `src/app/core/services/media.service.ts` (uploads, status, links)
- API: `Services/CourseAdminService.cs` (attach/release files), `Services/Media/*` (MediaService, StreamingWebhookService, clients, deletion worker), `Controllers/MediaControllers.cs`
