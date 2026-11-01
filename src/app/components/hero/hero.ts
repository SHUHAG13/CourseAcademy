import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CourseService } from '../../core/services/course.service';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { TranslatePipe, LocalNumberPipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-hero',
  imports: [TranslatePipe, LocalNumberPipe, RouterLink, CourseCover],
  templateUrl: './hero.html',
  styleUrl: './hero.scss',
})
export class Hero {
  private readonly courses = inject(CourseService).published;

  /** Admins can delete or unpublish any course, so fall back to whatever is available. */
  protected readonly showcase = computed(
    () => this.courses().find((c) => c.category === 'sports-journalism') ?? this.courses()[0],
  );
  protected readonly courseCount = computed(() => this.courses().length);
  protected readonly learners = computed(
    () => Math.floor(this.courses().reduce((s, c) => s + c.students, 0) / 1000) * 1000,
  );
  protected readonly avgRating = computed(() => {
    const rated = this.courses().filter((c) => c.ratingCount > 0);
    return rated.length ? rated.reduce((s, c) => s + c.rating, 0) / rated.length : 0;
  });
}
