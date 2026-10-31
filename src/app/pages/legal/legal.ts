import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';

interface LegalDoc {
  title: string;
  intro: string;
  sections: { heading: string; body: string }[];
}

// Placeholder policy text — have it reviewed before launch.
const DOCS: Record<'privacy' | 'terms', LegalDoc> = {
  privacy: {
    title: 'Privacy Policy',
    intro: 'How Course Academy collects, uses and protects your information.',
    sections: [
      {
        heading: 'Information we collect',
        body: 'When you contact us, enroll or create an account we collect your name, email address, phone number and the courses you are interested in or enrolled in.',
      },
      {
        heading: 'How we use it',
        body: 'We use your information to give you course access, verify payments, provide student support and, if you agree, send updates about new courses.',
      },
      {
        heading: 'Payments',
        body: 'Payments are made directly through bKash, Nagad or your bank. We only keep the transaction ID needed to verify your enrollment — never your PIN or password.',
      },
      {
        heading: 'Sharing',
        body: 'We do not sell your personal information. We share it only with service providers needed to run the platform, or when required by law.',
      },
      {
        heading: 'Your choices',
        body: 'You can ask us to update or delete your information at any time by contacting us.',
      },
    ],
  },
  terms: {
    title: 'Terms & Conditions',
    intro: 'The rules for using Course Academy and our courses.',
    sections: [
      {
        heading: 'Course access',
        body: 'After your payment is verified you receive personal, lifetime access to the course videos you enrolled in. Accounts may not be shared.',
      },
      {
        heading: 'Payments and refunds',
        body: 'Course fees are shown in Bangladeshi Taka (BDT). If you are unhappy with a course, contact us within 7 days of enrollment to discuss a refund.',
      },
      {
        heading: 'Course content',
        body: 'All videos, files and materials belong to Course Academy and its instructors. Downloading, recording or redistributing them is not allowed.',
      },
      {
        heading: 'Career opportunities',
        body: 'Work opportunities are offered at our discretion based on skills and portfolio. Completing a course does not guarantee employment.',
      },
      {
        heading: 'Changes',
        body: 'We may update these terms. Significant changes will be announced on the website.',
      },
    ],
  },
};

@Component({
  selector: 'app-legal',
  imports: [RouterLink],
  template: `
    <section class="page-hero">
      <div class="container">
        <ol class="breadcrumb-ca" aria-label="Breadcrumb">
          <li><a routerLink="/">Home</a></li>
          <li aria-current="page">{{ doc().title }}</li>
        </ol>
        <h1>{{ doc().title }}</h1>
        <p>{{ doc().intro }}</p>
      </div>
    </section>
    <section class="section-tight">
      <div class="container">
        <article class="ca-card doc">
          <p class="tag mb-4">Last updated: October 2026</p>
          @for (s of doc().sections; track s.heading; let i = $index) {
            <h2 class="h5">{{ i + 1 }}. {{ s.heading }}</h2>
            <p>{{ s.body }}</p>
          }
          <h2 class="h5">Contact</h2>
          <p class="mb-0">Questions? Email <a [href]="'mailto:' + email">{{ email }}</a> or visit our <a routerLink="/contact">contact page</a>.</p>
        </article>
      </div>
    </section>
  `,
  styles: `
    .doc { max-width: 820px; padding: clamp(1.5rem, 4vw, 3rem); }
    h2 { margin-top: 1.75rem; }
    h2:first-of-type { margin-top: 0; }
  `,
})
export class Legal {
  /** Bound from the route's `data.page`. */
  readonly page = input<'privacy' | 'terms'>('privacy');

  protected readonly doc = computed(() => DOCS[this.page()]);
  protected readonly email = CONTACT.email;
}
