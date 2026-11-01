import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { COURSE_FORMATS, Course, CourseFormat } from '../../core/models/course.model';
import { ConfirmService } from '../../core/services/confirm.service';
import { CourseService, discountPercent, lessonCount } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';

@Component({
  selector: 'app-admin-courses',
  imports: [FormsModule, RouterLink, BdtPipe, EmptyState, FormatBadge, StatusPill],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
      <h1 class="h3 mb-0">Courses</h1>
      <a routerLink="/admin/courses/new" class="btn btn-primary">
        <i class="bi bi-plus-lg" aria-hidden="true"></i> New course
      </a>
    </div>

    <div class="row g-2 mb-3">
      <div class="col-md-8">
        <label for="course-q" class="visually-hidden">Search courses</label>
        <input id="course-q" type="search" class="form-control" placeholder="Search by title"
          [ngModel]="search()" (ngModelChange)="search.set($event)" />
      </div>
      <div class="col-md-4">
        <label for="course-format" class="visually-hidden">Filter by format</label>
        <select id="course-format" class="form-select" [ngModel]="format()" (ngModelChange)="format.set($event)">
          <option value="all">All formats</option>
          @for (f of formats; track f) {
            <option [value]="f">{{ f }}</option>
          }
        </select>
      </div>
    </div>

    @if (filtered().length) {
      <div class="ca-card table-responsive">
        <table class="ca-table">
          <thead>
            <tr>
              <th scope="col">Course</th><th scope="col">Format</th><th scope="col">Price</th>
              <th scope="col">Content</th><th scope="col">Enrolled</th><th scope="col">Status</th>
              <th scope="col"><span class="visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            @for (c of filtered(); track c.id) {
              <tr>
                <td>
                  <a class="primary" [routerLink]="['/admin/courses', c.id]">{{ c.title }}</a>
                  <span class="secondary">{{ instructorName(c) }}</span>
                </td>
                <td><app-format-badge [format]="c.format" /></td>
                <td class="text-nowrap">
                  {{ c.price | bdt }}
                  @if (discount(c) > 0) {
                    <span class="secondary">{{ discount(c) }}% off {{ c.originalPrice | bdt }}</span>
                  }
                </td>
                <td class="text-nowrap">
                  {{ lessons(c) }} lessons
                  <span class="secondary">{{ c.schedule.length }} live · {{ c.materials.length }} materials</span>
                </td>
                <td>{{ enrolledCount(c) }}</td>
                <td><app-status-pill [status]="c.published ? 'published' : 'draft'" /></td>
                <td class="actions">
                  <button type="button" class="icon-btn" (click)="togglePublished(c)"
                    [title]="c.published ? 'Unpublish' : 'Publish'"
                    [attr.aria-label]="(c.published ? 'Unpublish ' : 'Publish ') + c.title">
                    <i class="bi" [class]="c.published ? 'bi-eye-slash' : 'bi-eye'" aria-hidden="true"></i>
                  </button>
                  <a class="icon-btn ms-1" [routerLink]="['/courses', c.slug]" title="View page" [attr.aria-label]="'View ' + c.title">
                    <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i>
                  </a>
                  <a class="icon-btn ms-1" [routerLink]="['/admin/courses', c.id]" title="Edit" [attr.aria-label]="'Edit ' + c.title">
                    <i class="bi bi-pencil" aria-hidden="true"></i>
                  </a>
                  <button type="button" class="icon-btn danger ms-1" (click)="remove(c)" title="Delete" [attr.aria-label]="'Delete ' + c.title">
                    <i class="bi bi-trash" aria-hidden="true"></i>
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else {
      <app-empty-state icon="bi-journal-x" title="No courses found"
        message="Change the search or filter, or create a new course." />
    }
  `,
})
export class AdminCourses {
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly formats = COURSE_FORMATS;
  protected readonly search = signal('');
  protected readonly format = signal<CourseFormat | 'all'>('all');

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.courses
      .courses()
      .filter((c) => (this.format() === 'all' || c.format === this.format()) && (!term || c.title.toLowerCase().includes(term)));
  });

  protected instructorName(course: Course): string {
    return this.courses.instructor(course.instructorId)?.name ?? 'No instructor';
  }

  protected discount(course: Course): number {
    return discountPercent(course);
  }

  protected lessons(course: Course): number {
    return lessonCount(course);
  }

  protected enrolledCount(course: Course): number {
    return this.enrollments.countForCourse(course.id);
  }

  protected async togglePublished(course: Course): Promise<void> {
    try {
      await this.courses.saveCourse({ ...course, published: !course.published });
      this.toast.success(`${course.title} is now ${course.published ? 'unpublished' : 'published'}.`);
    } catch {
      this.toast.error('The change could not be saved. Browser storage may be full or blocked.');
    }
  }

  protected async remove(course: Course): Promise<void> {
    const count = this.enrollments.countAllForCourse(course.id);
    const confirmed = await this.confirm.ask({
      title: `Delete "${course.title}"?`,
      message:
        (count ? `This also removes ${count} enrollment${count === 1 ? '' : 's'}. ` : '') +
        'Payment records are kept. This cannot be undone, except by resetting all demo data.',
      confirmLabel: 'Delete course',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await this.enrollments.removeForCourse(course.id);
      await this.courses.removeCourse(course.id);
      this.toast.success(`${course.title} was deleted.`);
    } catch {
      this.toast.error('The course could not be deleted. Browser storage may be full or blocked.');
    }
  }
}
