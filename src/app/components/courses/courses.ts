import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategoryId } from '../../core/models/course.model';
import { CourseService } from '../../core/services/course.service';
import { CourseCard } from '../../shared/components/course-card/course-card';

/** Home page "Featured Courses" section with quick category tabs. */
@Component({
  selector: 'app-courses',
  imports: [RouterLink, CourseCard],
  templateUrl: './courses.html',
  styleUrl: './courses.scss',
})
export class Courses {
  private readonly courseService = inject(CourseService);

  protected readonly categories = this.courseService.categories;
  protected readonly active = signal<CategoryId | 'featured'>('featured');

  protected readonly courses = computed(() => {
    const tab = this.active();
    if (tab === 'featured') return this.courseService.featured();
    return this.courseService.all
      .filter((c) => c.category === tab)
      .sort((a, b) => b.students - a.students)
      .slice(0, 4);
  });
}
