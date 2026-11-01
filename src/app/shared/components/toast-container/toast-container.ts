import { Component, inject } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

const ICONS = {
  success: 'bi-check-circle-fill',
  info: 'bi-info-circle-fill',
  error: 'bi-exclamation-circle-fill',
} as const;

@Component({
  selector: 'app-toast-container',
  imports: [TranslatePipe],
  template: `
    <div class="toast-stack" role="status" aria-live="polite">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="ca-toast" [class]="'ca-toast ' + toast.type">
          <i class="bi" [class]="icons[toast.type]" aria-hidden="true"></i>
          <span class="msg">{{ toast.message }}</span>
          <button type="button" class="close" (click)="toastService.dismiss(toast.id)" [attr.aria-label]="'Dismiss' | t">
            <i class="bi bi-x-lg" aria-hidden="true"></i>
          </button>
        </div>
      }
    </div>
  `,
  styleUrl: './toast-container.scss',
})
export class ToastContainer {
  protected readonly toastService = inject(ToastService);
  protected readonly icons = ICONS;
}
