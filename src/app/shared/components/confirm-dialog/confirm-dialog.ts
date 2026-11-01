import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { ConfirmService } from '../../../core/services/confirm.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-confirm-dialog',
  imports: [TranslatePipe],
  template: `
    <dialog #dialog class="confirm" aria-labelledby="confirm-title" (cancel)="confirm.answer(false)">
      @if (confirm.request(); as r) {
        <h2 id="confirm-title" class="h5">{{ r.title | t }}</h2>
        <p>{{ r.message | t }}</p>
        <div class="actions">
          <button type="button" class="btn btn-outline" (click)="confirm.answer(false)">{{ 'Go back' | t }}</button>
          <button type="button" class="btn" [class]="r.danger ? 'btn-danger' : 'btn-primary'" (click)="confirm.answer(true)">
            {{ r.confirmLabel | t }}
          </button>
        </div>
      }
    </dialog>
  `,
  styles: `
    .confirm {
      width: min(460px, calc(100vw - 2rem));
      padding: 1.5rem;
      border: 1px solid var(--ca-border);
      border-radius: var(--ca-radius-lg);
      box-shadow: var(--ca-shadow-lg);
    }
    .confirm::backdrop { background: rgba(15, 23, 42, 0.5); }
    p { color: var(--ca-muted); }
    .actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 0.5rem; margin-top: 1.25rem; }
  `,
})
export class ConfirmDialog {
  protected readonly confirm = inject(ConfirmService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const el = this.dialog().nativeElement;
      if (this.confirm.request()) {
        if (!el.open) el.showModal();
      } else if (el.open) {
        el.close();
      }
    });
  }
}
