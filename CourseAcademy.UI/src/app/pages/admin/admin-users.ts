import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { STUDENT_ROLE_ID } from '../../core/models/permission.model';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { PermissionService } from '../../core/services/permission.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { TranslatePipe, LocalNumberPipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';

/** Read-only list of demo accounts. Passwords are never displayed. */
@Component({
  selector: 'app-admin-users',
  imports: [LocalNumberPipe, TranslatePipe, LocalDatePipe, FormsModule, Avatar],
  template: `
    <h1 class="h3 mb-3">{{ 'Users' | t }}</h1>

    <div class="mb-3">
      <label for="user-q" class="visually-hidden">{{ 'Search users' | t }}</label>
      <input id="user-q" type="search" class="form-control" [placeholder]="'Search by name or email' | t"
        [ngModel]="search()" (ngModelChange)="search.set($event)" />
    </div>

    <div class="ca-card table-responsive">
      <table class="ca-table">
        <thead>
          <tr>
            <th scope="col">{{ 'Name' | t }}</th><th scope="col">{{ 'Email' | t }}</th><th scope="col">{{ 'Role' | t }}</th>
            <th scope="col">{{ 'Active courses' | t }}</th><th scope="col">{{ 'Joined' | t }}</th>
          </tr>
        </thead>
        <tbody>
          @for (u of users(); track u.id) {
            <tr>
              <td>
                <span class="d-flex align-items-center gap-2 primary"><app-avatar [name]="u.name" [size]="32" />{{ u.name | t }}</span>
              </td>
              <td>{{ u.email }}</td>
              <td><span class="tag" [class.blue]="u.role !== studentRole">{{ roleName(u.role) | t }}</span></td>
              <td>{{ activeCourses(u.id) | n }}</td>
              <td class="text-nowrap">{{ u.createdAt | ldate: 'd MMM y' }}</td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-center text-muted py-4">{{ 'No users match your search.' | t }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class AdminUsers {
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);

  protected readonly studentRole = STUDENT_ROLE_ID;

  protected readonly search = signal('');
  protected readonly users = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.auth.users().filter((u) => !term || u.name.toLowerCase().includes(term) || u.email.toLowerCase().includes(term));
  });

  protected roleName(roleId: string): string {
    return this.permissions.role(roleId)?.name ?? roleId;
  }

  protected activeCourses(userId: string): number {
    return this.enrollments.enrollments().filter((e) => e.userId === userId && e.status === 'active').length;
  }
}
