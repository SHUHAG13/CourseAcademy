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
}

export const POST_TITLE_MAX = 120;
export const POST_BODY_MAX = 2000;
export const COMMENT_MAX = 1000;
