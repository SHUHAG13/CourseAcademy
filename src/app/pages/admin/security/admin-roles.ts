import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SCREENS } from '../../../core/data/screens.data';
import { LanguageService } from '../../../core/i18n/language.service';
import { ADMIN_ROLE_ID, Permission, Role } from '../../../core/models/permission.model';
import { AuthService } from '../../../core/services/auth.service';
import { ConfirmService } from '../../../core/services/confirm.service';
import { PermissionService } from '../../../core/services/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { AccessMatrix } from '../../../shared/components/access-matrix/access-matrix';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

/** Create roles and choose what each one may do. */
@Component({
  selector: 'app-admin-roles',
  imports: [TranslatePipe, FormsModule, AccessMatrix],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
      <p class="text-muted mb-0">{{ 'A role\\'s permissions apply to every course. Per-course access is set under User access.' | t }}</p>
      @if (!editing() && canEdit()) {
        <button type="button" class="btn btn-primary" (click)="startCreate()">
          <i class="bi bi-plus-lg" aria-hidden="true"></i> {{ 'New role' | t }}
        </button>
      }
    </div>

    @if (editing(); as role) {
      <form class="panel ca-card mb-3" (ngSubmit)="save()" novalidate>
        <h2 class="panel-title">{{ (isNew() ? 'New role' : canEdit() ? 'Edit role' : role.name) | t }}</h2>
        <div class="row g-3 mb-3">
          <div class="col-md-5">
            <label for="r-name" class="form-label">{{ 'Name' | t }}</label>
            <input id="r-name" name="name" class="form-control" [(ngModel)]="name" required [disabled]="isAdminRole(role) || !canEdit()" />
          </div>
          <div class="col-md-7">
            <label for="r-desc" class="form-label">{{ 'Description' | t }}</label>
            <input id="r-desc" name="description" class="form-control" [(ngModel)]="description" [disabled]="!canEdit()" />
          </div>
        </div>

        @if (isAdminRole(role)) {
          <p class="small text-muted">{{ 'The Admin role always has every permission.' | t }}</p>
        } @else {
          <p class="small text-muted">{{ 'Users with this role get these screens unless an admin sets their own access.' | t }}</p>
          <app-access-matrix [(value)]="selected" [readonly]="!canEdit()" />
        }

        <div class="d-flex justify-content-end gap-2 mt-3">
          <button type="button" class="btn btn-outline" (click)="close()">{{ 'Cancel' | t }}</button>
          @if (canEdit()) {
            <button type="submit" class="btn btn-primary" [disabled]="saving()">{{ 'Save role' | t }}</button>
          }
        </div>
      </form>
    }

    <div class="row row-cols-1 row-cols-xl-2 g-3">
      @for (r of permissions.roles(); track r.id) {
        <div class="col">
          <article class="panel ca-card h-100">
            <div class="d-flex gap-3 align-items-start">
              <div class="flex-grow-1 min-w-0">
                <h2 class="h6 mb-1">
                  {{ r.name | t }}
                  @if (r.system) {
                    <span class="tag ms-1">{{ 'Built-in' | t }}</span>
                  }
                </h2>
                <p class="small text-muted mb-1">{{ r.description | t }}</p>
                <p class="small text-muted mb-0">
                  {{ '{0} screens · {1} users' | t: [screenCount(r), memberCount(r.id)] }}
                </p>
              </div>
              <div class="d-flex gap-1">
                <button type="button" class="icon-btn" (click)="startEdit(r)" [attr.aria-label]="(canEdit() ? 'Edit {0}' : 'View {0}') | t: [r.name]"
                  [title]="(canEdit() ? 'Edit' : 'View') | t">
                  <i class="bi" [class]="canEdit() ? 'bi-pencil' : 'bi-eye'" aria-hidden="true"></i>
                </button>
                @if (!r.system && canEdit()) {
                  <button type="button" class="icon-btn danger" (click)="remove(r)" [attr.aria-label]="'Delete {0}' | t: [r.name]" [title]="'Delete' | t">
                    <i class="bi bi-trash" aria-hidden="true"></i>
                  </button>
                }
              </div>
            </div>
          </article>
        </div>
      }
    </div>
  `,
})
export class AdminRoles {
  protected readonly permissions = inject(PermissionService);
  private readonly auth = inject(AuthService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly language = inject(LanguageService);

  protected readonly canEdit = computed(() => this.permissions.can('roles.edit'));
  protected readonly editing = signal<Role | null>(null);
  protected readonly isNew = signal(false);
  protected readonly selected = signal<Permission[]>([]);
  protected readonly saving = signal(false);
  protected name = '';
  protected description = '';

  protected isAdminRole(role: Role): boolean {
    return role.id === ADMIN_ROLE_ID;
  }

  protected screenCount(role: Role): number {
    return SCREENS.filter((s) => role.permissions.includes(s.view)).length;
  }

  protected memberCount(roleId: string): number {
    return this.auth.users().filter((u) => u.role === roleId).length;
  }

  protected startCreate(): void {
    this.open({ id: this.permissions.newRoleId(), name: '', description: '', permissions: [], system: false }, true);
  }

  protected startEdit(role: Role): void {
    this.open(role, false);
  }

  protected close(): void {
    this.editing.set(null);
  }

  protected async save(): Promise<void> {
    const role = this.editing();
    if (!role) return;
    this.saving.set(true);
    try {
      const saved = await this.permissions.saveRole({
        ...role,
        name: this.name,
        description: this.description,
        permissions: this.selected(),
      });
      this.toast.success('{0} was saved.', [saved.name]);
      this.close();
    } catch (error) {
      this.toast.error((error as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  protected async remove(role: Role): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: this.language.t('Delete {0}?', [role.name]),
      message: 'This cannot be undone, except by resetting all demo data.',
      confirmLabel: 'Delete role',
      danger: true,
    });
    if (!confirmed) return;
    try {
      await this.permissions.removeRole(role.id);
      this.toast.success('{0} was deleted.', [role.name]);
    } catch (error) {
      this.toast.error((error as Error).message);
    }
  }

  private open(role: Role, isNew: boolean): void {
    this.editing.set(role);
    this.isNew.set(isNew);
    this.name = role.name;
    this.description = role.description;
    this.selected.set(role.permissions);
  }
}
