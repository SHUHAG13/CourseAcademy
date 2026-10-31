import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import { WhatsappCta } from '../../components/whatsapp-cta/whatsapp-cta';
import { BD_PHONE_PATTERN, showError } from '../../shared/forms/validators';

@Component({
  selector: 'app-contact',
  imports: [ReactiveFormsModule, RouterLink, WhatsappCta],
  templateUrl: './contact.html',
  styleUrl: './contact.scss',
})
export class Contact {
  private readonly toast = inject(ToastService);

  protected readonly contact = CONTACT;
  protected readonly courses = inject(CourseService).all;
  protected readonly sending = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.pattern(BD_PHONE_PATTERN)]],
    topic: ['', Validators.required],
    message: ['', [Validators.required, Validators.minLength(10)]],
  });

  protected readonly showError = (name: string) => showError(this.form, name);

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('Please fix the highlighted fields.');
      return;
    }

    this.sending.set(true);
    // No backend yet — simulate the request.
    await new Promise((r) => setTimeout(r, 800));
    this.sending.set(false);
    this.form.reset();
    this.toast.success('Thanks! This is a demo form, so the message was not sent. Please contact us on WhatsApp.');
  }
}
