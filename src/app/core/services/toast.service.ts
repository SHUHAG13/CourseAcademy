import { Injectable, inject, signal } from '@angular/core';
import { LanguageService } from '../i18n/language.service';

export type ToastType = 'success' | 'info' | 'error';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly language = inject(LanguageService);
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  /** `message` is English text. It is translated here, with `params` filling its `{0}` placeholders. */
  show(message: string, type: ToastType = 'info', params?: readonly unknown[], durationMs = 4000): void {
    const id = this.nextId++;
    this.toasts.update((list) => [...list, { id, type, message: this.language.t(message, params) }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  success(message: string, params?: readonly unknown[]): void {
    this.show(message, 'success', params);
  }

  error(message: string, params?: readonly unknown[]): void {
    this.show(message, 'error', params);
  }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
