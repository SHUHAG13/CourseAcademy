import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';
import { CourseService } from '../../core/services/course.service';
import { TranslatePipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-footer',
  imports: [LocalDatePipe, TranslatePipe, RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.scss',
})
export class Footer {
  protected readonly categories = inject(CourseService).categories;
  protected readonly contact = CONTACT;
  protected readonly today = new Date();
}
