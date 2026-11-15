import { Component, input } from '@angular/core';
import { TESTIMONIALS } from '../../core/data/site.data';
import { Avatar } from '../../shared/components/avatar/avatar';
import { RatingStars } from '../../shared/components/rating-stars/rating-stars';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-testimonials',
  imports: [TranslatePipe, Avatar, RatingStars],
  templateUrl: './testimonials.html',
  styleUrl: './testimonials.scss',
})
export class Testimonials {
  /** True on the standalone /testimonials route, where this section is the page. */
  readonly asPage = input(false);

  protected readonly testimonials = TESTIMONIALS;
}
