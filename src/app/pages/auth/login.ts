import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DEMO_ADMIN_CREDENTIALS, DEMO_STUDENT_CREDENTIALS } from '../../core/data/demo-records.data';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { showError } from '../../shared/forms/validators';
import { AuthShell } from './auth-shell';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-login',
  imports: [TranslatePipe, ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './login.html',
  styleUrl: './auth-form.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Bound from `?returnUrl=`, set when a guard sent the user here. */
  readonly returnUrl = input<string>();

  protected readonly loading = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly error = signal('');

  protected readonly demoAccounts = [
    { label: 'Admin', ...DEMO_ADMIN_CREDENTIALS },
    { label: 'Student', ...DEMO_STUDENT_CREDENTIALS },
  ];

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    remember: [true],
  });

  protected readonly showError = (name: string) => showError(this.form, name);

  protected useDemoAccount(email: string, password: string): void {
    this.form.patchValue({ email, password });
    this.error.set('');
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set('');
    const { email, password, remember } = this.form.getRawValue();
    try {
      const user = await this.auth.login(email, password, remember);
      this.toast.success('Welcome back, {0}!', [user.name]);
      this.router.navigateByUrl(this.returnUrl() || this.auth.homeUrl());
    } catch (error) {
      this.error.set((error as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  protected forgotPassword(): void {
    this.toast.show('Password reset will be available once accounts are connected to a server.');
  }
}
