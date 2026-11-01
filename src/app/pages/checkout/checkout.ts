import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PAYMENT_METHODS, PaymentMethodId } from '../../core/payments/payment.model';
import { PaymentService } from '../../core/payments/payment.service';
import { AuthService } from '../../core/services/auth.service';
import { CourseService, discountPercent } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** Step 1 of checkout: review the order and choose how to pay. */
@Component({
  selector: 'app-checkout',
  imports: [TranslatePipe, FormsModule, RouterLink, BdtPipe, CourseCover, EmptyState, FormatBadge],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout {
  private readonly courses = inject(CourseService);
  private readonly payments = inject(PaymentService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Bound from the `:slug` route param. */
  readonly slug = input.required<string>();

  protected readonly methods = PAYMENT_METHODS;
  protected readonly method = signal<PaymentMethodId>('bkash');
  protected readonly submitting = signal(false);

  protected readonly course = computed(() => {
    const c = this.courses.bySlug(this.slug());
    return c?.published ? c : undefined;
  });
  protected readonly instructor = computed(() => {
    const c = this.course();
    return c ? this.courses.instructor(c.instructorId) : undefined;
  });
  protected readonly discount = computed(() => {
    const c = this.course();
    return c ? discountPercent(c) : 0;
  });
  protected readonly alreadyEnrolled = computed(() => {
    const c = this.course();
    return !!c && this.enrollments.isEnrolled(c.id);
  });
  protected readonly isAdmin = this.auth.isAdmin;

  protected async pay(): Promise<void> {
    const course = this.course();
    const user = this.auth.user();
    if (!course || !user || this.submitting()) return;

    this.submitting.set(true);
    try {
      if (course.price === 0) {
        await this.enrollments.grant(user.id, course.id);
        this.toast.success("You're enrolled in {0}.", [course.title]);
        await this.router.navigate(['/learn', course.slug]);
      } else {
        await this.router.navigateByUrl(await this.payments.checkout(course, this.method()));
      }
    } catch {
      this.toast.error('We could not start the payment. Browser storage may be full or blocked.');
    } finally {
      this.submitting.set(false);
    }
  }
}
