import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { uid } from '../../../core/data/repository';
import {
  COURSE_FORMATS,
  Course,
  CurriculumSection,
  Lesson,
  LiveSession,
  MATERIAL_TYPES,
  Material,
} from '../../../core/models/course.model';
import { CourseService } from '../../../core/services/course.service';
import { ToastService } from '../../../core/services/toast.service';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { URL_PATTERN } from '../../../shared/forms/validators';
import { BdtPipe } from '../../../shared/pipes/bdt.pipe';

const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const IMAGE_PATTERN = /^(?:\/[^\s/]|https?:\/\/)\S*$/i;

export const SITE_IMAGES = [
  { label: 'Creative studio', url: '/images/web/creative-studio.webp' },
  { label: 'Development team', url: '/images/web/dev-team.webp' },
  { label: 'Pitchside camera', url: '/images/web/pitchside-camera.webp' },
  { label: 'Reporter at stadium', url: '/images/web/reporter-stadium.webp' },
  { label: 'Sideline interview', url: '/images/web/sideline-interview.webp' },
];

function slugify(value: string): string {
  return value
    .toLowerCase()
    .replace(/&/g, ' and ')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function lines(value: string): string[] {
  return value
    .split('\n')
    .map((l) => l.trim())
    .filter(Boolean);
}

@Component({
  selector: 'app-course-form',
  imports: [ReactiveFormsModule, RouterLink, BdtPipe, EmptyState],
  templateUrl: './course-form.html',
  styleUrl: './course-form.scss',
})
export class CourseForm implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly courses = inject(CourseService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  /** Bound from the `:id` route param. Absent when creating a course. */
  readonly id = input<string>();

  protected readonly formats = COURSE_FORMATS;
  protected readonly materialTypes = MATERIAL_TYPES;
  protected readonly levels = ['Beginner', 'Intermediate', 'Advanced'];
  protected readonly badges = ['Bestseller', 'New', 'Popular'];
  protected readonly categories = this.courses.categories;
  protected readonly instructors = this.courses.instructors;
  protected readonly siteImages = SITE_IMAGES;

  protected readonly saving = signal(false);
  protected readonly existing = computed(() => {
    const id = Number(this.id());
    return Number.isFinite(id) ? this.courses.byId(id) : undefined;
  });
  protected readonly isNew = computed(() => this.id() === undefined);

  protected readonly form = this.fb.group(
    {
      title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(120)]],
      slug: ['', [Validators.required, Validators.pattern(SLUG_PATTERN), this.uniqueSlug.bind(this)]],
      subtitle: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', Validators.required],
      category: ['video-editing' as Course['category'], Validators.required],
      format: ['Pre-recorded' as Course['format'], Validators.required],
      instructorId: ['', Validators.required],
      level: ['Beginner' as Course['level'], Validators.required],
      language: ['Bangla', Validators.required],
      durationHours: [1, [Validators.required, Validators.min(1)]],
      originalPrice: [0, [Validators.required, Validators.min(0)]],
      price: [0, [Validators.required, Validators.min(0)]],
      coverLabel: ['', [Validators.required, Validators.maxLength(30)]],
      coverIcon: ['bi-journal-text', Validators.required],
      image: ['', Validators.pattern(IMAGE_PATTERN)],
      badge: [''],
      featured: [false],
      published: [false],
      outcomes: ['', Validators.required],
      requirements: [''],
      sections: this.fb.array<FormGroup>([]),
      sessions: this.fb.array<FormGroup>([]),
      materials: this.fb.array<FormGroup>([]),
    },
    { validators: [saleNotAboveRegular, publishedHasContent] },
  );

  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly discountPreview = computed(() => {
    const { price = 0, originalPrice = 0 } = this.values();
    return originalPrice > 0 && price <= originalPrice ? Math.round((1 - price / originalPrice) * 100) : 0;
  });
  protected readonly salePrice = computed(() => this.values().price ?? 0);
  protected readonly imagePreview = computed(() => {
    const control = this.form.controls.image;
    return this.values().image && control.valid ? control.value : '';
  });
  protected readonly needsSchedule = computed(() => this.values().format !== 'Pre-recorded');

  get sections(): FormArray<FormGroup> {
    return this.form.controls.sections;
  }

  get sessions(): FormArray<FormGroup> {
    return this.form.controls.sessions;
  }

  get materials(): FormArray<FormGroup> {
    return this.form.controls.materials;
  }

  lessonsOf(section: AbstractControl): FormArray<FormGroup> {
    return section.get('lessons') as FormArray<FormGroup>;
  }

  ngOnInit(): void {
    const course = this.existing();
    if (!course) {
      this.form.controls.instructorId.setValue(this.instructors()[0]?.id ?? '');
      return;
    }

    this.form.patchValue({
      ...course,
      image: course.image ?? '',
      badge: course.badge ?? '',
      featured: !!course.featured,
      outcomes: course.outcomes.join('\n'),
      requirements: course.requirements.join('\n'),
    });
    course.curriculum.forEach((s) => this.sections.push(this.sectionGroup(s)));
    course.schedule.forEach((s) => this.sessions.push(this.sessionGroup(s)));
    course.materials.forEach((m) => this.materials.push(this.materialGroup(m)));
  }

  // ── Form rows ───────────────────────────────────────────────

  private sectionGroup(section?: CurriculumSection): FormGroup {
    return this.fb.group({
      id: [section?.id ?? uid('section')],
      title: [section?.title ?? '', Validators.required],
      lessons: this.fb.array<FormGroup>((section?.lessons ?? []).map((l) => this.lessonGroup(l))),
    });
  }

  private lessonGroup(lesson?: Lesson): FormGroup {
    return this.fb.group({
      id: [lesson?.id ?? uid('lesson')],
      title: [lesson?.title ?? '', Validators.required],
      minutes: [lesson?.minutes ?? 10, [Validators.required, Validators.min(1)]],
      preview: [!!lesson?.preview],
    });
  }

  private sessionGroup(session?: LiveSession): FormGroup {
    return this.fb.group({
      id: [session?.id ?? uid('session')],
      title: [session?.title ?? '', Validators.required],
      startsAt: [session?.startsAt ?? '', Validators.required],
      durationMinutes: [session?.durationMinutes ?? 60, [Validators.required, Validators.min(1)]],
      meetingUrl: [session?.meetingUrl ?? 'https://meet.example.com/your-class', [Validators.required, Validators.pattern(URL_PATTERN)]],
    });
  }

  private materialGroup(material?: Material): FormGroup {
    return this.fb.group({
      id: [material?.id ?? uid('material')],
      title: [material?.title ?? '', Validators.required],
      type: [material?.type ?? 'PDF', Validators.required],
      url: [material?.url ?? 'https://example.com/demo-materials/file', [Validators.required, Validators.pattern(URL_PATTERN)]],
    });
  }

  protected addSection(): void {
    this.sections.push(this.sectionGroup());
  }

  protected addLesson(section: AbstractControl): void {
    this.lessonsOf(section).push(this.lessonGroup());
  }

  protected addSession(): void {
    this.sessions.push(this.sessionGroup());
  }

  protected addMaterial(): void {
    this.materials.push(this.materialGroup());
  }

  protected removeAt(array: FormArray, index: number): void {
    array.removeAt(index);
    this.form.markAsDirty();
  }

  /** Moves a row up (-1) or down (+1). Also used for reordering lessons and sections. */
  protected move(array: FormArray, index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= array.length) return;
    const control = array.at(index);
    array.removeAt(index);
    array.insert(target, control);
    this.form.markAsDirty();
  }

  // ── Validation ──────────────────────────────────────────────

  protected invalid(control: AbstractControl | null): boolean {
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  private uniqueSlug(control: AbstractControl): ValidationErrors | null {
    return this.courses.isSlugTaken(control.value, Number(this.id())) ? { taken: true } : null;
  }

  protected fillSlug(): void {
    const { slug, title } = this.form.controls;
    if (!slug.value && title.value) {
      slug.setValue(slugify(title.value));
      slug.markAsDirty();
    }
  }

  // ── Save ────────────────────────────────────────────────────

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('Please fix the highlighted fields.');
      return;
    }

    const value = this.form.getRawValue();
    const previous = this.existing();
    const course: Course = {
      // Keep demo stats that this form doesn't edit.
      rating: previous?.rating ?? 0,
      ratingCount: previous?.ratingCount ?? 0,
      students: previous?.students ?? 0,
      id: previous?.id ?? this.courses.nextCourseId(),
      slug: value.slug,
      title: value.title.trim(),
      subtitle: value.subtitle.trim(),
      description: value.description.trim(),
      category: value.category,
      format: value.format,
      published: value.published,
      instructorId: value.instructorId,
      durationHours: value.durationHours,
      level: value.level,
      language: value.language.trim(),
      price: value.price,
      originalPrice: value.originalPrice,
      coverLabel: value.coverLabel.trim(),
      coverIcon: value.coverIcon.trim(),
      image: value.image.trim() || undefined,
      badge: (value.badge || undefined) as Course['badge'],
      featured: value.featured,
      updated: new Date().toISOString().slice(0, 7),
      outcomes: lines(value.outcomes),
      requirements: lines(value.requirements),
      curriculum: value.sections as CurriculumSection[],
      schedule: value.sessions as LiveSession[],
      materials: value.materials as Material[],
    };

    this.saving.set(true);
    try {
      await this.courses.saveCourse(course);
      this.form.markAsPristine();
      this.toast.success(`${course.title} was saved.`);
      await this.router.navigateByUrl('/admin/courses');
    } catch {
      this.toast.error('The course could not be saved. Browser storage may be full or blocked.');
    } finally {
      this.saving.set(false);
    }
  }
}

function saleNotAboveRegular(group: AbstractControl): ValidationErrors | null {
  const { price, originalPrice } = group.value;
  return price > originalPrice ? { priceAboveRegular: true } : null;
}

/** Drafts can be incomplete, but a published course must have what its format promises. */
function publishedHasContent(group: AbstractControl): ValidationErrors | null {
  const { published, format, sections, sessions } = group.value;
  if (!published) return null;

  const hasLessons = (sections ?? []).some((s: { lessons?: unknown[] }) => s.lessons?.length);
  const errors: ValidationErrors = {};
  if (!hasLessons) errors['needsLessons'] = true;
  if (format !== 'Pre-recorded' && !sessions?.length) errors['needsSessions'] = true;
  return Object.keys(errors).length ? errors : null;
}
