import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CourseService } from '../../core/services/course.service';
import { CourseCover } from '../../shared/components/course-cover/course-cover';

@Component({
  selector: 'app-hero',
  imports: [RouterLink, DecimalPipe, CourseCover],
  templateUrl: './hero.html',
  styleUrl: './hero.scss',
})
export class Hero {
  private readonly courses = inject(CourseService);

  protected readonly showcase = this.courses.all.find((c) => c.slug === 'sports-journalism-masterclass')!;
  protected readonly courseCount = this.courses.all.length;
  protected readonly learners = Math.floor(this.courses.all.reduce((s, c) => s + c.students, 0) / 1000) * 1000;
  protected readonly avgRating =
    this.courses.all.reduce((s, c) => s + c.rating, 0) / this.courses.all.length;
}
