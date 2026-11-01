import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  title: string;
  message: string;
  confirmLabel: string;
  danger?: boolean;
}

/** Promise-based confirmation, rendered once by the dialog in the app shell. */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private resolver?: (confirmed: boolean) => void;

  readonly request = signal<ConfirmRequest | null>(null);

  ask(request: ConfirmRequest): Promise<boolean> {
    this.resolver?.(false);
    this.request.set(request);
    return new Promise((resolve) => (this.resolver = resolve));
  }

  answer(confirmed: boolean): void {
    this.resolver?.(confirmed);
    this.resolver = undefined;
    this.request.set(null);
  }
}
