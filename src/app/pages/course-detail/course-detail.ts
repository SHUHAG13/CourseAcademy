import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/i18n/language.service';
import { CONTACT } from '../../core/data/site.data';
import { AuthService } from '../../core/services/auth.service';
import { CourseService, discountPercent, lessonCount } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { CourseCard } from '../../shared/components/course-card/course-card';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { RatingStars } from '../../shared/components/rating-stars/rating-stars';
import { SessionList } from '../../shared/components/session-list/session-list';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { TranslatePipe, LocalNumberPipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';

const FORMAT_INTRO = {
  'Pre-recorded': 'Self-paced recorded lessons you can watch anytime.',
  Live: 'Instructor-led live online classes on a fixed schedule.',
  Hybrid: 'Self-paced recorded lessons plus scheduled live classes.',
};

@Component({
  selector: 'app-course-detail',
  imports: [
    TranslatePipe, LocalNumberPipe, LocalDatePipe,
    RouterLink,
    BdtPipe,
    Avatar,
    CourseCard,
    CourseCover,
    EmptyState,
    FormatBadge,
    RatingStars,
    SessionList,
  ],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail {
  private readonly courseService = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  protected readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly language = inject(LanguageService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  /** Bound from the `:slug` route param. */
  readonly slug = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly openSections = signal<Set<number>>(new Set([0]));

  /** Follows the store, so admin edits appear immediately. Only admins can open unpublished courses. */
  protected readonly course = computed(() => {
    const c = this.courseService.bySlug(this.slug());
    return c && (c.published || this.auth.isAdmin()) ? c : undefined;
  });

  protected readonly category = computed(() => {
    const c = this.course();
    return c ? this.courseService.category(c.category) : undefined;
  });
  protected readonly instructor = computed(() => {
    const c = this.course();
    return c ? this.courseService.instructor(c.instructorId) : undefined;
  });
  protected readonly instructorCourses = computed(() => {
    const i = this.instructor();
    return i ? this.courseService.coursesByInstructor(i.id) : 0;
  });
  protected readonly lessonCount = computed(() => {
    const c = this.course();
    return c ? lessonCount(c) : 0;
  });
  protected readonly discount = computed(() => {
    const c = this.course();
    return c ? discountPercent(c) : 0;
  });
  protected readonly reviews = computed(() => {
    const c = this.course();
    return c ? this.courseService.reviews(c) : [];
  });
  protected readonly breakdown = computed(() => {
    const c = this.course();
    return c ? this.courseService.ratingBreakdown(c) : [];
  });
  protected readonly related = computed(() => {
    const c = this.course();
    return c ? this.courseService.related(c) : [];
  });
  protected readonly enrolled = computed(() => {
    const c = this.course();
    return !!c && this.enrollments.isEnrolled(c.id);
  });
  protected readonly hasAccess = computed(() => {
    const c = this.course();
    return !!c && this.enrollments.hasAccess(c.id);
  });
  protected readonly formatIntro = computed(() => {
    const c = this.course();
    return c ? FORMAT_INTRO[c.format] : '';
  });
  protected readonly allOpen = computed(
    () => this.openSections().size === (this.course()?.curriculum.length ?? 0),
  );
  protected readonly whatsappLink = computed(() => {
    const text = `Hi! I'd like to enroll in "${this.course()?.title}". Please share the payment details.`;
    return `https://wa.me/${CONTACT.whatsapp}?text=${encodeURIComponent(text)}`;
  });

  constructor() {
    // Reload whenever the slug changes (e.g. clicking a related course).
    effect(() => {
      const slug = this.slug();
      untracked(() => this.load(slug));
    });

    effect(() => {
      if (this.loading()) return;
      const course = this.course();
      if (course) {
        this.title.setTitle(this.language.t('{0} | Course Academy', [course.title]));
        this.meta.updateTag({ name: 'description', content: this.language.t(course.subtitle) });
      } else {
        this.title.setTitle(this.language.t('{0} | Course Academy', ['Course not found']));
      }
    });
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    await this.courseService.whenLoaded();
    if (slug !== this.slug()) return;

    this.openSections.set(new Set([0]));
    this.loading.set(false);
  }

  protected sectionMinutes(index: number): number {
    return this.course()!.curriculum[index].lessons.reduce((n, l) => n + l.minutes, 0);
  }

  protected toggleSection(index: number): void {
    this.openSections.update((open) => {
      const next = new Set(open);
      if (next.has(index)) {
        next.delete(index);
      } else {
        next.add(index);
      }
      return next;
    });
  }

  protected toggleAll(): void {
    const count = this.course()?.curriculum.length ?? 0;
    this.openSections.set(this.allOpen() ? new Set() : new Set(Array.from({ length: count }, (_, i) => i)));
  }

  protected playPreview(): void {
    this.toast.show('Preview videos are coming soon. Check the free preview lessons in the curriculum.');
  }

  protected previewLesson(title: string): void {
    this.toast.show('“{0}” — free preview playback will be available when the video platform is connected.', 'info', [title]);
  }

  protected share(): void {
    navigator.clipboard
      ?.writeText(location.href)
      .then(() => this.toast.success('Course link copied to clipboard.'))
      .catch(() => this.toast.error('Could not copy the link.'));
  }
}
