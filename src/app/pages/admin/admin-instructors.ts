import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { uid } from '../../core/data/repository';
import { Instructor } from '../../core/models/course.model';
import { ConfirmService } from '../../core/services/confirm.service';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { showError } from '../../shared/forms/validators';

@Component({
  selector: 'app-admin-instructors',
  imports: [ReactiveFormsModule, Avatar],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
      <h1 class="h3 mb-0">Instructors</h1>
      @if (!formOpen()) {
        <button type="button" class="btn btn-primary" (click)="startCreate()">
          <i class="bi bi-plus-lg" aria-hidden="true"></i> New instructor
        </button>
      }
    </div>

    @if (formOpen()) {
      <form class="panel ca-card mb-3" [formGroup]="form" (ngSubmit)="save()" novalidate>
        <h2 class="panel-title">{{ editing() ? 'Edit instructor' : 'New instructor' }}</h2>
        <div class="row g-3">
          <div class="col-md-6">
            <label for="i-name" class="form-label">Name</label>
            <input id="i-name" class="form-control" formControlName="name" [class.is-invalid]="showError('name')" />
            @if (showError('name')) {
              <div class="field-error">Enter a name.</div>
            }
          </div>
          <div class="col-md-6">
            <label for="i-title" class="form-label">Professional title</label>
            <input id="i-title" class="form-control" formControlName="title" [class.is-invalid]="showError('title')" />
            @if (showError('title')) {
              <div class="field-error">Enter a title.</div>
            }
          </div>
          <div class="col-12">
            <label for="i-bio" class="form-label">Biography</label>
            <textarea id="i-bio" class="form-control" rows="3" formControlName="bio" [class.is-invalid]="showError('bio')"></textarea>
            @if (showError('bio')) {
              <div class="field-error">Enter a short biography.</div>
            }
          </div>
          <div class="col-md-6">
            <label for="i-expertise" class="form-label">Expertise <span class="text-muted fw-normal">(comma separated)</span></label>
            <input id="i-expertise" class="form-control" formControlName="expertise" />
          </div>
          <div class="col-6 col-md-3">
            <label for="i-years" class="form-label">Years' experience</label>
            <input id="i-years" type="number" min="0" class="form-control" formControlName="experienceYears"
              [class.is-invalid]="showError('experienceYears')" />
            @if (showError('experienceYears')) {
              <div class="field-error">Enter 0 or more.</div>
            }
          </div>
          <div class="col-6 col-md-3">
            <label for="i-students" class="form-label">Students taught</label>
            <input id="i-students" type="number" min="0" class="form-control" formControlName="students"
              [class.is-invalid]="showError('students')" />
            @if (showError('students')) {
              <div class="field-error">Enter 0 or more.</div>
            }
          </div>
        </div>
        <div class="d-flex justify-content-end gap-2 mt-3">
          <button type="button" class="btn btn-outline" (click)="closeForm()">Cancel</button>
          <button type="submit" class="btn btn-primary">Save instructor</button>
        </div>
      </form>
    }

    <div class="row row-cols-1 row-cols-xl-2 g-3">
      @for (i of courses.instructors(); track i.id) {
        <div class="col">
          <article class="panel ca-card h-100">
            <div class="d-flex gap-3 align-items-start">
              <app-avatar [name]="i.name" [size]="52" />
              <div class="flex-grow-1 min-w-0">
                <h2 class="h6 mb-0">{{ i.name }}</h2>
                <p class="small text-muted mb-1">{{ i.title }}</p>
                <p class="small mb-2">{{ i.bio }}</p>
                <p class="small text-muted mb-0">{{ i.experienceYears }} years · {{ courses.assignedCourses(i.id).length }} assigned courses</p>
              </div>
              <div class="d-flex gap-1">
                <button type="button" class="icon-btn" (click)="startEdit(i)" [attr.aria-label]="'Edit ' + i.name" title="Edit"><i class="bi bi-pencil" aria-hidden="true"></i></button>
                <button type="button" class="icon-btn danger" (click)="remove(i)" [attr.aria-label]="'Delete ' + i.name" title="Delete"><i class="bi bi-trash" aria-hidden="true"></i></button>
              </div>
            </div>
          </article>
        </div>
      }
    </div>
  `,
})
export class AdminInstructors {
  protected readonly courses = inject(CourseService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Instructor | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    title: ['', Validators.required],
    bio: ['', Validators.required],
    expertise: [''],
    experienceYears: [0, [Validators.required, Validators.min(0)]],
    students: [0, [Validators.required, Validators.min(0)]],
  });

  protected readonly showError = (name: string) => showError(this.form, name);

  protected startCreate(): void {
    this.editing.set(null);
    this.form.reset();
    this.formOpen.set(true);
  }

  protected startEdit(instructor: Instructor): void {
    this.editing.set(instructor);
    this.form.reset({ ...instructor, expertise: instructor.expertise.join(', ') });
    this.formOpen.set(true);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const previous = this.editing();
    try {
      await this.courses.saveInstructor({
        ...value,
        id: previous?.id ?? uid('instructor'),
        name: value.name.trim(),
        title: value.title.trim(),
        bio: value.bio.trim(),
        expertise: value.expertise.split(',').map((e) => e.trim()).filter(Boolean),
        rating: previous?.rating ?? 0,
      });
      this.toast.success(`${value.name.trim()} was saved.`);
      this.closeForm();
    } catch {
      this.toast.error('The instructor could not be saved. Browser storage may be full or blocked.');
    }
  }

  protected async remove(instructor: Instructor): Promise<void> {
    const assigned = this.courses.assignedCourses(instructor.id).length;
    if (assigned) {
      this.toast.error(`${instructor.name} teaches ${assigned} course${assigned === 1 ? '' : 's'}. Reassign them before deleting.`);
      return;
    }
    const confirmed = await this.confirm.ask({
      title: `Delete ${instructor.name}?`,
      message: 'This cannot be undone, except by resetting all demo data.',
      confirmLabel: 'Delete instructor',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await this.courses.removeInstructor(instructor.id);
      this.toast.success(`${instructor.name} was deleted.`);
    } catch {
      this.toast.error('The instructor could not be deleted. Browser storage may be full or blocked.');
    }
  }
}
