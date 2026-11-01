import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Course } from '../../../core/models/course.model';
import { CourseService, discountPercent } from '../../../core/services/course.service';
import { BdtPipe } from '../../pipes/bdt.pipe';
import { CourseCover } from '../course-cover/course-cover';
import { FormatBadge } from '../format-badge/format-badge';
import { RatingStars } from '../rating-stars/rating-stars';
import { TranslatePipe, LocalNumberPipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-course-card',
  imports: [TranslatePipe, LocalNumberPipe, RouterLink, BdtPipe, CourseCover, FormatBadge, RatingStars],
  templateUrl: './course-card.html',
  styleUrl: './course-card.scss',
})
export class CourseCard {
  private readonly courses = inject(CourseService);

  readonly course = input.required<Course>();

  protected readonly link = computed(() => ['/courses', this.course().slug]);
  protected readonly categoryName = computed(() => this.courses.category(this.course().category).name);
  protected readonly instructor = computed(() => this.courses.instructor(this.course().instructorId));
  protected readonly discount = computed(() => discountPercent(this.course()));
}
