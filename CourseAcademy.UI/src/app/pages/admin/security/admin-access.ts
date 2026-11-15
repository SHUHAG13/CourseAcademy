import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ADMIN_ROLE_ID } from '../../../core/models/permission.model';
import { AppUser } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { PermissionService } from '../../../core/services/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

/** Every user with their role and whether they have their own screen access. */
@Component({
  selector: 'app-admin-access',
  imports: [TranslatePipe, FormsModule, RouterLink, Avatar],
  template: `
    <div class="mb-3">
      <label for="access-q" class="visually-hidden">{{ 'Search users' | t }}</label>
      <input id="access-q" type="search" class="form-control" [placeholder]="'Search by name or email' | t"
        [ngModel]="search()" (ngModelChange)="search.set($event)" />
    </div>

    <div class="ca-card table-responsive">
      <table class="ca-table">
        <thead>
          <tr>
            <th scope="col">{{ 'Name' | t }}</th><th scope="col">{{ 'Role' | t }}</th>
            <th scope="col">{{ 'Screen access' | t }}</th><th scope="col">{{ 'Course access' | t }}</th>
            <th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
          </tr>
        </thead>
        <tbody>
          @for (u of users(); track u.id) {
            <tr>
              <td>
                <span class="d-flex align-items-center gap-2 primary"><app-avatar [name]="u.name" [size]="32" />{{ u.name | t }}</span>
                <span class="secondary">{{ u.email }}</span>
              </td>
              <td>
                @if (permissions.can('roles.edit') && u.id !== auth.user()?.id) {
                  <label class="visually-hidden" [for]="'role-' + u.id">{{ 'Role for {0}' | t: [u.name] }}</label>
                  <select class="form-select form-select-sm" [id]="'role-' + u.id" [ngModel]="u.role"
                    (ngModelChange)="changeRole(u, $event)">
                    @for (r of permissions.roles(); track r.id) {
                      <option [value]="r.id">{{ r.name | t }}</option>
                    }
                  </select>
                } @else {
                  {{ (permissions.role(u.role)?.name ?? u.role) | t }}
                }
              </td>
              <td>
                <span class="tag" [class.blue]="accessLabel(u) !== 'Role defaults'">{{ accessLabel(u) | t }}</span>
              </td>
              <td>{{ '{0} course(s)' | t: [permissions.grantsFor(u.id).length] }}</td>
              <td class="actions">
                <a class="btn btn-sm btn-outline" [routerLink]="['/admin/security/access', u.id]">
                  {{ (permissions.can('access.edit') ? 'Manage' : 'View') | t }}
                </a>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-center text-muted py-4">{{ 'No users match your search.' | t }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class AdminAccess {
  protected readonly auth = inject(AuthService);
  protected readonly permissions = inject(PermissionService);
  private readonly toast = inject(ToastService);

  protected readonly search = signal('');
  protected readonly users = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.auth.users().filter((u) => !term || u.name.toLowerCase().includes(term) || u.email.toLowerCase().includes(term));
  });

  protected accessLabel(user: AppUser): string {
    if (user.role === ADMIN_ROLE_ID) return 'Full access';
    return user.access ? 'Custom' : 'Role defaults';
  }

  protected async changeRole(user: AppUser, roleId: string): Promise<void> {
    try {
      await this.permissions.setUserRole(user.id, roleId);
      this.toast.success('{0} is now {1}.', [user.name, this.permissions.role(roleId)?.name ?? roleId]);
    } catch (error) {
      this.toast.error((error as Error).message);
    }
  }
}
