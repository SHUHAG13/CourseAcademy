import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';
import { CourseService } from '../../core/services/course.service';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.scss',
})
export class Footer {
  protected readonly categories = inject(CourseService).categories;
  protected readonly contact = CONTACT;
  protected readonly year = new Date().getFullYear();
}
