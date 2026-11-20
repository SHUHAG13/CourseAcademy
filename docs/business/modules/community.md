# Course Community (Discussions & Moderation)

## 1. Purpose
Each course has its own discussion board where enrolled students, the course's instructors and admins post questions, comment and reply; moderators can pin, lock, hide and delete.
Served by the API (backend Step 8, tables in the `Community` schema); the browser-storage demo mode follows the same rules.

## 2. Screens & Routes
| Route | Component | What the user sees / does |
|---|---|---|
| `/courses/:slug#community` | `CourseCommunity` (inside `CourseDetail`) | Outsiders: teaser with visible post count and "Log in to join" (returnUrl = course page). Members: post list (pinned first), new post form, expand comments, comment, reply, edit/delete own, role badges (Admin/Instructor/Student). Moderators: pin, lock, hide/unhide posts and comments, delete any. API mode: 20 posts at a time with a "Load more discussions" button; comments load when a post is opened. |
| `/learn/:slug` | `Learn` | Link to the community if the user can read it. |
| `/admin/community` | `AdminCommunity` | Moderation list for courses the user can moderate: filter by course, status (all/visible/hidden/pinned/locked), search (title/body/author). Posts list + latest 20 comments. Pin/lock/hide/delete. Guard `community.moderate` (any course). API mode: the server filters (search waits 300 ms after typing). |

## 3. Entities & Fields
**Post**
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `post-…` | PK |
| courseId | number | yes | FK Course | — | |
| authorId | string | yes | FK User | current user | |
| title | string | yes | trimmed, 1–120 chars | — | |
| body | string | yes | trimmed, 1–2000 chars | — | |
| createdAt | ISO datetime | yes | — | now | |
| editedAt | ISO datetime | no | — | set on edit | |
| pinned | bool | yes | — | false | |
| locked | bool | yes | — | false | |
| hidden | bool | yes | — | false | |

**Comment** (comment or reply)
| Field | Type | Required | Validation | Default | Notes |
|---|---|---|---|---|---|
| id | string | yes | — | `comment-…` | PK |
| postId | string | yes | FK Post | — | |
| courseId | number | yes | FK Course | from post | denormalised |
| parentId | string \| null | no | FK Comment (top-level only) | null | Replies are 1 level deep |
| authorId | string | yes | FK User | current user | |
| body | string | yes | trimmed, 1–1000 chars | — | |
| createdAt | ISO datetime | yes | — | now | |
| editedAt | ISO datetime | no | — | set on edit | |
| hidden | bool | yes | — | false | |

## 4. Business Rules
- **BR-COM-01** Moderator = `community.moderate` globally or on the course.
- **BR-COM-02** Read = moderator OR (`community.view` AND course access per BR-ENR-03).
- **BR-COM-03** Post/comment = can read AND (moderator OR `community.post`).
- **BR-COM-04** Comment on a locked post only by moderators; others get "This discussion is locked."
- **BR-COM-05** Post list: pinned first, then newest first. Hidden posts visible to moderators only; for others a hidden post behaves as removed (its comments cannot be read or added: "This post was removed."). API mode: 20 posts per page, "Load more discussions" adds the next page.
- **BR-COM-06** Comments: oldest first; hidden ones visible to moderators only, and so are the replies under a hidden comment. Comment count = the comments and replies the viewer can see.
- **BR-COM-07** Replies are one level deep: replying to a reply attaches to the top-level comment. Replying to a removed comment fails ("The comment you replied to was removed.").
- **BR-COM-08** Edit post/comment: author only (and must still be able to read). Sets `editedAt`. Moderators cannot edit others' content.
- **BR-COM-09** Delete post: author (who can read) or moderator; deletes all its comments. Delete comment: author or moderator; deletes its replies. With confirm dialog. Demo mode removes the rows; the API soft-deletes them (`IsDeleted`).
- **BR-COM-10** Pin / lock / hide post and hide comment: moderators only.
- **BR-COM-11** Text limits: title ≤ 120, body ≤ 2000, comment ≤ 1000 (after trim); empty → "Give your post a title." / "Write something in your post." / "Write a comment."; too long → "This is too long. Please shorten it."
- **BR-COM-12** Author badge: Admin if user role admin; Instructor if user's instructorId = course instructorId OR has grant `courses.edit` on course; else Student.
- **BR-COM-13** Deleted authors show as "Former member".
- **BR-COM-14** Public teaser count = non-hidden posts of the course.
- **BR-COM-15** Deleting a course deletes all its posts and comments (API: soft delete together with the course).
- **BR-COM-16** Admin moderation list shows the latest 20 comments only; status filter pinned/locked doesn't filter comments.

## 5. Workflows & Status Transitions
Post flags (independent toggles), by Moderator: `pinned: false ⇄ true`, `locked: false ⇄ true`, `hidden: false ⇄ true`. Comment: `hidden: false ⇄ true`.
Discussion: Member creates post → members comment/reply → moderator may pin/lock/hide → author or moderator deletes.

## 6. Permissions
| Action | Guest | Enrolled student | Course instructor (grant) | Admin |
|---|---|---|---|---|
| See teaser count | ✓ | ✓ | ✓ | ✓ |
| Read | — | ✓ | ✓ | ✓ |
| Post / comment / reply | — | ✓ | ✓ | ✓ |
| Comment on locked post | — | — | ✓ | ✓ |
| Edit own | — | ✓ | ✓ | ✓ |
| Delete own | — | ✓ | ✓ | ✓ |
| Delete any / pin / lock / hide | — | — | ✓ | ✓ |
| Admin moderation screen | — | — | ✓ (own courses) | ✓ |

## 7. Calculations
- Comment count per post = visible comments + replies.

## 8. Data Source
API mode (default) uses `/api/v1` (api-contract §12); demo mode keeps browser storage (`ca-posts`, `ca-comments`).
| Service method | API endpoint |
|---|---|
| `loadCourse` → `publicCount`, `postsFor` | `GET /courses/{slug}/community` (public), then `GET /courses/{slug}/posts?page=1&pageSize=20` |
| `loadMorePosts` / `hasMorePosts` | `GET /courses/{slug}/posts?page=n&pageSize=20` |
| `createPost` / `editPost` / `deletePost` | `POST /courses/{slug}/posts`, `PUT /posts/{id}`, `DELETE /posts/{id}` |
| `loadComments` → `commentsFor` / `repliesTo` | `GET /posts/{id}/comments` (tree, 1 level) |
| `addComment` / `editComment` / `deleteComment` | `POST /posts/{id}/comments` `{body, parentId?}`, `PUT /comments/{id}`, `DELETE /comments/{id}` |
| `setPostFlags` | `PATCH /posts/{id}/flags` `{isPinned?, isLocked?, isHidden?}` |
| `setCommentHidden` | `PATCH /comments/{id}/hidden` |
| `loadModeration` → `posts`, `comments` (admin) | `GET /admin/community/posts?courseId=&status=&search=`, `GET /admin/community/comments?courseId=&status=&search=&limit=20` |
| `canEdit`, `canDelete`, `canComment`, `authorOf`, `badgeOf` | worked out by the server (`PostDto`/`CommentDto`) |

## 9. Open Questions / Assumptions
- ~~Hard delete vs soft delete for posts/comments?~~ — API soft-deletes, like every table (BR-COM-09).
- Reporting/flagging by students? Notifications for replies?
- ~~No pagination on post lists~~ — decided 2026-11-20: 20 per page with "Load more discussions" (BR-COM-05).
- Author can still edit a post after it is locked or hidden. Intended?
- Revoked students lose read access; their posts stay. OK?

## 10. Source Files
- `src/app/core/services/community.service.ts`
- `src/app/core/models/community.model.ts`, `src/app/core/api/community.models.ts`
- Backend: `Controllers/CommunityControllers.cs`, `Services/CommunityService.cs`, `Repositories/CommunityRepositories.cs`, `Entities/Community.cs`, `Data/Configurations/CommunityConfigurations.cs`, `Data/Seed/DemoCertificationCommunitySeeder.cs`
- `src/app/pages/course-detail/course-community/course-community.ts` / `.html`
- `src/app/pages/admin/admin-community.ts`
