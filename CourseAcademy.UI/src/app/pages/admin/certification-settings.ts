import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CertificationMode, EXAM_TYPES, ExamType } from '../../core/models/certificate.model';
import { CertificateService } from '../../core/services/certificate.service';
import { CourseService } from '../../core/services/course.service';
import { failureMessage } from '../../core/services/permission.service';
import { ToastService } from '../../core/services/toast.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** Configures how one course awards its certificate. */
@Component({
  selector: 'app-certification-settings',
  imports: [TranslatePipe, ReactiveFormsModule, RouterLink, EmptyState],
  template: `
    @if (course(); as c) {
      <a routerLink="/admin/courses" class="small fw-semibold"><i class="bi bi-arrow-left" aria-hidden="true"></i> {{ 'All courses' | t }}</a>
      <h1 class="h3 mt-1 mb-3">{{ 'Certification' | t }}: {{ c.title | t }}</h1>

      <form class="panel ca-card" [formGroup]="form" (ngSubmit)="save()" novalidate>
        <div class="form-check form-switch mb-3">
          <input id="c-enabled" class="form-check-input" type="checkbox" role="switch" formControlName="enabled" />
          <label for="c-enabled" class="form-check-label">{{ 'This course awards a certificate' | t }}</label>
        </div>

        <fieldset class="mb-3" [disabled]="!values().enabled">
          <legend class="form-label">{{ 'How students earn it' | t }}</legend>
          @for (m of modes; track m.id) {
            <div class="form-check">
              <input class="form-check-input" type="radio" [id]="'mode-' + m.id" formControlName="mode" [value]="m.id" />
              <label class="form-check-label" [for]="'mode-' + m.id">
                <strong>{{ m.label | t }}</strong> <span class="text-muted small">— {{ m.help | t }}</span>
              </label>
            </div>
          }
        </fieldset>

        <div class="row g-3" [class.opacity-50]="!values().enabled">
          <div class="col-sm-6 col-lg-4">
            <label for="c-progress" class="form-label">{{ 'Required course progress (%)' | t }}</label>
            <input id="c-progress" type="number" min="1" max="100" class="form-control" formControlName="minProgress"
              [class.is-invalid]="invalid('minProgress')" />
          </div>
        </div>

        @if (values().mode === 'exam') {
          <h2 class="panel-title mt-4">{{ 'Central exam' | t }}</h2>
          <div class="row g-3">
            <div class="col-sm-6 col-lg-4">
              <label for="c-type" class="form-label">{{ 'Exam type' | t }}</label>
              <select id="c-type" class="form-select" formControlName="examType">
                @for (t of examTypes; track t) {
                  <option [value]="t">{{ (t === 'presentation' ? 'Presentation' : 'Project') | t }}</option>
                }
              </select>
            </div>
            <div class="col-sm-6 col-lg-8">
              <label for="c-title" class="form-label">{{ 'Exam title' | t }}</label>
              <input id="c-title" class="form-control" formControlName="examTitle" [class.is-invalid]="invalid('examTitle')" />
            </div>
            <div class="col-12">
              <label for="c-instructions" class="form-label">{{ 'Instructions for students' | t }}</label>
              <textarea id="c-instructions" class="form-control" rows="3" formControlName="examInstructions"></textarea>
            </div>
            <div class="col-4 col-lg-3">
              <label for="c-max" class="form-label">{{ 'Maximum score' | t }}</label>
              <input id="c-max" type="number" min="1" class="form-control" formControlName="maxScore" [class.is-invalid]="invalid('maxScore')" />
            </div>
            <div class="col-4 col-lg-3">
              <label for="c-pass" class="form-label">{{ 'Pass mark' | t }}</label>
              <input id="c-pass" type="number" min="1" class="form-control" formControlName="passingScore" [class.is-invalid]="invalid('passingScore')" />
            </div>
            <div class="col-4 col-lg-3">
              <label for="c-attempts" class="form-label">{{ 'Attempts (0 = unlimited)' | t }}</label>
              <input id="c-attempts" type="number" min="0" class="form-control" formControlName="maxAttempts" [class.is-invalid]="invalid('maxAttempts')" />
            </div>
          </div>
          <p class="small text-muted mt-2 mb-0">
            {{ 'Students hand in a link to their work. An instructor or admin scores it under Admin → Certificates, and a passing score issues the certificate.' | t }}
          </p>
        }

        <div class="d-flex justify-content-end gap-2 mt-4">
          <a routerLink="/admin/courses" class="btn btn-outline">{{ 'Cancel' | t }}</a>
          <button type="submit" class="btn btn-primary" [disabled]="saving()">{{ 'Save settings' | t }}</button>
        </div>
      </form>
    } @else {
      <app-empty-state icon="bi-journal-x" title="Course not found" message="This course doesn't exist or may have been removed.">
        <a routerLink="/admin/courses" class="btn btn-primary">{{ 'All courses' | t }}</a>
      </app-empty-state>
    }
  `,
})
export class CertificationSettings implements OnInit {
  private readonly certificates = inject(CertificateService);
  private readonly courses = inject(CourseService);
  private readonly toast = inject(ToastService);

  /** Bound from the `:id` route param. */
  readonly id = input.required<string>();

  protected readonly course = computed(() => this.courses.byId(Number(this.id())));
  protected readonly examTypes = EXAM_TYPES;
  protected readonly modes: { id: CertificationMode; label: string; help: string }[] = [
    { id: 'completion', label: 'Course completion', help: 'issued automatically when the progress requirement is met' },
    { id: 'exam', label: 'Central exam', help: 'after the course, students hand in a project or presentation that is scored' },
  ];
  protected readonly saving = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    enabled: [true],
    mode: ['completion' as CertificationMode],
    minProgress: [100, [Validators.required, Validators.min(1), Validators.max(100)]],
    examType: ['project' as ExamType],
    examTitle: [''],
    examInstructions: [''],
    maxScore: [100, [Validators.required, Validators.min(1)]],
    passingScore: [60, [Validators.required, Validators.min(1)]],
    maxAttempts: [0, [Validators.required, Validators.min(0)]],
  });
  protected readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  ngOnInit(): void {
    const course = this.course();
    if (course) this.form.reset(this.certificates.rule(course.id));
  }

  protected invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected async save(): Promise<void> {
    const course = this.course();
    if (!course) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    try {
      // The service checks the rest, e.g. that the pass mark is not above the maximum score.
      await this.certificates.saveRule({ id: course.id, ...this.form.getRawValue() });
      this.form.markAsPristine();
      this.toast.success('Certification settings for {0} were saved.', [course.title]);
    } catch (error) {
      this.toast.error(failureMessage(error, (error as Error).message));
    } finally {
      this.saving.set(false);
    }
  }
}
