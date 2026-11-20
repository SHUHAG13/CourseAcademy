/** The badge shown next to an author's name in one course (BR-COM-12). */
export type CommunityRole = 'admin' | 'instructor' | 'student';

/** API mode: the author as the server shows them, with their badge in the course. */
export interface CommunityAuthor {
  name: string;
  badge: CommunityRole;
}

/** A discussion started in one course's community. */
export interface Post {
  id: string;
  courseId: number;
  authorId: string;
  title: string;
  body: string;
  createdAt: string;
  editedAt?: string;
  /** Shown above the other posts. */
  pinned: boolean;
  /** No new comments, except from moderators. */
  locked: boolean;
  /** Removed from view by a moderator. Moderators still see it. */
  hidden: boolean;
  // API mode: worked out by the server for the signed-in user.
  author?: CommunityAuthor;
  commentCount?: number;
  canEdit?: boolean;
  canDelete?: boolean;
  canComment?: boolean;
  /** Admin moderation list only. */
  courseTitle?: string;
  courseSlug?: string;
}

/** A comment on a post, or with `parentId`, a reply to a comment. Replies are one level deep. */
export interface Comment {
  id: string;
  postId: string;
  courseId: number;
  parentId: string | null;
  authorId: string;
  body: string;
  createdAt: string;
  editedAt?: string;
  hidden: boolean;
  // API mode: worked out by the server for the signed-in user.
  author?: CommunityAuthor;
  canEdit?: boolean;
  canDelete?: boolean;
  /** Admin moderation list only. */
  postTitle?: string;
  courseTitle?: string;
  courseSlug?: string;
}

export const POST_TITLE_MAX = 120;
export const POST_BODY_MAX = 2000;
export const COMMENT_MAX = 1000;
