import { Injectable, inject } from '@angular/core';
import { PaymentService } from '../payments/payment.service';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { ToastService } from './toast.service';

/** Coordinates start-up and reset across every browser-stored collection. */
@Injectable({ providedIn: 'root' })
export class DemoDataService {
  private readonly courses = inject(CourseService);
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly payments = inject(PaymentService);
  private readonly toast = inject(ToastService);

  /** Waits for all data, so route guards and pages can read it synchronously. */
  async load(): Promise<void> {
    await Promise.all([this.courses.ready, this.auth.ready, this.enrollments.ready, this.payments.ready]);

    const recovered =
      this.courses.recovered() || this.auth.recovered() || this.enrollments.recovered() || this.payments.recovered();
    if (recovered) {
      this.toast.error('Some saved demo data could not be read, so the defaults were restored.');
    }
  }

  async reset(): Promise<void> {
    await Promise.all([this.courses.reset(), this.auth.reset(), this.enrollments.reset(), this.payments.reset()]);
  }
}
