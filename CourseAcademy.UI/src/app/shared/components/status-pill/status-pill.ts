import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';

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
  imports: [TranslatePipe],
  template: `<span [class]="'tag text-capitalize ' + tone()">{{ status() | t }}</span>`,
})
export class StatusPill {
  readonly status = input.required<string>();
  protected readonly tone = computed(() => TONES[this.status()] ?? '');
}
