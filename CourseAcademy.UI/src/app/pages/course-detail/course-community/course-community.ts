import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { COMMENT_MAX, Comment, POST_BODY_MAX, POST_TITLE_MAX, Post } from '../../../core/models/community.model';
import { Course } from '../../../core/models/course.model';
import { AuthService } from '../../../core/services/auth.service';
import { CommunityRole, CommunityService } from '../../../core/services/community.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { failureMessage } from '../../../core/services/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { LocalDatePipe, TranslatePipe } from '../../../shared/pipes/translate.pipe';

const ROLE_LABELS: Record<CommunityRole, string> = { admin: 'Admin', instructor: 'Instructor', student: 'Student' };

/** A course's discussion board, shown at the bottom of the course page. */
@Component({
  selector: 'app-course-community',
  imports: [TranslatePipe, LocalDatePipe, FormsModule, RouterLink, Avatar, NgTemplateOutlet],
  templateUrl: './course-community.html',
  styleUrl: './course-community.scss',
})
export class CourseCommunity {
  protected readonly community = inject(CommunityService);
  protected readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly course = input.required<Course>();

  protected readonly limits = { title: POST_TITLE_MAX, body: POST_BODY_MAX, comment: COMMENT_MAX };
  protected readonly roleLabels = ROLE_LABELS;

  protected readonly canRead = computed(() => this.community.canRead(this.course().id));
  protected readonly canPost = computed(() => this.community.canPost(this.course().id));
  protected readonly canModerate = computed(() => this.community.canModerate(this.course().id));
  protected readonly posts = computed(() => this.community.postsFor(this.course().id));
  protected readonly loginUrl = computed(() => `/courses/${this.course().slug}`);

  protected readonly composing = signal(false);
  protected readonly busy = signal(false);
  /** Posts whose comments are shown. */
  protected readonly open = signal(new Set<string>());
  protected readonly editingPost = signal<string | null>(null);
  protected readonly editingComment = signal<string | null>(null);
  protected readonly replyingTo = signal<string | null>(null);

  // Draft text, bound with ngModel.
  protected newTitle = '';
  protected newBody = '';
  protected editTitle = '';
  protected editBody = '';
  protected replyBody = '';
  protected readonly commentDrafts: Record<string, string> = {};

  protected roleOf(userId: string): CommunityRole {
    return this.community.roleIn(userId, this.course().id);
  }

  protected isOpen(post: Post): boolean {
    return this.open().has(post.id);
  }

  protected toggleOpen(post: Post): void {
    this.open.update((set) => {
      const next = new Set(set);
      if (!next.delete(post.id)) next.add(post.id);
      return next;
    });
  }

  // ── Posts ───────────────────────────────────────────────────

  protected async createPost(): Promise<void> {
    await this.run(async () => {
      const post = await this.community.createPost(this.course().id, this.newTitle, this.newBody);
      this.newTitle = '';
      this.newBody = '';
      this.composing.set(false);
      this.open.update((set) => new Set(set).add(post.id));
      this.toast.success('Your post was published.');
    });
  }

  protected startEditPost(post: Post): void {
    this.editingPost.set(post.id);
    this.editTitle = post.title;
    this.editBody = post.body;
  }

  protected async saveEditPost(post: Post): Promise<void> {
    await this.run(async () => {
      await this.community.editPost(post.id, this.editTitle, this.editBody);
      this.editingPost.set(null);
    });
  }

  protected async deletePost(post: Post): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete this post?',
      message: 'The post and all its comments are removed for everyone.',
      confirmLabel: 'Delete post',
      danger: true,
    });
    if (confirmed) await this.run(() => this.community.deletePost(post.id), 'The post was deleted.');
  }

  protected async setFlag(post: Post, flag: 'pinned' | 'locked' | 'hidden'): Promise<void> {
    const on = !post[flag];
    const messages = {
      pinned: on ? 'The post is pinned to the top.' : 'The post is no longer pinned.',
      locked: on ? 'The discussion is locked.' : 'The discussion is open again.',
      hidden: on ? 'The post is hidden from students.' : 'The post is visible again.',
    };
    await this.run(() => this.community.setPostFlags(post.id, { [flag]: on }), messages[flag]);
  }

  // ── Comments ────────────────────────────────────────────────

  protected async addComment(post: Post): Promise<void> {
    await this.run(async () => {
      await this.community.addComment(post.id, this.commentDrafts[post.id] ?? '');
      this.commentDrafts[post.id] = '';
    });
  }

  protected startReply(comment: Comment): void {
    this.replyingTo.set(this.replyingTo() === comment.id ? null : comment.id);
    this.replyBody = '';
  }

  protected async sendReply(comment: Comment): Promise<void> {
    await this.run(async () => {
      await this.community.addComment(comment.postId, this.replyBody, comment.id);
      this.replyingTo.set(null);
      this.replyBody = '';
    });
  }

  protected startEditComment(comment: Comment): void {
    this.editingComment.set(comment.id);
    this.editBody = comment.body;
  }

  protected async saveEditComment(comment: Comment): Promise<void> {
    await this.run(async () => {
      await this.community.editComment(comment.id, this.editBody);
      this.editingComment.set(null);
    });
  }

  protected async deleteComment(comment: Comment): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete this comment?',
      message: comment.parentId ? 'The reply is removed for everyone.' : 'The comment and its replies are removed for everyone.',
      confirmLabel: 'Delete comment',
      danger: true,
    });
    if (confirmed) await this.run(() => this.community.deleteComment(comment.id), 'The comment was deleted.');
  }

  protected async toggleCommentHidden(comment: Comment): Promise<void> {
    await this.run(
      () => this.community.setCommentHidden(comment.id, !comment.hidden),
      comment.hidden ? 'The comment is visible again.' : 'The comment is hidden from students.',
    );
  }

  /** Runs a change, showing the service's message if it is refused. */
  private async run(action: () => Promise<unknown>, success?: string): Promise<void> {
    this.busy.set(true);
    try {
      await action();
      if (success) this.toast.success(success);
    } catch (error) {
      this.toast.error(failureMessage(error, (error as Error).message));
    } finally {
      this.busy.set(false);
    }
  }
}
