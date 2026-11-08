import { LanguageService } from '../../core/i18n/language.service';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Enrollment } from '../../core/models/enrollment.model';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { CourseService } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { TranslatePipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';
import { PermissionService, failureMessage } from '../../core/services/permission.service';

@Component({
  selector: 'app-admin-enrollments',
  imports: [TranslatePipe, LocalDatePipe, FormsModule, StatusPill],
  template: `
    <h1 class="h3 mb-3">{{ 'Enrollments' | t }}</h1>

    <div class="row g-2 mb-3">
      <div class="col-md-8">
        <label for="enroll-q" class="visually-hidden">{{ 'Search enrollments' | t }}</label>
        <input id="enroll-q" type="search" class="form-control" [placeholder]="'Search by student or course' | t"
          [ngModel]="search()" (ngModelChange)="search.set($event)" />
      </div>
      <div class="col-md-4">
        <label for="enroll-status" class="visually-hidden">{{ 'Filter by status' | t }}</label>
        <select id="enroll-status" class="form-select" [ngModel]="status()" (ngModelChange)="status.set($event)">
          <option value="all">{{ 'All statuses' | t }}</option>
          <option value="active">{{ 'Active' | t }}</option>
          <option value="revoked">{{ 'Revoked' | t }}</option>
        </select>
      </div>
    </div>

    <div class="ca-card table-responsive">
      <table class="ca-table">
        <thead>
          <tr>
            <th scope="col">{{ 'Student' | t }}</th><th scope="col">{{ 'Course' | t }}</th><th scope="col">{{ 'Enrolled' | t }}</th>
            <th scope="col">{{ 'Payment' | t }}</th><th scope="col">{{ 'Status' | t }}</th><th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
          </tr>
        </thead>
        <tbody>
          @for (r of rows(); track r.enrollment.id) {
            <tr>
              <td><span class="primary">{{ r.student }}</span><span class="secondary">{{ r.email }}</span></td>
              <td>{{ r.course | t }}</td>
              <td class="text-nowrap">{{ r.enrollment.enrolledAt | ldate: 'd MMM y' }}</td>
              <td class="text-nowrap">{{ (r.enrollment.transactionId ?? 'Free') | t }}</td>
              <td><app-status-pill [status]="r.enrollment.status" /></td>
              <td class="actions">
                @if (permissions.can('enrollments.edit')) {
                  @if (r.enrollment.status === 'active') {
                    <button type="button" class="btn btn-sm btn-outline" (click)="revoke(r.enrollment, r.student, r.course)">{{ 'Revoke' | t }}</button>
                  } @else {
                    <button type="button" class="btn btn-sm btn-outline" (click)="restore(r.enrollment)">{{ 'Restore' | t }}</button>
                  }
                }
              </td>
            </tr>
          } @empty {
            <tr><td colspan="6" class="text-center text-muted py-4">{{ 'No enrollments match your filters.' | t }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class AdminEnrollments {
  private readonly auth = inject(AuthService);
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly language = inject(LanguageService);
  protected readonly permissions = inject(PermissionService);

  protected readonly search = signal('');
  protected readonly status = signal<'all' | Enrollment['status']>('all');

  protected readonly rows = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.enrollments
      .enrollments()
      .map((enrollment) => {
        const user = this.auth.users().find((u) => u.id === enrollment.userId);
        return {
          enrollment,
          student: user?.name ?? 'Deleted user',
          email: user?.email ?? '',
          course: this.courses.byId(enrollment.courseId)?.title ?? 'Deleted course',
        };
      })
      .filter(
        (r) =>
          (this.status() === 'all' || r.enrollment.status === this.status()) &&
          (!term || `${r.student} ${r.email} ${r.course}`.toLowerCase().includes(term)),
      )
      .sort((a, b) => b.enrollment.enrolledAt.localeCompare(a.enrollment.enrolledAt));
  });

  protected async revoke(enrollment: Enrollment, student: string, course: string): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Revoke course access?',
      message: this.language.t('{0} will lose access to {1}. The payment record is not changed. You can restore access later.', [student, course]),
      confirmLabel: 'Revoke access',
      danger: true,
    });
    if (confirmed) await this.setStatus(enrollment, 'revoked');
  }

  protected restore(enrollment: Enrollment): Promise<void> {
    return this.setStatus(enrollment, 'active');
  }

  private async setStatus(enrollment: Enrollment, status: Enrollment['status']): Promise<void> {
    try {
      await this.enrollments.setStatus(enrollment.id, status);
      this.toast.success(status === 'active' ? 'Access restored.' : 'Access revoked.');
    } catch (error) {
      this.toast.error(failureMessage(error, 'The change could not be saved. Browser storage may be full or blocked.'));
    }
  }
}
