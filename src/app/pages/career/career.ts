import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OPPORTUNITIES, Opportunity } from '../../core/data/site.data';
import { ToastService } from '../../core/services/toast.service';
import { BD_PHONE_PATTERN, URL_PATTERN, showError } from '../../shared/forms/validators';

type Team = Opportunity['team'] | 'All';

@Component({
  selector: 'app-career',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './career.html',
  styleUrl: './career.scss',
})
export class Career {
  private readonly toast = inject(ToastService);

  protected readonly teams: Team[] = ['All', 'Sports Media', 'Digital Platform', 'Software Projects'];
  protected readonly team = signal<Team>('All');
  protected readonly roles = computed(() =>
    this.team() === 'All' ? OPPORTUNITIES : OPPORTUNITIES.filter((o) => o.team === this.team()),
  );
  protected readonly allRoles = OPPORTUNITIES;
  protected readonly submitting = signal(false);

  protected readonly steps = [
    { icon: 'bi-mortarboard', title: 'Complete a course', text: 'Finish your course and its final project.' },
    { icon: 'bi-folder2-open', title: 'Build your portfolio', text: 'Show your best edits, articles or code.' },
    { icon: 'bi-send', title: 'Apply', text: 'Apply for an open role with your portfolio link.' },
    { icon: 'bi-people', title: 'Join the team', text: 'Selected students work with our teams — paid.' },
  ];

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.pattern(BD_PHONE_PATTERN)]],
    role: ['', Validators.required],
    portfolio: ['', Validators.pattern(URL_PATTERN)],
    message: ['', [Validators.maxLength(600)]],
  });

  protected readonly showError = (name: string) => showError(this.form, name);

  protected apply(role: Opportunity): void {
    this.form.controls.role.setValue(role.role);
    document.getElementById('apply')?.scrollIntoView({ behavior: 'smooth' });
    document.getElementById('a-name')?.focus({ preventScroll: true });
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('Please fix the highlighted fields.');
      return;
    }
    this.submitting.set(true);
    await new Promise((r) => setTimeout(r, 900));
    this.submitting.set(false);
    this.form.reset();
    this.toast.success('Application received (demo). In the live version our team will contact you.');
  }
}
