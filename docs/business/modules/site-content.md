# Site Content, Contact, Career & Language

## 1. Purpose
Public marketing and information pages (about, sports media, career opportunities, testimonials, FAQ, legal), the contact and career-application forms, and the Bangla/English language switch.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/about` | `About` | Static about page. |
| `/sports` | `Sports` | Sports stories filtered by topic (All, Cricket, Football, Analysis, Interview, Journalism); clicking a story shows "preview only" toast; top 3 sports-journalism courses by students. |
| `/career` | `Career` | Opportunities filtered by team; 4-step path; application form (pre-filled role on "Apply"). |
| `/contact` | `Contact` | Contact details, WhatsApp/phone CTA, contact form. |
| `/testimonials` | `Testimonials` (as page) | Student stories. |
| `/faq` | `Faq` (as page) | FAQ list. |
| `/privacy`, `/terms` | `Legal` | Static legal text (route data `page`). |
| `**` | `NotFound` | 404 page. |
| (top bar) | `Navbar` | Language switch bn/en; course category menu. |

## 3. Entities & Fields
**ContactMessage** (form only — not stored today) *(inferred entity)*
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| name | string | yes | min 2 | — | |
| email | string | yes | email | — | |
| phone | string | no | BD phone pattern | — | |
| topic | string | yes | a published course slug, or `payment`, `career`, `other` | — | "Please choose a topic." |
| message | string | yes | min 10 | — | |

**CareerApplication** (form only — not stored today) *(inferred entity)*
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| name | string | yes | min 2 | — | |
| email | string | yes | email | — | |
| phone | string | **yes** | BD phone pattern | — | |
| role | string | yes | an opportunity role name | — | |
| portfolio | string | no | URL pattern `^https?://…` | — | |
| message | string | no | ≤ 600 | — | |

**Opportunity** (static): id, role, team (`Sports Media` / `Digital Platform` / `Software Projects`), type (`Part-time` / `Freelance` / `Internship`), skills[], description, icon.
**SportsStory** (static): id, topic (Cricket/Football/Analysis/Interview/Journalism), title, excerpt, date (YYYY-MM-DD), readMinutes, image?, icon.
**Testimonial** (static): name, course, outcome, rating, text.
**FaqItem** (static): question, answer.
**Contact info** (static): phone `+880 1700 000000`, WhatsApp `8801700000000`, email `support@courseacademy.com`, address Dhaka, social links.

## 4. Business Rules
- **BR-SITE-01** Contact form submission is simulated (800 ms); nothing is sent; form resets; toast tells user to use WhatsApp.
- **BR-SITE-02** Career application is simulated (900 ms); nothing is stored; form resets.
- **BR-SITE-03** Contact topic list = all published course titles + Payment & enrollment + Career + Something else.
- **BR-SITE-04** Career "Apply" on a role pre-fills the role field and scrolls to the form.
- **BR-SITE-05** Default language is Bangla (`bn`); choice saved in browser (`ca-language`). Missing translations fall back to the English text.
- **BR-SITE-06** In Bangla, numbers and dates use Bangla digits and `bn` locale; AM/PM → এএম/পিএম.
- **BR-SITE-07** Page titles follow `<Page> | Course Academy` (course pages use course title).
- **BR-SITE-08** WhatsApp links pre-fill a message, including course title (and price on the payment page).
- **BR-SITE-09** Sports stories are previews only; full articles not available.

## 5. Workflows & Status Transitions
Contact: Visitor fills form → validated → (future) stored + emailed to support.
Career: Visitor picks role → fills form → validated → (future) stored for HR review. Inferred future statuses: `New → Reviewed → Shortlisted/Rejected` *(inferred)*.

## 6. Permissions
All pages and forms are public. No admin screens exist for messages, applications, stories, opportunities, FAQs or testimonials.

## 7. Calculations
None.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| Contact form (simulated) | `POST /api/contact-messages` |
| Career form (simulated) | `POST /api/career-applications` |
| `OPPORTUNITIES` (static) | `GET /api/career/opportunities?team=` |
| `SPORTS_STORIES` (static) | `GET /api/sports/stories?topic=` |
| `TESTIMONIALS`, `FAQS` (static) | `GET /api/testimonials`, `GET /api/faqs` (or keep static) |
| `CONTACT` (static) | keep in config |
| Translations (`bn.translations.ts`) | keep in frontend; course content translation TBD |

## 9. Open Questions / Assumptions
- Should contact messages and career applications be stored and managed in admin? Email notification?
- Should stories, opportunities, FAQs, testimonials be admin-editable (CMS) or stay static?
- Career phone is required, contact phone optional — intended?
- Is admin-entered course content multilingual (bn + en fields)?

## 10. Source Files
- `src/app/core/data/site.data.ts`
- `src/app/pages/contact/contact.ts`, `career/career.ts`, `sports/sports.ts`, `legal/legal.ts`, `about/`, `not-found/`
- `src/app/components/testimonials/`, `faq/`, `navbar/`
- `src/app/core/i18n/language.service.ts`, `bn.translations.ts`, `bn-content.translations.ts`
