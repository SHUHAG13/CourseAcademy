import { Component, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { PermissionService } from '../../core/services/permission.service';
import { ToastService } from '../../core/services/toast.service';
import { BD_PHONE_PATTERN, PASSWORD_PATTERN, matchFields, showError } from '../../shared/forms/validators';
import { AuthShell } from './auth-shell';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-register',
  imports: [TranslatePipe, ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './register.html',
  styleUrl: './auth-form.scss',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly permissions = inject(PermissionService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Bound from `?returnUrl=`, passed along from the login page. */
  readonly returnUrl = input<string>();

  protected readonly loading = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly error = signal('');

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
    this.error.set('');
    const { name, email, password, phone } = this.form.getRawValue();
    try {
      await this.auth.register(name.trim(), email, password, phone);
      this.toast.success('Welcome to Course Academy, {0}! (Demo account)', [name.trim()]);
      this.router.navigateByUrl(this.returnUrl() || this.permissions.homeUrl());
    } catch (error) {
      this.error.set((error as Error).message);
    } finally {
      this.loading.set(false);
    }
  }
}
