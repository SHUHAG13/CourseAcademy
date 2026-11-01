import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';
import { PAYMENT_METHODS } from '../../core/payments/payment.model';
import { CourseService } from '../../core/services/course.service';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { WhatsappCta } from '../whatsapp-cta/whatsapp-cta';

/** Explains how enrollment and the demo checkout work. Payments happen at `/checkout`. */
@Component({
  selector: 'app-payment-methods',
  imports: [RouterLink, BdtPipe, CourseCover, WhatsappCta],
  templateUrl: './payment-methods.html',
  styleUrl: './payment-methods.scss',
})
export class PaymentMethods {
  private readonly courses = inject(CourseService);

  /** Bound from `?course=<slug>`. */
  readonly course = input<string>();

  protected readonly selected = computed(() => this.courses.published().find((c) => c.slug === this.course()));

  protected readonly whatsappLink = computed(() => {
    const c = this.selected();
    const text = c
      ? `Hi! I have a question about enrolling in "${c.title}" (${c.price} BDT).`
      : `Hi! I have a question about enrolling in a course.`;
    return `https://wa.me/${CONTACT.whatsapp}?text=${encodeURIComponent(text)}`;
  });

  protected readonly methods = PAYMENT_METHODS;

  protected readonly steps = [
    { title: 'Choose a course', text: 'Open a course page, check the price and select Enroll Now.' },
    { title: 'Review your order', text: 'Log in, confirm the price and discount, and pick a payment method.' },
    { title: 'Complete the demo payment', text: 'Our simulated gateway lets you try a successful, failed or cancelled payment.' },
    { title: 'Start learning', text: 'After a successful payment the course opens immediately in your dashboard.' },
  ];
}
