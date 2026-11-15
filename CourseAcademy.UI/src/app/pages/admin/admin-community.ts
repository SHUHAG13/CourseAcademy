import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Comment, Post } from '../../core/models/community.model';
import { CommunityService } from '../../core/services/community.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { CourseService } from '../../core/services/course.service';
import { failureMessage } from '../../core/services/permission.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { LocalDatePipe, TranslatePipe } from '../../shared/pipes/translate.pipe';

type StatusFilter = 'all' | 'visible' | 'hidden' | 'pinned' | 'locked';

/** Moderation across every course community the user may moderate. */
@Component({
  selector: 'app-admin-community',
  imports: [TranslatePipe, LocalDatePipe, FormsModule, RouterLink, EmptyState],
  template: `
    <h1 class="h3 mb-3">{{ 'Community' | t }}</h1>

    <div class="row g-2 mb-3">
      <div class="col-md-5">
        <label for="com-q" class="visually-hidden">{{ 'Search posts' | t }}</label>
        <input id="com-q" type="search" class="form-control" [placeholder]="'Search by title, text or author' | t"
          [ngModel]="search()" (ngModelChange)="search.set($event)" />
      </div>
      <div class="col-md-4">
        <label for="com-course" class="visually-hidden">{{ 'Filter by course' | t }}</label>
        <select id="com-course" class="form-select" [ngModel]="courseId()" (ngModelChange)="courseId.set(+$event)">
          <option [ngValue]="0">{{ 'All courses' | t }}</option>
          @for (c of moderated(); track c.id) {
            <option [ngValue]="c.id">{{ c.title | t }}</option>
          }
        </select>
      </div>
      <div class="col-md-3">
        <label for="com-status" class="visually-hidden">{{ 'Filter by status' | t }}</label>
        <select id="com-status" class="form-select" [ngModel]="status()" (ngModelChange)="status.set($event)">
          @for (s of statuses; track s.id) {
            <option [value]="s.id">{{ s.label | t }}</option>
          }
        </select>
      </div>
    </div>

    <h2 class="h6 mb-2">{{ 'Posts' | t }}</h2>
    @if (posts().length) {
      <div class="ca-card table-responsive mb-4">
        <table class="ca-table">
          <thead>
            <tr>
              <th scope="col">{{ 'Post' | t }}</th><th scope="col">{{ 'Course' | t }}</th><th scope="col">{{ 'Comments' | t }}</th>
              <th scope="col">{{ 'Status' | t }}</th><th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
            </tr>
          </thead>
          <tbody>
            @for (p of posts(); track p.id) {
              <tr>
                <td>
                  <span class="primary">{{ p.title }}</span>
                  <span class="secondary">{{ community.authorName(p.authorId) | t }} · {{ p.createdAt | ldate: 'd MMM y' }}</span>
                </td>
                <td>{{ courseTitle(p.courseId) | t }}</td>
                <td>{{ community.commentCount(p) }}</td>
                <td class="text-nowrap">
                  @if (p.pinned) { <span class="tag me-1">{{ 'Pinned' | t }}</span> }
                  @if (p.locked) { <span class="tag me-1">{{ 'Locked' | t }}</span> }
                  @if (p.hidden) { <span class="tag red">{{ 'Hidden' | t }}</span> } @else if (!p.pinned && !p.locked) {
                    <span class="tag">{{ 'Visible' | t }}</span>
                  }
                </td>
                <td class="actions text-nowrap">
                  <a class="icon-btn" [routerLink]="['/courses', courseSlug(p.courseId)]" fragment="community"
                    [title]="'Open discussion' | t" [attr.aria-label]="'Open {0}' | t: [p.title]">
                    <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i>
                  </a>
                  <button type="button" class="icon-btn ms-1" (click)="flag(p, 'pinned')" [title]="(p.pinned ? 'Unpin' : 'Pin') | t"
                    [attr.aria-label]="(p.pinned ? 'Unpin {0}' : 'Pin {0}') | t: [p.title]">
                    <i class="bi" [class]="p.pinned ? 'bi-pin-angle-fill' : 'bi-pin-angle'" aria-hidden="true"></i>
                  </button>
                  <button type="button" class="icon-btn ms-1" (click)="flag(p, 'locked')" [title]="(p.locked ? 'Unlock' : 'Lock') | t"
                    [attr.aria-label]="(p.locked ? 'Unlock {0}' : 'Lock {0}') | t: [p.title]">
                    <i class="bi" [class]="p.locked ? 'bi-unlock' : 'bi-lock'" aria-hidden="true"></i>
                  </button>
                  <button type="button" class="icon-btn ms-1" (click)="flag(p, 'hidden')" [title]="(p.hidden ? 'Unhide' : 'Hide') | t"
                    [attr.aria-label]="(p.hidden ? 'Unhide {0}' : 'Hide {0}') | t: [p.title]">
                    <i class="bi" [class]="p.hidden ? 'bi-eye' : 'bi-eye-slash'" aria-hidden="true"></i>
                  </button>
                  <button type="button" class="icon-btn danger ms-1" (click)="removePost(p)" [title]="'Delete' | t"
                    [attr.aria-label]="'Delete {0}' | t: [p.title]">
                    <i class="bi bi-trash" aria-hidden="true"></i>
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else {
      <div class="mb-4">
        <app-empty-state icon="bi-chat-square" title="No posts found" message="Change the search or filters." />
      </div>
    }

    <h2 class="h6 mb-2">{{ 'Recent comments' | t }}</h2>
    <div class="ca-card table-responsive">
      <table class="ca-table">
        <thead>
          <tr>
            <th scope="col">{{ 'Comment' | t }}</th><th scope="col">{{ 'On post' | t }}</th><th scope="col">{{ 'Status' | t }}</th>
            <th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
          </tr>
        </thead>
        <tbody>
          @for (c of comments(); track c.id) {
            <tr>
              <td>
                <span class="primary excerpt">{{ c.body }}</span>
                <span class="secondary">{{ community.authorName(c.authorId) | t }} · {{ c.createdAt | ldate: 'd MMM y, h:mm a' }}</span>
              </td>
              <td>{{ postTitle(c.postId) }}</td>
              <td>
                @if (c.hidden) { <span class="tag red">{{ 'Hidden' | t }}</span> } @else { <span class="tag">{{ 'Visible' | t }}</span> }
              </td>
              <td class="actions text-nowrap">
                <button type="button" class="icon-btn" (click)="toggleComment(c)" [title]="(c.hidden ? 'Unhide' : 'Hide') | t"
                  [attr.aria-label]="(c.hidden ? 'Unhide comment' : 'Hide comment') | t">
                  <i class="bi" [class]="c.hidden ? 'bi-eye' : 'bi-eye-slash'" aria-hidden="true"></i>
                </button>
                <button type="button" class="icon-btn danger ms-1" (click)="removeComment(c)" [title]="'Delete' | t" [attr.aria-label]="'Delete comment' | t">
                  <i class="bi bi-trash" aria-hidden="true"></i>
                </button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="4" class="text-center text-muted py-4">{{ 'No comments match your filters.' | t }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: `
    .excerpt { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; font-weight: 500; }
  `,
})
export class AdminCommunity {
  protected readonly community = inject(CommunityService);
  private readonly courses = inject(CourseService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly statuses: { id: StatusFilter; label: string }[] = [
    { id: 'all', label: 'All statuses' },
    { id: 'visible', label: 'Visible' },
    { id: 'hidden', label: 'Hidden' },
    { id: 'pinned', label: 'Pinned' },
    { id: 'locked', label: 'Locked' },
  ];
  protected readonly search = signal('');
  protected readonly courseId = signal(0);
  protected readonly status = signal<StatusFilter>('all');

  /** Courses whose community this user may moderate. */
  protected readonly moderated = computed(() => this.courses.courses().filter((c) => this.community.canModerate(c.id)));

  private inScope(courseId: number): boolean {
    return this.community.canModerate(courseId) && (!this.courseId() || this.courseId() === courseId);
  }

  protected readonly posts = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    return this.community
      .posts()
      .filter((p) => this.inScope(p.courseId))
      .filter(
        (p) =>
          status === 'all' ||
          (status === 'visible' && !p.hidden) ||
          (status === 'hidden' && p.hidden) ||
          (status === 'pinned' && p.pinned) ||
          (status === 'locked' && p.locked),
      )
      .filter(
        (p) =>
          !term ||
          p.title.toLowerCase().includes(term) ||
          p.body.toLowerCase().includes(term) ||
          this.community.authorName(p.authorId).toLowerCase().includes(term),
      )
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  });

  protected readonly comments = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.status();
    return this.community
      .comments()
      .filter((c) => this.inScope(c.courseId))
      .filter((c) => status === 'all' || (status === 'hidden' ? c.hidden : status === 'visible' ? !c.hidden : true))
      .filter((c) => !term || c.body.toLowerCase().includes(term) || this.community.authorName(c.authorId).toLowerCase().includes(term))
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
      .slice(0, 20);
  });

  protected courseTitle(id: number): string {
    return this.courses.byId(id)?.title ?? 'Deleted course';
  }

  protected courseSlug(id: number): string {
    return this.courses.byId(id)?.slug ?? '';
  }

  protected postTitle(id: string): string {
    return this.community.posts().find((p) => p.id === id)?.title ?? '';
  }

  protected async flag(post: Post, flag: 'pinned' | 'locked' | 'hidden'): Promise<void> {
    await this.run(() => this.community.setPostFlags(post.id, { [flag]: !post[flag] }), 'The post was updated.');
  }

  protected async toggleComment(comment: Comment): Promise<void> {
    await this.run(() => this.community.setCommentHidden(comment.id, !comment.hidden), 'The comment was updated.');
  }

  protected async removePost(post: Post): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete this post?',
      message: 'The post and all its comments are removed for everyone.',
      confirmLabel: 'Delete post',
      danger: true,
    });
    if (confirmed) await this.run(() => this.community.deletePost(post.id), 'The post was deleted.');
  }

  protected async removeComment(comment: Comment): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Delete this comment?',
      message: comment.parentId ? 'The reply is removed for everyone.' : 'The comment and its replies are removed for everyone.',
      confirmLabel: 'Delete comment',
      danger: true,
    });
    if (confirmed) await this.run(() => this.community.deleteComment(comment.id), 'The comment was deleted.');
  }

  private async run(action: () => Promise<unknown>, success: string): Promise<void> {
    try {
      await action();
      this.toast.success(success);
    } catch (error) {
      this.toast.error(failureMessage(error, (error as Error).message));
    }
  }
}
