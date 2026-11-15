import { Injectable, computed, inject } from '@angular/core';
import { COMMENT_REPOSITORY, POST_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { COMMENT_MAX, Comment, POST_BODY_MAX, POST_TITLE_MAX, Post } from '../models/community.model';
import { ADMIN_ROLE_ID } from '../models/permission.model';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

export type CommunityRole = 'admin' | 'instructor' | 'student';

/**
 * Each course's discussion board. Enrolled students, the course's instructors
 * and admins can read it; moderators can pin, lock, hide and delete. Every
 * method checks these rules itself, so pages cannot skip them.
 */
@Injectable({ providedIn: 'root' })
export class CommunityService {
  private readonly postStore = new EntityStore(inject(POST_REPOSITORY));
  private readonly commentStore = new EntityStore(inject(COMMENT_REPOSITORY));
  private readonly auth = inject(AuthService);
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);

  readonly ready = Promise.all([this.postStore.ready, this.commentStore.ready]);
  readonly recovered = computed(() => this.postStore.recovered() || this.commentStore.recovered());
  readonly posts = this.postStore.items;
  readonly comments = this.commentStore.items;

  // ── Access ──────────────────────────────────────────────────

  canModerate(courseId: number): boolean {
    return this.permissions.can('community.moderate', courseId);
  }

  canRead(courseId: number): boolean {
    if (this.canModerate(courseId)) return true;
    return this.permissions.can('community.view') && this.enrollments.hasAccess(courseId);
  }

  canPost(courseId: number): boolean {
    return this.canRead(courseId) && (this.canModerate(courseId) || this.permissions.can('community.post'));
  }

  canComment(post: Post): boolean {
    return this.canPost(post.courseId) && (!post.locked || this.canModerate(post.courseId));
  }

  isAuthor(item: Post | Comment): boolean {
    return item.authorId === this.auth.user()?.id;
  }

  /** The badge shown next to an author's name in one course. */
  roleIn(userId: string, courseId: number): CommunityRole {
    const user = this.auth.users().find((u) => u.id === userId);
    if (user?.role === ADMIN_ROLE_ID) return 'admin';
    const course = this.courses.byId(courseId);
    const teaches = !!user?.instructorId && user.instructorId === course?.instructorId;
    const granted = this.permissions.grantsFor(userId).some((g) => g.courseId === courseId && g.permissions.includes('courses.edit'));
    return teaches || granted ? 'instructor' : 'student';
  }

  authorName(userId: string): string {
    return this.auth.users().find((u) => u.id === userId)?.name ?? 'Former member';
  }

  // ── Reading ─────────────────────────────────────────────────

  /** Posts a course's members can see: pinned first, then newest. Moderators also see hidden posts. */
  postsFor(courseId: number): Post[] {
    if (!this.canRead(courseId)) return [];
    const moderator = this.canModerate(courseId);
    return this.posts()
      .filter((p) => p.courseId === courseId && (moderator || !p.hidden))
      .sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.createdAt.localeCompare(a.createdAt));
  }

  /** Visible post count, for the teaser shown to people outside the community. */
  publicCount(courseId: number): number {
    return this.posts().filter((p) => p.courseId === courseId && !p.hidden).length;
  }

  /** Top-level comments of a post, oldest first. */
  commentsFor(post: Post): Comment[] {
    return this.visibleComments(post).filter((c) => c.parentId === null);
  }

  repliesTo(comment: Comment): Comment[] {
    const post = this.posts().find((p) => p.id === comment.postId);
    return post ? this.visibleComments(post).filter((c) => c.parentId === comment.id) : [];
  }

  commentCount(post: Post): number {
    return this.visibleComments(post).length;
  }

  private visibleComments(post: Post): Comment[] {
    if (!this.canRead(post.courseId)) return [];
    const moderator = this.canModerate(post.courseId);
    return this.comments()
      .filter((c) => c.postId === post.id && (moderator || !c.hidden))
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  }

  // ── Writing ─────────────────────────────────────────────────

  async createPost(courseId: number, title: string, body: string): Promise<Post> {
    if (!this.canPost(courseId)) throw new PermissionDeniedError();
    return this.postStore.save({
      id: uid('post'),
      courseId,
      authorId: this.auth.user()!.id,
      ...checkPost(title, body),
      createdAt: new Date().toISOString(),
      pinned: false,
      locked: false,
      hidden: false,
    });
  }

  async editPost(postId: string, title: string, body: string): Promise<Post> {
    const post = this.findPost(postId);
    if (!this.isAuthor(post) || !this.canRead(post.courseId)) throw new PermissionDeniedError();
    return this.postStore.save({ ...post, ...checkPost(title, body), editedAt: new Date().toISOString() });
  }

  /** Authors and moderators can delete a post. Its comments go with it. */
  async deletePost(postId: string): Promise<void> {
    const post = this.findPost(postId);
    if (!(this.isAuthor(post) && this.canRead(post.courseId)) && !this.canModerate(post.courseId)) {
      throw new PermissionDeniedError();
    }
    for (const c of this.comments().filter((x) => x.postId === postId)) await this.commentStore.remove(c.id);
    await this.postStore.remove(postId);
  }

  /** Comments on a post, or replies when `parentId` is given. A reply to a reply joins the same thread. */
  async addComment(postId: string, body: string, parentId: string | null = null): Promise<Comment> {
    const post = this.findPost(postId);
    if (!this.canComment(post)) {
      throw post.locked ? new Error('This discussion is locked.') : new PermissionDeniedError();
    }
    let parent = parentId ? this.comments().find((c) => c.id === parentId && c.postId === postId) : undefined;
    if (parentId && !parent) throw new Error('The comment you replied to was removed.');
    if (parent?.parentId) parent = this.comments().find((c) => c.id === parent!.parentId);

    return this.commentStore.save({
      id: uid('comment'),
      postId,
      courseId: post.courseId,
      parentId: parent?.id ?? null,
      authorId: this.auth.user()!.id,
      body: checkText(body, COMMENT_MAX, 'Write a comment.'),
      createdAt: new Date().toISOString(),
      hidden: false,
    });
  }

  async editComment(commentId: string, body: string): Promise<Comment> {
    const comment = this.findComment(commentId);
    if (!this.isAuthor(comment) || !this.canRead(comment.courseId)) throw new PermissionDeniedError();
    return this.commentStore.save({
      ...comment,
      body: checkText(body, COMMENT_MAX, 'Write a comment.'),
      editedAt: new Date().toISOString(),
    });
  }

  /** Authors and moderators can delete a comment. Its replies go with it. */
  async deleteComment(commentId: string): Promise<void> {
    const comment = this.findComment(commentId);
    if (!(this.isAuthor(comment) && this.canRead(comment.courseId)) && !this.canModerate(comment.courseId)) {
      throw new PermissionDeniedError();
    }
    for (const r of this.comments().filter((x) => x.parentId === commentId)) await this.commentStore.remove(r.id);
    await this.commentStore.remove(commentId);
  }

  // ── Moderation ──────────────────────────────────────────────

  async setPostFlags(postId: string, flags: Partial<Pick<Post, 'pinned' | 'locked' | 'hidden'>>): Promise<Post> {
    const post = this.findPost(postId);
    if (!this.canModerate(post.courseId)) throw new PermissionDeniedError();
    return this.postStore.save({ ...post, ...flags });
  }

  async setCommentHidden(commentId: string, hidden: boolean): Promise<Comment> {
    const comment = this.findComment(commentId);
    if (!this.canModerate(comment.courseId)) throw new PermissionDeniedError();
    return this.commentStore.save({ ...comment, hidden });
  }

  /** Clean-up when a course is deleted. */
  async removeForCourse(courseId: number): Promise<void> {
    this.permissions.assert('courses.delete', courseId);
    for (const c of this.comments().filter((x) => x.courseId === courseId)) await this.commentStore.remove(c.id);
    for (const p of this.posts().filter((x) => x.courseId === courseId)) await this.postStore.remove(p.id);
  }

  async reset(): Promise<void> {
    await Promise.all([this.postStore.reset(), this.commentStore.reset()]);
  }

  private findPost(id: string): Post {
    const post = this.posts().find((p) => p.id === id);
    if (!post) throw new Error('This post was removed.');
    return post;
  }

  private findComment(id: string): Comment {
    const comment = this.comments().find((c) => c.id === id);
    if (!comment) throw new Error('This comment was removed.');
    return comment;
  }
}

function checkPost(title: string, body: string): Pick<Post, 'title' | 'body'> {
  return {
    title: checkText(title, POST_TITLE_MAX, 'Give your post a title.'),
    body: checkText(body, POST_BODY_MAX, 'Write something in your post.'),
  };
}

function checkText(text: string, max: number, emptyMessage: string): string {
  const trimmed = text.trim();
  if (!trimmed) throw new Error(emptyMessage);
  if (trimmed.length > max) throw new Error('This is too long. Please shorten it.');
  return trimmed;
}
