import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SPORTS_STORIES } from '../../core/data/site.data';
import { TranslatePipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-sports-teaser',
  imports: [TranslatePipe, LocalDatePipe, RouterLink],
  templateUrl: './sports-teaser.html',
  styleUrl: './sports-teaser.scss',
})
export class SportsTeaser {
  protected readonly lead = SPORTS_STORIES[0];
  protected readonly stories = SPORTS_STORIES.slice(1, 5);
}
