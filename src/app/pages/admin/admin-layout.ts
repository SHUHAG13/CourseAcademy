import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-admin-layout',
  imports: [TranslatePipe, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="admin container-fluid">
      <nav class="side" [attr.aria-label]="'Admin' | t">
        <p class="side-title">{{ 'Admin dashboard' | t }}</p>
        <ul>
          @for (link of links; track link.path) {
            <li>
              <a [routerLink]="link.path" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: !!link.exact }">
                <i class="bi" [class]="link.icon" aria-hidden="true"></i>{{ link.label | t }}
              </a>
            </li>
          }
        </ul>
      </nav>
      <div class="content">
        <div class="demo-note mb-4">
          <i class="bi bi-info-circle" aria-hidden="true"></i>
          <p>
            {{ 'Frontend demo: this data lives only in this browser. It is not secure and is not shared with other devices or users.' | t }}
          </p>
        </div>
        <router-outlet />
      </div>
    </div>
  `,
  styles: `
    .admin {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      max-width: 1500px;
      padding-block: 1.25rem 3rem;
    }
    .side-title {
      margin: 0 0.6rem 0.5rem;
      font-size: 0.72rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--ca-muted);
    }
    ul { display: flex; gap: 0.25rem; margin: 0 0 1.25rem; padding: 0 0 0.25rem; overflow-x: auto; list-style: none; }
    a {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.55rem 0.7rem;
      border-radius: var(--ca-radius-sm);
      font-size: 0.92rem;
      font-weight: 500;
      white-space: nowrap;
      color: var(--ca-text-2);
    }
    a:hover { background: var(--ca-surface-muted); }
    a.active { background: var(--ca-blue-50); color: var(--ca-blue-600); font-weight: 600; }
    @media (min-width: 992px) {
      .admin { grid-template-columns: 220px minmax(0, 1fr); gap: 2rem; padding-top: 2rem; }
      .side { position: sticky; top: calc(var(--ca-nav-height) + 24px); align-self: start; }
      ul { flex-direction: column; overflow: visible; }
    }
  `,
})
export class AdminLayout {
  protected readonly links = [
    { label: 'Overview', path: '/admin', icon: 'bi-speedometer2', exact: true },
    { label: 'Courses', path: '/admin/courses', icon: 'bi-journal-text' },
    { label: 'Instructors', path: '/admin/instructors', icon: 'bi-person-video3' },
    { label: 'Users', path: '/admin/users', icon: 'bi-people' },
    { label: 'Enrollments', path: '/admin/enrollments', icon: 'bi-card-checklist' },
    { label: 'Transactions', path: '/admin/transactions', icon: 'bi-credit-card' },
  ];
}
