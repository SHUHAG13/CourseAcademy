import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import { PagedResult } from '../api/api.models';
import {
  CommentDto,
  CommunityInfoDto,
  ModerationCommentDto,
  ModerationPostDto,
  PostDto,
  fromCommentDto,
  fromPostDto,
} from '../api/community.models';
import { COMMENT_REPOSITORY, POST_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { COMMENT_MAX, Comment, CommunityRole, POST_BODY_MAX, POST_TITLE_MAX, Post } from '../models/community.model';
import { ADMIN_ROLE_ID } from '../models/permission.model';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

export type { CommunityRole } from '../models/community.model';

/** Posts loaded per page on the course page (API mode). */
export const POSTS_PAGE_SIZE = 20;
const MODERATION_PAGE_SIZE = 100;

/** Filters of the admin moderation page. */
export interface ModerationFilter {
  courseId: number;
  status: 'all' | 'visible' | 'hidden' | 'pinned' | 'locked';
  search: string;
}

interface LoadedPosts {
  items: Post[];
  total: number;
  /** The page "Load more" asks for, counted in pages of POSTS_PAGE_SIZE. */
  nextPage: number;
}

/**
 * Each course's discussion board. Enrolled students, the course's instructors
 * and admins can read it; moderators can pin, lock, hide and delete. Every
 * method checks these rules itself, so pages cannot skip them.
 * - API mode: the API keeps the discussions and checks every change again; pages load what they show
 *   (`loadCourse`, `loadMorePosts`, `loadComments`, `loadModeration`).
 * - Demo mode: everything lives in browser storage.
 */
@Injectable({ providedIn: 'root' })
export class CommunityService {
  private readonly postStore = new EntityStore(inject(POST_REPOSITORY));
  private readonly commentStore = new EntityStore(inject(COMMENT_REPOSITORY));
  private readonly auth = inject(AuthService);
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);

  // API mode: what the pages loaded.
  private readonly apiInfo = signal(new Map<number, CommunityInfoDto>());
  private readonly apiPosts = signal(new Map<number, LoadedPosts>());
  /** Comments and replies of opened posts, oldest first, by post id. */
  private readonly apiComments = signal(new Map<string, Comment[]>());
  private readonly apiModerationPosts = signal<Post[]>([]);
  private readonly apiModerationComments = signal<Comment[]>([]);
  private moderationFilter: ModerationFilter | null = null;

  readonly ready: Promise<unknown> = this.apiMode ? Promise.resolve() : Promise.all([this.postStore.ready, this.commentStore.ready]);
  readonly recovered = computed(() => !this.apiMode && (this.postStore.recovered() || this.commentStore.recovered()));
  /** Demo mode: every post. API mode: the admin moderation list (after `loadModeration`). */
  readonly posts = computed(() => (this.apiMode ? this.apiModerationPosts() : this.postStore.items()));
  /** Demo mode: every comment. API mode: the newest comments of the admin moderation list. */
  readonly comments = computed(() => (this.apiMode ? this.apiModerationComments() : this.commentStore.items()));

  constructor() {
    if (this.apiMode) {
      // Another account signs in: what it may see differs, so everything loaded is dropped.
      let loadedFor: number | null | undefined;
      effect(() => {
        const id = this.auth.currentSession()?.id ?? null;
        untracked(() => {
          if (loadedFor !== undefined && loadedFor !== id) {
            this.apiInfo.set(new Map());
            this.apiPosts.set(new Map());
            this.apiComments.set(new Map());
            this.apiModerationPosts.set([]);
            this.apiModerationComments.set([]);
          }
          loadedFor = id;
        });
      });
    }
  }

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
    if (this.apiMode) return post.canComment ?? false;
    return this.canPost(post.courseId) && (!post.locked || this.canModerate(post.courseId));
  }

  isAuthor(item: Post | Comment): boolean {
    return item.authorId === this.auth.user()?.id;
  }

  /** Only the author edits, while they can still read the community (BR-COM-08). */
  canEdit(item: Post | Comment): boolean {
    if (this.apiMode) return item.canEdit ?? false;
    return this.isAuthor(item) && this.canRead(item.courseId);
  }

  /** The author, or a moderator, may delete (BR-COM-09). */
  canDelete(item: Post | Comment): boolean {
    if (this.apiMode) return item.canDelete ?? false;
    return (this.isAuthor(item) && this.canRead(item.courseId)) || this.canModerate(item.courseId);
  }

  /** The author's name as shown on a post or comment. */
  authorOf(item: Post | Comment): string {
    return item.author?.name ?? this.authorName(item.authorId);
  }

  /** The author's badge on a post or comment (BR-COM-12). */
  badgeOf(item: Post | Comment): CommunityRole {
    return item.author?.badge ?? this.roleIn(item.authorId, item.courseId);
  }

  /** Demo mode: the badge shown next to an author's name in one course. */
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
    if (this.apiMode) return this.apiPosts().get(courseId)?.items ?? [];
    if (!this.canRead(courseId)) return [];
    const moderator = this.canModerate(courseId);
    return this.posts()
      .filter((p) => p.courseId === courseId && (moderator || !p.hidden))
      .sort((a, b) => Number(b.pinned) - Number(a.pinned) || b.createdAt.localeCompare(a.createdAt));
  }

  /** API mode: whether the course page can load more posts. */
  hasMorePosts(courseId: number): boolean {
    const loaded = this.apiPosts().get(courseId);
    return !!loaded && loaded.items.length < loaded.total;
  }

  /** Visible post count, for the teaser shown to people outside the community. */
  publicCount(courseId: number): number {
    if (this.apiMode) return this.apiInfo().get(courseId)?.publicPostCount ?? 0;
    return this.posts().filter((p) => p.courseId === courseId && !p.hidden).length;
  }

  /** Top-level comments of a post, oldest first. API mode: after `loadComments`. */
  commentsFor(post: Post): Comment[] {
    if (this.apiMode) return (this.apiComments().get(post.id) ?? []).filter((c) => c.parentId === null);
    return this.visibleComments(post).filter((c) => c.parentId === null);
  }

  repliesTo(comment: Comment): Comment[] {
    if (this.apiMode) return (this.apiComments().get(comment.postId) ?? []).filter((c) => c.parentId === comment.id);
    const post = this.posts().find((p) => p.id === comment.postId);
    return post ? this.visibleComments(post).filter((c) => c.parentId === comment.id) : [];
  }

  commentCount(post: Post): number {
    if (this.apiMode) return post.commentCount ?? 0;
    return this.visibleComments(post).length;
  }

  // ── Loading (API mode) ──────────────────────────────────────

  /** Reads the course's community: the public count, and the first page of posts for members. */
  async loadCourse(courseId: number): Promise<void> {
    const course = this.courses.byId(courseId);
    if (!this.apiMode || !course) return;
    const info = await this.api.get<CommunityInfoDto>(`courses/${encodeURIComponent(course.slug)}/community`);
    this.apiInfo.update((map) => new Map(map).set(courseId, info));
    if (info.canRead) {
      await this.loadPosts(courseId, 1, POSTS_PAGE_SIZE, false);
    } else {
      this.apiPosts.update((map) => {
        const next = new Map(map);
        next.delete(courseId);
        return next;
      });
    }
  }

  /** Adds the next page of posts ("Load more"). */
  async loadMorePosts(courseId: number): Promise<void> {
    await this.loadPosts(courseId, this.apiPosts().get(courseId)?.nextPage ?? 1, POSTS_PAGE_SIZE, true);
  }

  /** Reads a post's comments and replies. */
  async loadComments(postId: string): Promise<void> {
    if (!this.apiMode) return;
    const threads = await this.api.get<CommentDto[]>(`posts/${postId}/comments`);
    const flat = threads.flatMap((t) => [fromCommentDto(t), ...t.replies.map((r) => fromCommentDto(r))]);
    this.apiComments.update((map) => new Map(map).set(postId, flat));
  }

  /** Reads the admin moderation lists for the given filters. */
  async loadModeration(filter: ModerationFilter): Promise<void> {
    if (!this.apiMode) return;
    this.moderationFilter = filter;
    const query = {
      courseId: filter.courseId || undefined,
      status: filter.status,
      search: filter.search.trim() || undefined,
    };
    const posts: Post[] = [];
    for (let page = 1; ; page++) {
      const result = await this.api.get<PagedResult<ModerationPostDto>>('admin/community/posts', { ...query, page, pageSize: MODERATION_PAGE_SIZE });
      posts.push(...result.items.map((m) => ({ ...fromPostDto(m.post), courseTitle: m.courseTitle, courseSlug: m.courseSlug })));
      if (posts.length >= result.total || result.items.length === 0) break;
    }
    const comments = await this.api.get<ModerationCommentDto[]>('admin/community/comments', query);
    // Ignore an older response that arrives after a newer filter was chosen.
    if (this.moderationFilter !== filter) return;
    this.apiModerationPosts.set(posts);
    this.apiModerationComments.set(
      comments.map((m) => ({ ...fromCommentDto(m.comment), postTitle: m.postTitle, courseTitle: m.courseTitle, courseSlug: m.courseSlug })),
    );
  }

  private async loadPosts(courseId: number, page: number, pageSize: number, append: boolean): Promise<void> {
    const course = this.courses.byId(courseId);
    if (!this.apiMode || !course) return;
    const result = await this.api.get<PagedResult<PostDto>>(`courses/${encodeURIComponent(course.slug)}/posts`, { page, pageSize });
    const items = result.items.map(fromPostDto);
    // Pages after a reload with a bigger page size continue where it ended.
    const nextPage = Math.ceil((page * pageSize) / POSTS_PAGE_SIZE) + 1;
    this.apiPosts.update((map) => {
      const before = append ? (map.get(courseId)?.items ?? []) : [];
      const ids = new Set(items.map((p) => p.id));
      return new Map(map).set(courseId, { items: [...before.filter((p) => !ids.has(p.id)), ...items], total: result.total, nextPage });
    });
  }

  /** After a change: reloads what the pages show, keeping as many posts as were loaded. */
  private async refresh(courseId: number, postId?: string): Promise<void> {
    const course = this.courses.byId(courseId);
    const loaded = this.apiPosts().get(courseId)?.items.length ?? 0;
    const work: Promise<unknown>[] = [];
    if (course && this.apiInfo().has(courseId)) {
      work.push(
        this.api.get<CommunityInfoDto>(`courses/${encodeURIComponent(course.slug)}/community`).then((info) =>
          this.apiInfo.update((map) => new Map(map).set(courseId, info)),
        ),
      );
    }
    if (this.apiPosts().has(courseId)) work.push(this.loadPosts(courseId, 1, Math.min(Math.max(loaded, POSTS_PAGE_SIZE), 100), false));
    if (postId && this.apiComments().has(postId)) work.push(this.loadComments(postId).catch(() => this.forgetComments(postId)));
    if (this.moderationFilter) work.push(this.loadModeration(this.moderationFilter));
    await Promise.all(work);
  }

  private forgetComments(postId: string): void {
    this.apiComments.update((map) => {
      const next = new Map(map);
      next.delete(postId);
      return next;
    });
  }

  /** API mode: the course of a loaded post. */
  private courseOfPost(postId: string): number {
    for (const [courseId, loaded] of this.apiPosts()) if (loaded.items.some((p) => p.id === postId)) return courseId;
    return this.apiModerationPosts().find((p) => p.id === postId)?.courseId ?? 0;
  }

  /** API mode: the post of a loaded comment. */
  private postOfComment(commentId: string): string | undefined {
    for (const [postId, list] of this.apiComments()) if (list.some((c) => c.id === commentId)) return postId;
    return this.apiModerationComments().find((c) => c.id === commentId)?.postId;
  }

  private courseOfComment(commentId: string): number {
    const all = [...[...this.apiComments().values()].flat(), ...this.apiModerationComments()];
    return all.find((c) => c.id === commentId)?.courseId ?? 0;
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
    if (this.apiMode) {
      const slug = this.courses.byId(courseId)?.slug ?? '';
      const post = fromPostDto(await this.api.post<PostDto>(`courses/${encodeURIComponent(slug)}/posts`, { title, body }));
      await this.refresh(courseId);
      return post;
    }
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
    if (this.apiMode) {
      const post = fromPostDto(await this.api.put<PostDto>(`posts/${postId}`, { title, body }));
      await this.refresh(post.courseId);
      return post;
    }
    const post = this.findPost(postId);
    if (!this.isAuthor(post) || !this.canRead(post.courseId)) throw new PermissionDeniedError();
    return this.postStore.save({ ...post, ...checkPost(title, body), editedAt: new Date().toISOString() });
  }

  /** Authors and moderators can delete a post. Its comments go with it. */
  async deletePost(postId: string): Promise<void> {
    if (this.apiMode) {
      const courseId = this.courseOfPost(postId);
      await this.api.delete(`posts/${postId}`);
      this.forgetComments(postId);
      await this.refresh(courseId);
      return;
    }
    const post = this.findPost(postId);
    if (!(this.isAuthor(post) && this.canRead(post.courseId)) && !this.canModerate(post.courseId)) {
      throw new PermissionDeniedError();
    }
    for (const c of this.comments().filter((x) => x.postId === postId)) await this.commentStore.remove(c.id);
    await this.postStore.remove(postId);
  }

  /** Comments on a post, or replies when `parentId` is given. A reply to a reply joins the same thread. */
  async addComment(postId: string, body: string, parentId: string | null = null): Promise<Comment> {
    if (this.apiMode) {
      const courseId = this.courseOfPost(postId);
      const comment = fromCommentDto(
        await this.api.post<CommentDto>(`posts/${postId}/comments`, { body, parentId: parentId ? Number(parentId) : null }),
      );
      if (!this.apiComments().has(postId)) this.apiComments.update((map) => new Map(map).set(postId, []));
      await this.refresh(courseId, postId);
      return comment;
    }
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
    if (this.apiMode) {
      const comment = fromCommentDto(await this.api.put<CommentDto>(`comments/${commentId}`, { body }));
      await this.refresh(comment.courseId, comment.postId);
      return comment;
    }
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
    if (this.apiMode) {
      const postId = this.postOfComment(commentId);
      const courseId = this.courseOfComment(commentId);
      await this.api.delete(`comments/${commentId}`);
      await this.refresh(courseId, postId);
      return;
    }
    const comment = this.findComment(commentId);
    if (!(this.isAuthor(comment) && this.canRead(comment.courseId)) && !this.canModerate(comment.courseId)) {
      throw new PermissionDeniedError();
    }
    for (const r of this.comments().filter((x) => x.parentId === commentId)) await this.commentStore.remove(r.id);
    await this.commentStore.remove(commentId);
  }

  // ── Moderation ──────────────────────────────────────────────

  async setPostFlags(postId: string, flags: Partial<Pick<Post, 'pinned' | 'locked' | 'hidden'>>): Promise<Post> {
    if (this.apiMode) {
      const post = fromPostDto(
        await this.api.patch<PostDto>(`posts/${postId}/flags`, { isPinned: flags.pinned, isLocked: flags.locked, isHidden: flags.hidden }),
      );
      await this.refresh(post.courseId, postId);
      return post;
    }
    const post = this.findPost(postId);
    if (!this.canModerate(post.courseId)) throw new PermissionDeniedError();
    return this.postStore.save({ ...post, ...flags });
  }

  async setCommentHidden(commentId: string, hidden: boolean): Promise<Comment> {
    if (this.apiMode) {
      const comment = fromCommentDto(await this.api.patch<CommentDto>(`comments/${commentId}/hidden`, { isHidden: hidden }));
      await this.refresh(comment.courseId, comment.postId);
      return comment;
    }
    const comment = this.findComment(commentId);
    if (!this.canModerate(comment.courseId)) throw new PermissionDeniedError();
    return this.commentStore.save({ ...comment, hidden });
  }

  /** Clean-up when a course is deleted. In API mode the server removes them with the course. */
  async removeForCourse(courseId: number): Promise<void> {
    this.permissions.assert('courses.delete', courseId);
    if (this.apiMode) {
      this.apiPosts.update((map) => {
        const next = new Map(map);
        next.delete(courseId);
        return next;
      });
      this.apiModerationPosts.update((list) => list.filter((p) => p.courseId !== courseId));
      this.apiModerationComments.update((list) => list.filter((c) => c.courseId !== courseId));
      return;
    }
    for (const c of this.comments().filter((x) => x.courseId === courseId)) await this.commentStore.remove(c.id);
    for (const p of this.posts().filter((x) => x.courseId === courseId)) await this.postStore.remove(p.id);
  }

  async reset(): Promise<void> {
    // Communities in API mode live on the server; demo reset does not touch them.
    if (this.apiMode) return;
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
