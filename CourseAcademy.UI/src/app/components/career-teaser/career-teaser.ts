import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OPPORTUNITIES } from '../../core/data/site.data';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-career-teaser',
  imports: [TranslatePipe, RouterLink],
  templateUrl: './career-teaser.html',
  styleUrl: './career-teaser.scss',
})
export class CareerTeaser {
  protected readonly roles = OPPORTUNITIES.slice(0, 3);
}
