import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** Shared two-panel layout for the login and register pages. */
@Component({
  selector: 'app-auth-shell',
  imports: [TranslatePipe, RouterLink],
  template: `
    <section class="auth">
      <div class="container">
        <div class="panel ca-card">
          <aside class="side" aria-hidden="true">
            <a routerLink="/" class="brand" tabindex="-1">
              <span class="logo-mark">CA</span> Course Academy
            </a>
            <h2>{{ tagline() | t }}</h2>
            <ul>
              <li><i class="bi bi-check-circle-fill"></i>{{ 'Lifetime access to your courses' | t }}</li>
              <li><i class="bi bi-check-circle-fill"></i>{{ 'Project-based lessons in Bangla' | t }}</li>
              <li><i class="bi bi-check-circle-fill"></i>{{ 'Certificates and career opportunities' | t }}</li>
            </ul>
            <p class="foot">{{ 'Learn → Build Skills → Work → Earn → Grow' | t }}</p>
          </aside>
          <div class="main">
            <div class="demo"><i class="bi bi-info-circle" aria-hidden="true"></i>{{ 'Demo only — no real account is created and no data leaves your browser.' | t }}</div>
            <ng-content />
          </div>
        </div>
      </div>
    </section>
  `,
  styleUrl: './auth-shell.scss',
})
export class AuthShell {
  readonly tagline = input('Start building real skills today.');
}
