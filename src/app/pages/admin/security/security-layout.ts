import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SCREENS } from '../../../core/data/screens.data';
import { PermissionService } from '../../../core/services/permission.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

/** The admin Security area: roles, per-user access and, later, the audit log. */
@Component({
  selector: 'app-security-layout',
  imports: [TranslatePipe, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <h1 class="h3 mb-3">{{ 'Security' | t }}</h1>
    <nav class="tabs mb-4" [attr.aria-label]="'Security' | t">
      @for (tab of tabs(); track tab.path) {
        <a [routerLink]="tab.path" routerLinkActive="active" ariaCurrentWhenActive="page">
          <i class="bi" [class]="tab.icon" aria-hidden="true"></i>{{ tab.label | t }}
        </a>
      }
    </nav>
    <router-outlet />
  `,
  styles: `
    .tabs { display: flex; gap: 0.25rem; overflow-x: auto; border-bottom: 1px solid var(--ca-border); }
    a {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.6rem 0.9rem;
      margin-bottom: -1px;
      border-bottom: 2px solid transparent;
      font-size: 0.92rem;
      font-weight: 500;
      white-space: nowrap;
      color: var(--ca-text-2);
    }
    a:hover { color: var(--ca-blue-600); }
    a.active { border-bottom-color: var(--ca-blue-600); color: var(--ca-blue-600); font-weight: 600; }
  `,
})
export class SecurityLayout {
  private readonly permissions = inject(PermissionService);

  protected readonly tabs = computed(() => SCREENS.filter((s) => s.section === 'security' && this.permissions.canView(s)));
}
