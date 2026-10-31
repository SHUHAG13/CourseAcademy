import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Course } from '../../../core/models/course.model';
import { CourseService } from '../../../core/services/course.service';
import { BdtPipe } from '../../pipes/bdt.pipe';
import { CourseCover } from '../course-cover/course-cover';
import { RatingStars } from '../rating-stars/rating-stars';

@Component({
  selector: 'app-course-card',
  imports: [RouterLink, DecimalPipe, BdtPipe, CourseCover, RatingStars],
  templateUrl: './course-card.html',
  styleUrl: './course-card.scss',
})
export class CourseCard {
  private readonly courses = inject(CourseService);

  readonly course = input.required<Course>();

  protected readonly link = computed(() => ['/courses', this.course().slug]);
  protected readonly categoryName = computed(() => this.courses.category(this.course().category).name);
  protected readonly instructor = computed(() => this.courses.instructor(this.course().instructorId));
  protected readonly discount = computed(() => {
    const c = this.course();
    return Math.round((1 - c.price / c.originalPrice) * 100);
  });
}
