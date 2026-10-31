import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';
import { CourseService } from '../../core/services/course.service';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { WhatsappCta } from '../whatsapp-cta/whatsapp-cta';

/**
 * Enrollment & payment instructions. Payments are handled manually
 * (bKash / Nagad / bank) — there is no online checkout yet.
 */
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

  protected readonly selected = computed(() => this.courses.all.find((c) => c.slug === this.course()));

  protected readonly whatsappLink = computed(() => {
    const c = this.selected();
    const text = c
      ? `Hi! I'd like to enroll in "${c.title}" (${c.price} BDT). Please share the payment details.`
      : `Hi! I'd like to enroll in a course. Please share the payment details.`;
    return `https://wa.me/${CONTACT.whatsapp}?text=${encodeURIComponent(text)}`;
  });

  protected readonly methods = [
    { name: 'bKash', logo: '/logos/bkash.jpg', text: 'Send Money or Payment from your bKash account.' },
    { name: 'Nagad', logo: '/logos/nagad.png', text: 'Pay instantly from your Nagad wallet.' },
    { name: 'Bank Transfer', logo: '/logos/bank.jpg', text: 'Transfer from any Bangladeshi bank account.' },
  ];

  protected readonly steps = [
    { title: 'Contact us', text: 'Message us on WhatsApp with the course you want. We reply with payment details.' },
    { title: 'Complete payment', text: 'Pay the course fee with bKash, Nagad or bank transfer.' },
    { title: 'Send the transaction ID', text: 'Share the transaction ID or a screenshot so we can verify it.' },
    { title: 'Start learning', text: 'Course access is activated after verification, usually within a few hours.' },
  ];
}
