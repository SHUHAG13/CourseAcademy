import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Course, LiveSession } from '../../core/models/course.model';
import { PaymentMethodId, paymentMethod } from '../../core/payments/payment.model';
import { PaymentService } from '../../core/payments/payment.service';
import { Certificate } from '../../core/models/certificate.model';
import { AuthService } from '../../core/services/auth.service';
import { downloadCertificatePdf } from '../../core/services/certificate-pdf';
import { CertificateService } from '../../core/services/certificate.service';
import { CourseService, sectionCount } from '../../core/services/course.service';
import { ProgressService } from '../../core/services/progress.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { TranslatePipe, LocalNumberPipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-dashboard',
  imports: [LocalNumberPipe, TranslatePipe, LocalDatePipe, RouterLink, BdtPipe, CourseCover, EmptyState, FormatBadge, StatusPill],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly payments = inject(PaymentService);
  protected readonly auth = inject(AuthService);
  protected readonly progress = inject(ProgressService);
  protected readonly certificates = inject(CertificateService);
  private readonly toast = inject(ToastService);

  protected readonly transactions = this.payments.mine;

  /** Enrollments paired with their course. Records for deleted courses are dropped. */
  protected readonly enrolled = computed(() =>
    this.enrollments.mine().flatMap((enrollment) => {
      const course = this.courses.byId(enrollment.courseId);
      return course ? [{ enrollment, course }] : [];
    }),
  );
  protected readonly activeCount = computed(() => this.enrolled().filter((e) => e.enrollment.status === 'active').length);

  protected readonly upcoming = computed(() => {
    const now = Date.now();
    const sessions: { session: LiveSession; course: Course }[] = [];
    for (const { enrollment, course } of this.enrolled()) {
      if (enrollment.status !== 'active') continue;
      for (const session of course.schedule) {
        if (new Date(session.startsAt).getTime() + session.durationMinutes * 60_000 >= now) {
          sessions.push({ session, course });
        }
      }
    }
    return sessions.sort((a, b) => a.session.startsAt.localeCompare(b.session.startsAt)).slice(0, 5);
  });

  protected sections(course: Course): number {
    return sectionCount(course);
  }

  protected methodName(id: PaymentMethodId): string {
    return paymentMethod(id).name;
  }

  protected async download(certificate: Certificate): Promise<void> {
    try {
      await downloadCertificatePdf(certificate, `${location.origin}/verify/${certificate.code}`);
    } catch {
      this.toast.error('The PDF could not be created. Please try again.');
    }
  }
}
