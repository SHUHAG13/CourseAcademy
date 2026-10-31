import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { BD_PHONE_PATTERN, PASSWORD_PATTERN, matchFields, showError } from '../../shared/forms/validators';
import { AuthShell } from './auth-shell';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './register.html',
  styleUrl: './auth-form.scss',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly showPassword = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(BD_PHONE_PATTERN)]],
      password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      confirm: ['', Validators.required],
      terms: [false, Validators.requiredTrue],
    },
    { validators: matchFields('password', 'confirm') },
  );

  /** 0–4 score: length ≥ 8, letters, numbers, symbol/upper-case. */
  protected readonly strength = toSignal(
    this.form.controls.password.valueChanges.pipe(
      map((pw) =>
        [pw.length >= 8, /[a-z]/i.test(pw), /\d/.test(pw), /[^a-z0-9]|[A-Z]/.test(pw)].filter(Boolean).length,
      ),
    ),
    { initialValue: 0 },
  );

  protected readonly showError = (name: string) => showError(this.form, name);

  protected mismatch(): boolean {
    const confirm = this.form.controls.confirm;
    return this.form.hasError('mismatch') && (confirm.touched || confirm.dirty);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    const { name, email } = this.form.getRawValue();
    await this.auth.register(name.trim(), email);
    this.loading.set(false);
    this.toast.success(`Welcome to Course Academy, ${name.trim()}! (Demo account)`);
    this.router.navigateByUrl('/courses');
  }
}
