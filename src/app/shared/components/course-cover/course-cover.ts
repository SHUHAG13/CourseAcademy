import { Component, computed, inject, input } from '@angular/core';
import { Course } from '../../../core/models/course.model';
import { CourseService } from '../../../core/services/course.service';

/**
 * Course thumbnail. Courses without a photo get a generated cover, rendered in
 * HTML/CSS so every course stays branded without stock photography.
 */
@Component({
  selector: 'app-course-cover',
  templateUrl: './course-cover.html',
  styleUrl: './course-cover.scss',
  host: {
    role: 'img',
    '[attr.aria-label]': 'course().title',
    '[class.cat-digital-marketing]': `course().category === 'digital-marketing'`,
    '[class.cat-sports-journalism]': `course().category === 'sports-journalism'`,
    '[class.cat-software-development]': `course().category === 'software-development'`,
    '[class.has-photo]': 'course().image',
  },
})
export class CourseCover {
  private readonly courses = inject(CourseService);

  readonly course = input.required<Course>();
  readonly showPlay = input(false);

  protected readonly categoryName = computed(() => this.courses.category(this.course().category).shortName);
}
