import { Injectable, inject } from '@angular/core';
import { PaymentService } from '../payments/payment.service';
import { AuthService } from './auth.service';
import { CertificateService } from './certificate.service';
import { CommunityService } from './community.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionService } from './permission.service';
import { ProgressService } from './progress.service';
import { ToastService } from './toast.service';

/** Coordinates start-up and reset across every browser-stored collection. */
@Injectable({ providedIn: 'root' })
export class DemoDataService {
  private readonly courses = inject(CourseService);
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly payments = inject(PaymentService);
  private readonly permissions = inject(PermissionService);
  private readonly progress = inject(ProgressService);
  private readonly community = inject(CommunityService);
  private readonly certificates = inject(CertificateService);
  private readonly toast = inject(ToastService);

  /** Waits for all data, so route guards and pages can read it synchronously. */
  async load(): Promise<void> {
    await Promise.all([this.courses.ready, this.auth.ready, this.enrollments.ready, this.payments.ready, this.permissions.ready, this.progress.ready, this.community.ready, this.certificates.ready]);

    const recovered =
      this.courses.recovered() ||
      this.auth.recovered() ||
      this.enrollments.recovered() ||
      this.payments.recovered() ||
      this.permissions.recovered() ||
      this.progress.recovered() ||
      this.community.recovered() ||
      this.certificates.recovered();
    if (recovered) {
      this.toast.error('Some saved demo data could not be read, so the defaults were restored.');
    }
  }

  async reset(): Promise<void> {
    this.permissions.assert('system.reset');
    await Promise.all([
      this.courses.reset(),
      this.auth.reset(),
      this.enrollments.reset(),
      this.payments.reset(),
      this.permissions.reset(),
      this.progress.reset(),
      this.community.reset(),
      this.certificates.reset(),
    ]);
  }
}
