import { Component, computed, input } from '@angular/core';

const TONES = ['#14213D', '#2563EB', '#EA580C', '#334155', '#1D4ED8'];

/** Initials avatar — used instead of stock photos for people in demo content. */
@Component({
  selector: 'app-avatar',
  template: `<span class="avatar" [style.width.px]="size()" [style.height.px]="size()"
    [style.font-size.px]="size() * 0.38" [style.background]="tone()" aria-hidden="true">{{ initials() }}</span>`,
  styles: `
    .avatar {
      display: inline-flex;
      flex-shrink: 0;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      color: #fff;
      font-family: var(--ca-font-display);
      font-weight: 700;
      letter-spacing: 0.02em;
    }
  `,
})
export class Avatar {
  readonly name = input.required<string>();
  readonly size = input(44);

  protected readonly initials = computed(() =>
    this.name()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0].toUpperCase())
      .join(''),
  );

  protected readonly tone = computed(() => {
    const hash = [...this.name()].reduce((sum, ch) => sum + ch.charCodeAt(0), 0);
    return TONES[hash % TONES.length];
  });
}
