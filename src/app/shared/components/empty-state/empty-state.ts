import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty ca-card">
      <span class="icon-tile mb-3"><i class="bi" [class]="icon()" aria-hidden="true"></i></span>
      <h3 class="h5 mb-2">{{ title() }}</h3>
      <p class="text-muted mb-3">{{ message() }}</p>
      <ng-content />
    </div>
  `,
  styles: `
    .empty {
      padding: 3rem 1.5rem;
      text-align: center;
      border-style: dashed;
      box-shadow: none;
    }
    p { max-width: 420px; margin-inline: auto; }
  `,
})
export class EmptyState {
  readonly icon = input('bi-search');
  readonly title = input.required<string>();
  readonly message = input('');
}
