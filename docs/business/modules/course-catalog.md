# Course Catalog (Public Browsing)

## 1. Purpose
Lets visitors discover, filter and evaluate courses before buying: catalogue list, category filters, course detail page with curriculum, free previews, instructor, reviews and pricing.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/` | `Home` (+ Hero, Categories, Courses, Features, LearningJourney, SportsTeaser, CareerTeaser, Testimonials, Faq, FinalCta) | Featured courses (max 4), category tiles with course counts, marketing sections. |
| `/courses` | `CourseList` | Search, category, price range, level, format filters; sort; "Load more" (6 per page); skeleton loading. Query params `?category=`, `?q=`. |
| `/courses/:slug` | `CourseDetail` | Title, subtitle, rating, students, price + discount, format intro, outcomes, curriculum (expand/collapse modules), free preview videos inline, live schedule (links hidden until enrolled), materials (locked icons), requirements, instructor card, reviews + rating breakdown, related courses, community teaser/board, CTA (Enroll Now → checkout, or Go to course), share link, WhatsApp enquiry. |

## 3. Entities & Fields
**Category** (static, not editable)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | enum CategoryId | yes | `video-editing`, `digital-marketing`, `sports-journalism`, `software-development` | — | PK |
| name | string | yes | — | — | |
| shortName | string | yes | — | — | |
| description | string | yes | — | — | |
| icon | string | yes | Bootstrap icon class | — | |
| image | string | yes | site path | — | |
| skills | string[] | yes | — | — | |

**Review** (static demo pool)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| name | string | yes | — | — | reviewer name |
| rating | number | yes | 1–5 (inferred) | — | |
| date | string | yes | — | — | |
| text | string | yes | — | — | |

**CourseFilter** (UI state, not stored)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| search | string | no | trimmed, case-insensitive | '' | from `?q=` |
| category | CategoryId \| all | no | must be a known category else `all` | all | from `?category=` |
| price | PriceRange | no | `all`, `under-2500`, `2500-4500`, `over-4500` | all | |
| levels | Level[] | no | Beginner/Intermediate/Advanced | [] | multi-select |
| formats | CourseFormat[] | no | Pre-recorded/Live/Hybrid | [] | multi-select |
| sort | CourseSort | no | popular, rating, newest, price-asc, price-desc | popular | |

Course, Instructor, curriculum entities: see `course-management.md`.

## 4. Business Rules
- **BR-CAT-01** Only courses with `published = true` appear in the catalogue, home, related lists, category counts, contact topic list and checkout.
- **BR-CAT-02** An unpublished course detail page is visible only to users with `courses.view` on that course; others see "not found".
- **BR-CAT-03** Search matches title, subtitle or coverLabel (contains, case-insensitive), in English or the current translated text.
- **BR-CAT-04** Price ranges: under-2500 = price < 2500; 2500-4500 = 2500 ≤ price ≤ 4500; over-4500 = price > 4500 (BDT, sale price).
- **BR-CAT-05** Level/format filters: empty selection = all; otherwise course must match one of the selected values.
- **BR-CAT-06** Sort: popular = students desc; rating = rating desc then ratingCount desc; newest = `updated` desc (string compare YYYY-MM); price-asc / price-desc on sale price.
- **BR-CAT-07** Page size 6; "Load more" adds 6; any filter change resets to 6.
- **BR-CAT-08** Active filter count = (price ≠ all) + #levels + #formats + (category ≠ all). Search and sort are not counted.
- **BR-CAT-09** Level/format option counts are computed within the active category.
- **BR-CAT-10** Featured (home) = published courses with `featured = true`, first 4.
- **BR-CAT-11** Related courses = other published courses, same category first, then others, max 4.
- **BR-CAT-12** Reviews shown = 3 entries from the demo pool chosen by `course.id mod poolSize` (deterministic, demo only).
- **BR-CAT-13** Free preview: videos with `preview = true` can be played by anyone; their attached documents are unlocked for everyone.
- **BR-CAT-14** Non-preview documents and all materials show as locked unless the user has course access (BR-ENR-03).
- **BR-CAT-15** Live session meeting links are shown only to users with course access.
- **BR-CAT-16** CTA: user with access → "Go to course" (`/learn/:slug`); otherwise "Enroll Now" (`/checkout/:slug`).
- **BR-CAT-17** "Play preview" opens the first module containing a preview video; if none: "This course has no free preview videos yet."
- **BR-CAT-18** Instructor card course count = number of **published** courses by that instructor.
- **BR-CAT-19** Format intro text: Pre-recorded = self-paced recorded; Live = instructor-led live classes; Hybrid = recorded + scheduled live.
- **BR-CAT-20** Video URLs: YouTube (`youtu.be`, `youtube.com/watch|embed|shorts`), Vimeo (`vimeo.com/<id>`) or direct `https://…mp4|webm`; anything else shows a placeholder player.

## 5. Workflows & Status Transitions
Browse: Guest → `/courses` → filter/sort → open course → preview → Enroll Now → (login if needed) → checkout (see checkout-payments).
No status transitions in this module.

## 6. Permissions
| Action | Guest | Student | Instructor | Admin |
|---|---|---|---|---|
| View published catalogue/detail | ✓ | ✓ | ✓ | ✓ |
| View unpublished course detail | — | — | if grant `courses.view` | ✓ |
| Watch preview videos | ✓ | ✓ | ✓ | ✓ |
| Open locked docs/materials/meeting links | — | if enrolled | if enrolled or grant | ✓ |

## 7. Calculations
- `discountPercent = originalPrice > 0 ? round((1 − price / originalPrice) × 100) : 0`
- Section count = all sections incl. nested topics.
- Module minutes = Σ video minutes of all sections/topics in the module.
- Rating breakdown (demo): five = max(0, round((rating − 4) × 85)); four = max(0, 92 − five); then 5★=five, 4★=four, 3★=5, 2★=2, 1★=1 (each capped at 100). Not real data.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| `CourseService.published` + `filterCourses()` (client) | `GET /api/courses?search=&category=&price=&levels=&formats=&sort=&page=&pageSize=` |
| `CourseService.bySlug()` | `GET /api/courses/{slug}` |
| `CourseService.featured()` | `GET /api/courses?featured=true&limit=4` |
| `CourseService.related()` | `GET /api/courses/{id}/related?limit=4` |
| `CourseService.categories` (static `CATEGORIES`) | `GET /api/categories` (with published course counts) |
| `CourseService.reviews()` / `ratingBreakdown()` (static demo) | `GET /api/courses/{id}/reviews` (planned) |
| `CourseService.instructor()` | included in course detail response |
| Simulated latency 350 ms | — |

## 9. Open Questions / Assumptions
- Reviews and rating breakdown are fake. Should students be able to submit reviews? Who can review (enrolled only)? Then `rating`, `ratingCount` become calculated.
- `students`, `rating`, `ratingCount` on Course are static demo numbers, not calculated from enrollments. Should they be calculated?
- Should search also match description/instructor name? Currently not.
- Categories are hard-coded (4). Should admins manage categories?
- "Newest" sorts by `updated` (YYYY-MM string), not creation date.
- Course detail search in Bangla only works for text in the translation dictionary; admin-typed courses aren't translated. Is multilingual course content needed (title_bn, title_en)?

## 10. Source Files
- `src/app/core/services/course.service.ts` (`filterCourses`, `discountPercent`)
- `src/app/pages/course-list/course-list.ts`
- `src/app/pages/course-detail/course-detail.ts` / `.html`
- `src/app/pages/home/home.html`, `src/app/components/*`
- `src/app/core/data/courses.data.ts`
- `src/app/shared/components/video-embed/video-embed.ts`
