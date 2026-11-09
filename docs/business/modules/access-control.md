# Access Control (Roles, Permissions, User Access, Course Grants)

## 1. Purpose
Decides who can open which screen and perform which action. Supports roles, a per-user override of a role, and per-course permissions (e.g. an instructor managing only the courses they teach).

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/admin` (layout) | `AdminLayout` | Sidebar lists only admin screens the user can view; Roles + User access collapse into one "Security" link. Guard: `admin.access`. |
| `/admin/security` | `SecurityLayout` | Redirects to the first security tab the user can view. Tabs: Roles, User access. |
| `/admin/security/roles` | `AdminRoles` | List roles with screen count + member count. Create / edit (name, description, access matrix) / delete. |
| `/admin/security/access` | `AdminAccess` | List users (search name/email) with role dropdown and access label (Full access / Custom / Role defaults). Change a user's role. |
| `/admin/security/access/:userId` | `UserAccess` | One user's access matrix (edit + save, or "reset to role defaults") and per-course grant checkboxes. |
| `/admin/users` | `AdminUsers` | Read-only user list: name, email, role name, active course count; search. |

## 3. Entities & Fields
**Role**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `role-<time36>-<rand6>`; system: `admin`, `instructor`, `user` | PK |
| name | string | yes | trimmed, non-empty, unique (case-insensitive) | — | |
| description | string | no | trimmed | '' | |
| permissions | Permission[] | yes | only assignable permissions (all except `admin.access`) | [] | Admin role always saved with ALL permissions |
| system | bool | yes | cannot be set by client; kept from existing record | false | Built-in roles cannot be deleted |

**CourseGrant**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `grant-…` | PK |
| userId | string | yes | FK User | — | unique with courseId |
| courseId | number | yes | FK Course | — | |
| permissions | Permission[] | yes | subset of course permissions: `courses.view`, `courses.edit`, `community.moderate`, `certificates.manage`; non-empty | — | Empty list deletes the grant |

**User.access** (override) — Permission[]; when present it *replaces* the role's permissions. See accounts-auth.

**Screen** (static config, not stored) — id, label, area (admin/student), path, icon, view/create/edit/delete/extra permission, courseScoped, section.

**Permission enum** — see data-model.md (28 values).

## 4. Business Rules
- **BR-ACL-01** Effective permissions = (role `admin` → ALL) else (`user.access` if set, else role.permissions) **plus** course grants for the specific course.
- **BR-ACL-02** `admin.access` is never assigned directly; it is derived: true if the user can view any admin screen OR has at least one course grant.
- **BR-ACL-03** A course-scoped check (`can(p, courseId)`) passes if the user has `p` globally or a grant with `p` on that course.
- **BR-ACL-04** Course-scoped screens (Courses, Community moderation, Certificates) open if the user has the view permission globally or on **any** course; they then list only the courses the user has rights on.
- **BR-ACL-05** Role save: name required and unique (case-insensitive); `admin.access` stripped; Admin role permissions always forced to ALL; `system` flag cannot be changed.
- **BR-ACL-06** Built-in (system) roles cannot be deleted: "Built-in roles cannot be deleted."
- **BR-ACL-07** A role with users assigned cannot be deleted: "Move the users with this role to another role first."
- **BR-ACL-08** A user cannot change their own role ("You can't change your own role.") or their own access ("You can't change your own access.").
- **BR-ACL-09** Home URL after login = first admin screen (in sidebar order) the user can view (section screens → `/admin/security`); else `/dashboard` if `dashboard.view`; else `/`.
- **BR-ACL-10** Users with role Admin always have full access; their access cannot be customised ("Admin users always have full access.").
- **BR-ACL-11** "Reset to role defaults" removes `user.access`, returning the user to role permissions.
- **BR-ACL-12** Access matrix: unticking a screen's View removes all its actions; ticking any action also ticks View.
- **BR-ACL-13** Setting a course grant keeps only course permissions; an empty list deletes the grant.
- **BR-ACL-14** Deleting a course deletes all grants on it (requires `courses.delete` on the course).
- **BR-ACL-15** Guard failure shows "You don't have permission to open that page." and redirects to home URL (never loops).
- **BR-ACL-16** Every service write re-checks permissions (`assert`), independent of UI hiding. Backend must do the same on every endpoint.
- **BR-ACL-17** Changing a role requires `roles.edit`; changing user access / course grants requires `access.edit`.
- **BR-ACL-18** Demo seed: each instructor account gets a grant (`courses.view`, `courses.edit`, `community.moderate`, `certificates.manage`) on every course whose `instructorId` matches the account's `instructorId`.

## 5. Workflows & Status Transitions
- **Create role**: Admin (roles.edit) → New role → name, description, matrix → Save → validated (BR-ACL-05).
- **Assign role**: (roles.edit) → User access list → choose role → saved (BR-ACL-08).
- **Custom access**: (access.edit) → user page → edit matrix → Save → `user.access` set. → Reset → `user.access` cleared.
- **Course grant**: (access.edit) → user page → tick course × permission → grant upserted/deleted immediately.
No status transitions.

## 6. Permissions
| Action | Permission | Default holders |
|---|---|---|
| Open admin panel | derived `admin.access` | Admin; instructors with grants |
| View roles | `roles.view` | Admin |
| Create/edit/delete/assign roles | `roles.edit` | Admin |
| View user access | `access.view` | Admin |
| Change user access / course grants | `access.edit` | Admin |
| View users list | `users.view` | Admin |

Default role permissions:
| Role | Permissions |
|---|---|
| Admin (`admin`) | ALL |
| Instructor (`instructor`) | dashboard.view, learn.view, checkout.view, community.view, community.post (+ course grants) |
| Student (`user`) | dashboard.view, learn.view, checkout.view, community.view, community.post |

## 7. Calculations
- Role "screen count" = number of screens whose view permission is in the role's permissions.
- Role "member count" = users with `role = role.id`.
- Users list "active courses" = enrollments of the user with status `active`.

## 8. Data Source
| Current | Future endpoint |
|---|---|
| `PermissionService.roles` (`ca-roles`) | `GET /api/roles` |
| `saveRole` | `POST /api/roles`, `PUT /api/roles/{id}` |
| `removeRole` | `DELETE /api/roles/{id}` |
| `setUserRole` | `PUT /api/users/{id}/role` |
| `setUserAccess` / `resetUserAccess` | `PUT /api/users/{id}/access`, `DELETE /api/users/{id}/access` |
| `grants` (`ca-course-grants`) | `GET /api/users/{id}/course-grants` |
| `setCourseGrant` | `PUT /api/users/{id}/course-grants/{courseId}` (empty → delete) |
| `AuthService.users` | `GET /api/users?search=` |
| client `permissionsOf` | `GET /api/auth/me` returns effective permissions + grants |

## 9. Open Questions / Assumptions
- Instructor and Student roles have identical global permissions; the only difference is course grants. Is that intended?
- Grants are not created automatically when an admin assigns an instructor to a course (only demo seed does it). Should assigning `Course.instructorId` auto-grant the linked account?
- No UI to set `User.instructorId` (linking an account to an instructor profile). Needed?
- Is `admin.access` derivation from "any course grant" correct even for grants with only `courses.view`?
- No audit log of role/access changes. Required?
- No user create/delete/block in admin.

## 10. Source Files
- `src/app/core/services/permission.service.ts`
- `src/app/core/models/permission.model.ts`
- `src/app/core/data/screens.data.ts`, `access.data.ts`
- `src/app/core/guards/auth.guards.ts`
- `src/app/pages/admin/security/*`, `admin-users.ts`, `admin-layout.ts`
- `src/app/shared/components/access-matrix/access-matrix.ts`
