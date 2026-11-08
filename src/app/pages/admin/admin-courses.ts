import { LanguageService } from '../../core/i18n/language.service';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { COURSE_FORMATS, Course, CourseFormat } from '../../core/models/course.model';
import { CommunityService } from '../../core/services/community.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { CourseService, discountPercent, sectionCount } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { TranslatePipe, LocalNumberPipe } from '../../shared/pipes/translate.pipe';
import { PermissionService, failureMessage } from '../../core/services/permission.service';

@Component({
  selector: 'app-admin-courses',
  imports: [LocalNumberPipe, TranslatePipe, FormsModule, RouterLink, BdtPipe, EmptyState, FormatBadge, StatusPill],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
      <h1 class="h3 mb-0">{{ 'Courses' | t }}</h1>
      @if (permissions.can('courses.create')) {
        <a routerLink="/admin/courses/new" class="btn btn-primary">
          <i class="bi bi-plus-lg" aria-hidden="true"></i> {{ 'New course' | t }}
        </a>
      }
    </div>

    <div class="row g-2 mb-3">
      <div class="col-md-8">
        <label for="course-q" class="visually-hidden">{{ 'Search courses' | t }}</label>
        <input id="course-q" type="search" class="form-control" [placeholder]="'Search by title' | t"
          [ngModel]="search()" (ngModelChange)="search.set($event)" />
      </div>
      <div class="col-md-4">
        <label for="course-format" class="visually-hidden">{{ 'Filter by format' | t }}</label>
        <select id="course-format" class="form-select" [ngModel]="format()" (ngModelChange)="format.set($event)">
          <option value="all">{{ 'All formats' | t }}</option>
          @for (f of formats; track f) {
            <option [value]="f">{{ f | t }}</option>
          }
        </select>
      </div>
    </div>

    @if (filtered().length) {
      <div class="ca-card table-responsive">
        <table class="ca-table">
          <thead>
            <tr>
              <th scope="col">{{ 'Course' | t }}</th><th scope="col">{{ 'Format' | t }}</th><th scope="col">{{ 'Price' | t }}</th>
              <th scope="col">{{ 'Content' | t }}</th><th scope="col">{{ 'Enrolled' | t }}</th><th scope="col">{{ 'Status' | t }}</th>
              <th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
            </tr>
          </thead>
          <tbody>
            @for (c of filtered(); track c.id) {
              <tr>
                <td>
                  @if (permissions.can('courses.edit', c.id)) {
                    <a class="primary" [routerLink]="['/admin/courses', c.id]">{{ c.title | t }}</a>
                  } @else {
                    <span class="primary">{{ c.title | t }}</span>
                  }
                  <span class="secondary">{{ instructorName(c) | t }}</span>
                </td>
                <td><app-format-badge [format]="c.format" /></td>
                <td class="text-nowrap">
                  {{ c.price | bdt }}
                  @if (discount(c) > 0) {
                    <span class="secondary">{{ '{0}% off {1}' | t: [discount(c), (c.originalPrice | bdt)] }}</span>
                  }
                </td>
                <td class="text-nowrap">
                  {{ '{0} sections' | t: [sections(c)] }}
                  <span class="secondary">{{ '{0} live · {1} materials' | t: [c.schedule.length, c.materials.length] }}</span>
                </td>
                <td>{{ enrolledCount(c) | n }}</td>
                <td><app-status-pill [status]="c.published ? 'published' : 'draft'" /></td>
                <td class="actions">
                  @if (permissions.can('courses.edit', c.id)) {
                    <button type="button" class="icon-btn" (click)="togglePublished(c)"
                      [title]="(c.published ? 'Unpublish' : 'Publish') | t"
                      [attr.aria-label]="(c.published ? 'Unpublish {0}' : 'Publish {0}') | t: [c.title]">
                      <i class="bi" [class]="c.published ? 'bi-eye-slash' : 'bi-eye'" aria-hidden="true"></i>
                    </button>
                  }
                  <a class="icon-btn ms-1" [routerLink]="['/courses', c.slug]" [title]="'View page' | t" [attr.aria-label]="'View {0}' | t: [c.title]">
                    <i class="bi bi-box-arrow-up-right" aria-hidden="true"></i>
                  </a>
                  @if (permissions.can('certificates.manage', c.id)) {
                    <a class="icon-btn ms-1" [routerLink]="['/admin/courses', c.id, 'certification']" [title]="'Certification' | t"
                      [attr.aria-label]="'Certification settings for {0}' | t: [c.title]">
                      <i class="bi bi-award" aria-hidden="true"></i>
                    </a>
                  }
                  @if (permissions.can('courses.edit', c.id)) {
                    <a class="icon-btn ms-1" [routerLink]="['/admin/courses', c.id, 'curriculum']" [title]="'Curriculum' | t"
                      [attr.aria-label]="'Edit the curriculum of {0}' | t: [c.title]">
                      <i class="bi bi-list-nested" aria-hidden="true"></i>
                    </a>
                    <a class="icon-btn ms-1" [routerLink]="['/admin/courses', c.id]" [title]="'Edit' | t" [attr.aria-label]="'Edit {0}' | t: [c.title]">
                      <i class="bi bi-pencil" aria-hidden="true"></i>
                    </a>
                  }
                  @if (permissions.can('courses.delete', c.id)) {
                    <button type="button" class="icon-btn danger ms-1" (click)="remove(c)" [title]="'Delete' | t" [attr.aria-label]="'Delete {0}' | t: [c.title]">
                      <i class="bi bi-trash" aria-hidden="true"></i>
                    </button>
                  }
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
  private readonly language = inject(LanguageService);
  protected readonly permissions = inject(PermissionService);
  private readonly community = inject(CommunityService);

  protected readonly formats = COURSE_FORMATS;
  protected readonly search = signal('');
  protected readonly format = signal<CourseFormat | 'all'>('all');

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.courses
      .courses()
      .filter((c) => this.permissions.can('courses.view', c.id))
      .filter((c) => (this.format() === 'all' || c.format === this.format()) && (!term || c.title.toLowerCase().includes(term)));
  });

  protected instructorName(course: Course): string {
    return this.courses.instructor(course.instructorId)?.name ?? 'No instructor';
  }

  protected discount(course: Course): number {
    return discountPercent(course);
  }

  protected sections(course: Course): number {
    return sectionCount(course);
  }

  protected enrolledCount(course: Course): number {
    return this.enrollments.countForCourse(course.id);
  }

  protected async togglePublished(course: Course): Promise<void> {
    try {
      await this.courses.saveCourse({ ...course, published: !course.published });
      this.toast.success(course.published ? '{0} is now unpublished.' : '{0} is now published.', [course.title]);
    } catch (error) {
      this.toast.error(failureMessage(error, 'The change could not be saved. Browser storage may be full or blocked.'));
    }
  }

  protected async remove(course: Course): Promise<void> {
    const count = this.enrollments.countAllForCourse(course.id);
    const confirmed = await this.confirm.ask({
      title: this.language.t('Delete "{0}"?', [course.title]),
      message: this.language.t(
        count
          ? 'This also removes {0} enrollment(s). Payment records are kept. This cannot be undone, except by resetting all demo data.'
          : 'Payment records are kept. This cannot be undone, except by resetting all demo data.',
        [count],
      ),
      confirmLabel: 'Delete course',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await this.enrollments.removeForCourse(course.id);
      await this.community.removeForCourse(course.id);
      await this.permissions.removeGrantsForCourse(course.id);
      await this.courses.removeCourse(course.id);
      this.toast.success('{0} was deleted.', [course.title]);
    } catch (error) {
      this.toast.error(failureMessage(error, 'The course could not be deleted. Browser storage may be full or blocked.'));
    }
  }
}
