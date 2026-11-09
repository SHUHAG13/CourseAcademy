# Accounts & Authentication

## 1. Purpose
Lets visitors create an account and sign in so they can buy courses, learn, and (for staff) open the admin panel. Holds the user record that every other module references.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/login` | `Login` | Email + password + "Remember me". Demo-account quick-fill buttons (Admin, Instructor, Software instructor, Student). "Forgot password" shows a "not available yet" toast. Accepts `?returnUrl=`. |
| `/register` | `Register` | Name, email, optional BD phone, password (with 0–4 strength meter), confirm password, accept-terms checkbox. Accepts `?returnUrl=`. |
| (navbar) | `Navbar` | Shows login/register when signed out; account menu + logout when signed in; link to "home URL" (admin panel or dashboard). |

## 3. Entities & Fields
**User** (`AppUser`)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `user-<time36>-<rand6>` | PK |
| name | string | yes | min 2 chars (register), trimmed | — | |
| email | string | yes | valid email; unique (case-insensitive) | — | Stored trimmed + lower-case on register |
| phone | string | no | `^(?:\+?88)?01[3-9]\d{8}$` | — | Empty → not stored |
| password | string | yes | register: `^(?=.*[A-Za-z])(?=.*\d).{8,}$`; login form: min 6 | — | Demo stores plain text. Backend must hash. |
| role | string (Role.id) | yes | must exist | `user` (Student) | Public sign-up always Student |
| access | Permission[] | no | only assignable permissions | absent | Per-user override, see access-control |
| instructorId | string (Instructor.id) | no | — | absent | Links an account to an instructor profile |
| createdAt | ISO datetime | yes | — | now | |

**Register form-only fields**: `confirm` (required, must equal password → "mismatch"), `terms` (must be true).
**Login form-only field**: `remember` (bool, default true).

## 4. Business Rules
- **BR-AUTH-01** Login matches email case-insensitively after trimming; password must match exactly. Failure message: "Incorrect email or password." (no distinction between unknown email and wrong password).
- **BR-AUTH-02** Register rejects an email that already exists (case-insensitive): "An account with this email already exists."
- **BR-AUTH-03** Public registration always assigns role `user` (Student). Role cannot be chosen at sign-up.
- **BR-AUTH-04** Successful registration signs the user in immediately with "remember" = true.
- **BR-AUTH-05** "Remember me" on → session persists across browser restarts (localStorage); off → session ends when the tab/browser session ends (sessionStorage).
- **BR-AUTH-06** After login/register, redirect to `returnUrl` if given, otherwise to the user's home URL (BR-ACL-09).
- **BR-AUTH-07** Password strength score (display only) = count of: length ≥ 8; has a letter; has a digit; has a symbol or upper-case letter. Range 0–4.
- **BR-AUTH-08** Phone is optional; when given it must be a Bangladeshi mobile number (`01[3-9]` + 8 digits, optional `+88`/`88` prefix).
- **BR-AUTH-09** Protected routes redirect a signed-out user to `/login?returnUrl=<requested url>`.
- **BR-AUTH-10** Logout clears the session from both persistent and session storage.
- **BR-AUTH-11** Session changes in another tab are picked up (logging out in one tab logs out the others).
- **BR-AUTH-12** Password reset is not implemented (toast only).
- **BR-AUTH-13** A user whose id no longer exists in the user list is treated as signed out.

## 5. Workflows & Status Transitions
**Register**: Guest fills form → client validation → server checks unique email → user created (role Student) → session started → redirect.
**Login**: Guest submits → credentials checked → session started (persistent or tab-only) → redirect.
**Logout**: Signed-in user → session cleared → public view.
No status field on User (no active/blocked state exists).

## 6. Permissions
| Action | Guest | Student | Instructor | Admin |
|---|---|---|---|---|
| Register | ✓ | — | — | — |
| Login | ✓ | — | — | — |
| Logout | — | ✓ | ✓ | ✓ |
| View user list | — | — | via `users.view` | ✓ |
| Change role / access | see access-control | | | |

## 7. Calculations
- Password strength = number of satisfied criteria (BR-AUTH-07).

## 8. Data Source
| Current | Future endpoint |
|---|---|
| `AuthService.login()` (localStorage `ca-users`, 600 ms fake delay) | `POST /api/auth/login` |
| `AuthService.register()` (700 ms fake delay) | `POST /api/auth/register` |
| `AuthService.logout()` | `POST /api/auth/logout` |
| Session key `ca-demo-session` (local/session storage) | `GET /api/auth/me` + token/cookie |
| `AuthService.users` | `GET /api/users` (see access-control) |
| Forgot password (toast) | `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` (planned) |

## 9. Open Questions / Assumptions
- Login form requires password min 6, register requires min 8 + letter + digit. Which is the real rule? (Login rule is probably just a soft check.)
- Should users be able to edit their own profile (name, phone, password)? No screen exists.
- Should accounts have a status (active / blocked / deleted)? No user delete or block exists.
- Email verification and phone OTP are not present. Required?
- Is `phone` unique? Not enforced.
- Token type (JWT vs cookie) and session lifetime not defined.
- `Terms` acceptance is not stored. Should we store `TermsAcceptedAt`? (inferred need)

## 10. Source Files
- `src/app/core/services/auth.service.ts`
- `src/app/core/models/user.model.ts`
- `src/app/pages/auth/login.ts`, `register.ts`
- `src/app/shared/forms/validators.ts`
- `src/app/core/guards/auth.guards.ts`
- `src/app/core/data/demo-records.data.ts` (demo users)
