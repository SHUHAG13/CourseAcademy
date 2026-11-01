import { Component, computed, input } from '@angular/core';

const TONES: Record<string, string> = {
  success: 'green',
  active: 'green',
  published: 'green',
  pending: 'orange',
  draft: 'orange',
  failed: 'red',
  revoked: 'red',
};

/** Coloured label for payment, enrollment and publishing states. */
@Component({
  selector: 'app-status-pill',
  template: `<span [class]="'tag text-capitalize ' + tone()">{{ status() }}</span>`,
})
export class StatusPill {
  readonly status = input.required<string>();
  protected readonly tone = computed(() => TONES[this.status()] ?? '');
}
