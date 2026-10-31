import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CourseService } from '../../core/services/course.service';
import { FinalCta } from '../../components/final-cta/final-cta';
import { LearningJourney } from '../../components/learning-journey/learning-journey';

@Component({
  selector: 'app-about',
  imports: [RouterLink, FinalCta, LearningJourney],
  templateUrl: './about.html',
  styleUrl: './about.scss',
})
export class About {
  protected readonly categories = inject(CourseService).categories;

  protected readonly program = [
    {
      phase: 'Months 1–2',
      title: 'Skill Foundation',
      text: 'Learn the core tools and fundamentals of your track — editing software, sports reporting basics or programming — with hands-on practice every week.',
    },
    {
      phase: 'Months 3–4',
      title: 'Professional Mastery',
      text: 'Move into real-world projects: client-style edits, live match coverage or full applications — and build a portfolio you can show.',
    },
  ];
}
