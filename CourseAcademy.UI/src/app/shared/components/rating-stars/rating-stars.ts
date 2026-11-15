import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-rating-stars',
  imports: [TranslatePipe],
  template: `
    <span class="stars" role="img" [attr.aria-label]="'Rated {0} out of 5' | t: [rating()]">
      @for (star of stars(); track $index) {
        <i class="bi" [class]="star" aria-hidden="true"></i>
      }
    </span>
  `,
  styles: `
    .stars {
      display: inline-flex;
      gap: 1px;
      color: var(--ca-star);
      font-size: 0.85em;
      line-height: 1;
    }
  `,
})
export class RatingStars {
  readonly rating = input.required<number>();

  protected readonly stars = computed(() =>
    [1, 2, 3, 4, 5].map((i) => {
      const r = this.rating();
      if (r >= i - 0.25) return 'bi-star-fill';
      if (r >= i - 0.75) return 'bi-star-half';
      return 'bi-star';
    }),
  );
}
