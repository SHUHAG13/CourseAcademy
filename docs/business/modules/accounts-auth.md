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
- **BR-AUTH-14** (API) Access tokens last 15 minutes. Refresh tokens last 30 days with "Remember me", otherwise 24 hours, and work only once: each refresh returns a new pair. Reusing a used refresh token ends every session of that user.
- **BR-AUTH-15** (API) Sign-up, sign-in and refresh are limited to 20 attempts per minute per client address; more → "Too many attempts. Please wait a minute and try again."
- **BR-AUTH-16** (API) Sign-up password rule is enforced by the server: 8–100 characters with at least one letter and one digit. Sign-in only requires a non-empty password.
- **BR-AUTH-17** (API) When the session can't be renewed, the user is signed out, sees "Your session has expired. Please log in again." and is sent to `/login?returnUrl=<current page>`.

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
| Current | Endpoint |
|---|---|
| `AuthService.login()` | **Done (API mode):** `POST /api/v1/auth/login` · demo mode: localStorage `ca-users` |
| `AuthService.register()` | **Done (API mode):** `POST /api/v1/auth/register` |
| `AuthService.logout()` | **Done (API mode):** `POST /api/v1/auth/logout` |
| Session | **Done (API mode):** tokens in `ca-auth` (localStorage with "Remember me", else sessionStorage); `GET /api/v1/auth/me` on start; `POST /api/v1/auth/refresh` by `authInterceptor` · demo mode: `ca-demo-session` |
| `AuthService.users` | **Done (API mode):** `GET /api/v1/users` (see access-control) |
| Forgot password (toast) | `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` (planned) |

## 9. Open Questions / Assumptions
- Login form requires password min 6, register requires min 8 + letter + digit. The API enforces the register rule and accepts any non-empty password at sign-in (BR-AUTH-16). Should the login form's min-6 check stay?
- Should users be able to edit their own profile (name, phone, password)? No screen exists.
- Should accounts have a status (active / blocked / deleted)? No user delete or block exists.
- Email verification and phone OTP are not present. Required?
- Is `phone` unique? Not enforced.
- `Terms` acceptance is not stored. Should we store `TermsAcceptedAt`? (inferred need)

## 10. Source Files
- `src/app/core/services/auth.service.ts`
- `src/app/core/models/user.model.ts`
- `src/app/pages/auth/login.ts`, `register.ts`
- `src/app/shared/forms/validators.ts`
- `src/app/core/guards/auth.guards.ts`
- `src/app/core/data/demo-records.data.ts` (demo users)
