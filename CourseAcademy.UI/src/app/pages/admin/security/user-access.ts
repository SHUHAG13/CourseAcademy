import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_ROLE_ID, COURSE_PERMISSIONS, PERMISSION_GROUPS, Permission } from '../../../core/models/permission.model';
import { AuthService } from '../../../core/services/auth.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { CourseService } from '../../../core/services/course.service';
import { PermissionService } from '../../../core/services/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { AccessMatrix } from '../../../shared/components/access-matrix/access-matrix';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

const PERMISSION_LABELS = new Map(PERMISSION_GROUPS.flatMap((g) => g.permissions.map((p) => [p.id, p.label] as const)));

/** One user's screen access and their extra permissions on single courses. */
@Component({
  selector: 'app-user-access',
  imports: [TranslatePipe, RouterLink, AccessMatrix, Avatar, EmptyState],
  template: `
    <a routerLink="/admin/security/access" class="small fw-semibold d-inline-block mb-3">
      <i class="bi bi-arrow-left" aria-hidden="true"></i> {{ 'All users' | t }}
    </a>

    @if (user(); as u) {
      <section class="panel ca-card mb-4" aria-labelledby="screen-access-title">
        <div class="d-flex flex-wrap align-items-center gap-3 mb-3">
          <app-avatar [name]="u.name" [size]="48" />
          <div class="flex-grow-1 min-w-0">
            <h2 id="screen-access-title" class="h5 mb-0">{{ u.name | t }}</h2>
            <p class="small text-muted mb-0">{{ u.email }} · {{ (permissions.role(u.role)?.name ?? u.role) | t }}</p>
          </div>
          <span class="tag" [class.blue]="u.access || isAdmin()">
            {{ (isAdmin() ? 'Full access' : u.access ? 'Custom' : 'Role defaults') | t }}
          </span>
        </div>

        @if (lockReason(); as reason) {
          <p class="demo-note small"><i class="bi bi-lock" aria-hidden="true"></i>{{ reason | t }}</p>
        } @else {
          <p class="small text-muted">
            {{ 'Tick the screens this user can open and what they can do there. This replaces the permissions of their role.' | t }}
          </p>
        }

        <app-access-matrix [(value)]="draft" [readonly]="!!lockReason()" />

        @if (!lockReason()) {
          <div class="d-flex flex-wrap justify-content-end gap-2">
            @if (u.access) {
              <button type="button" class="btn btn-outline" [disabled]="saving()" (click)="resetToRole()">
                <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i> {{ 'Reset to role defaults' | t }}
              </button>
            }
            <button type="button" class="btn btn-primary" [disabled]="saving()" (click)="save()">{{ 'Save access' | t }}</button>
          </div>
        }
      </section>

      <section class="panel ca-card" aria-labelledby="grant-title">
        <h2 id="grant-title" class="panel-title">{{ 'Course access' | t }}</h2>
        <p class="small text-muted">{{ 'These permissions apply only to the ticked course, on top of the screens above.' | t }}</p>
        <div class="table-responsive">
          <table class="ca-table">
            <thead>
              <tr>
                <th scope="col">{{ 'Course' | t }}</th>
                @for (p of coursePermissions; track p) {
                  <th scope="col">{{ label(p) | t }}</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (c of courses.courses(); track c.id) {
                <tr>
                  <td><span class="primary">{{ c.title | t }}</span></td>
                  @for (p of coursePermissions; track p) {
                    <td>
                      <input class="form-check-input" type="checkbox" [checked]="hasGrant(c.id, p)" [disabled]="!permissions.can('access.edit')"
                        (change)="toggleGrant(c.id, p)" [attr.aria-label]="'{0}: {1}' | t: [label(p), c.title]" />
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      </section>
    } @else {
      <app-empty-state icon="bi-person-x" title="User not found" message="This account may have been removed." />
    }
  `,
})
export class UserAccess {
  protected readonly permissions = inject(PermissionService);
  protected readonly courses = inject(CourseService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  /** Bound from the `:userId` route param. */
  readonly userId = input.required<string>();

  protected readonly coursePermissions = COURSE_PERMISSIONS;
  protected readonly saving = signal(false);
  protected readonly user = computed(() => this.auth.users().find((u) => u.id === this.userId()));
  protected readonly isAdmin = computed(() => this.user()?.role === ADMIN_ROLE_ID);

  /** The ticks being edited. Starts from what the user has now, and follows saved changes. */
  protected readonly draft = linkedSignal<Permission[]>(() => this.permissions.permissionsOf(this.user() ?? null));

  /** Why the matrix can't be edited, if it can't. The service enforces the same rules. */
  protected readonly lockReason = computed(() => {
    if (this.isAdmin()) return 'Admin users always have full access.';
    if (this.user()?.id === this.auth.user()?.id) return "You can't change your own access.";
    if (!this.permissions.can('access.edit')) return 'You can view this access but not change it.';
    return null;
  });

  protected label(permission: Permission): string {
    return PERMISSION_LABELS.get(permission) ?? permission;
  }

  protected hasGrant(courseId: number, permission: Permission): boolean {
    return this.permissions.grantsFor(this.userId()).some((g) => g.courseId === courseId && g.permissions.includes(permission));
  }

  protected async save(): Promise<void> {
    const u = this.user();
    if (!u) return;
    this.saving.set(true);
    try {
      await this.permissions.setUserAccess(u.id, this.draft());
      this.toast.success('Access for {0} was saved.', [u.name]);
    } catch (error) {
      this.toast.error((error as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  protected async resetToRole(): Promise<void> {
    const u = this.user();
    if (!u) return;
    const confirmed = await this.confirm.ask({
      title: 'Reset to role defaults?',
      message: "The user's own screen access is removed and they get their role's permissions again.",
      confirmLabel: 'Reset access',
    });
    if (!confirmed) return;
    this.saving.set(true);
    try {
      await this.permissions.resetUserAccess(u.id);
      this.toast.success('{0} now has the permissions of their role.', [u.name]);
    } catch (error) {
      this.toast.error((error as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  protected async toggleGrant(courseId: number, permission: Permission): Promise<void> {
    const current = this.permissions.grantsFor(this.userId()).find((g) => g.courseId === courseId)?.permissions ?? [];
    const next = current.includes(permission) ? current.filter((p) => p !== permission) : [...current, permission];
    try {
      await this.permissions.setCourseGrant(this.userId(), courseId, next);
    } catch (error) {
      this.toast.error((error as Error).message);
    }
  }
}
