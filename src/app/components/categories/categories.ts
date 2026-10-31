import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CourseService } from '../../core/services/course.service';

@Component({
  selector: 'app-categories',
  imports: [RouterLink],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class Categories {
  private readonly courses = inject(CourseService);

  protected readonly categories = this.courses.categories.map((cat, i) => ({
    ...cat,
    count: this.courses.countByCategory(cat.id),
    tone: ['orange', 'blue', 'navy'][i],
  }));
}
