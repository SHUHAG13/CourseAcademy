import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-not-found',
  imports: [TranslatePipe, FormsModule, RouterLink],
  template: `
    <section class="section nf">
      <div class="container text-center">
        <span class="code">404</span>
        <h1 class="h2 mt-3">{{ 'We couldn\\'t find that page' | t }}</h1>
        <p class="text-muted mx-auto mb-4">{{ 'The link may be broken or the page may have moved. Try searching for a course instead.' | t }}</p>

        <form class="search mx-auto mb-4" role="search" (submit)="$event.preventDefault(); search()">
          <label for="nf-search" class="visually-hidden">{{ 'Search courses' | t }}</label>
          <div class="input-icon">
            <i class="bi bi-search" aria-hidden="true"></i>
            <input id="nf-search" type="search" class="form-control" [placeholder]="'Search courses' | t" name="q"
              [ngModel]="query()" (ngModelChange)="query.set($event)" />
          </div>
          <button type="submit" class="btn btn-primary">{{ 'Search' | t }}</button>
        </form>

        <div class="d-flex justify-content-center flex-wrap gap-2">
          <a routerLink="/" class="btn btn-outline">{{ 'Go to homepage' | t }}</a>
          <a routerLink="/courses" class="btn btn-accent">{{ 'Browse courses' | t }}</a>
        </div>
      </div>
    </section>
  `,
  styles: `
    .nf { min-height: 60vh; display: flex; align-items: center; }
    .code {
      font-family: var(--ca-font-display);
      font-size: clamp(4rem, 3rem + 6vw, 7rem);
      font-weight: 800;
      line-height: 1;
      color: var(--ca-navy);
      letter-spacing: -0.04em;
      text-shadow: 6px 6px 0 var(--ca-orange-50);
    }
    p { max-width: 460px; }
    .search { display: flex; gap: 0.5rem; max-width: 460px; }
    .search .input-icon { flex: 1; }
  `,
})
export class NotFound {
  private readonly router = inject(Router);
  protected readonly query = signal('');

  protected search(): void {
    const q = this.query().trim();
    this.router.navigate(['/courses'], { queryParams: q ? { q } : {} });
  }
}
