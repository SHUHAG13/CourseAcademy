import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { CONTACT } from '../../core/data/site.data';
import { Course } from '../../core/models/course.model';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { CourseCard } from '../../shared/components/course-card/course-card';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { RatingStars } from '../../shared/components/rating-stars/rating-stars';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';

@Component({
  selector: 'app-course-detail',
  imports: [RouterLink, DatePipe, DecimalPipe, BdtPipe, Avatar, CourseCard, CourseCover, EmptyState, RatingStars],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail {
  private readonly courseService = inject(CourseService);
  private readonly toast = inject(ToastService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  /** Bound from the `:slug` route param. */
  readonly slug = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly course = signal<Course | undefined>(undefined);
  protected readonly openSections = signal<Set<number>>(new Set([0]));

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
  protected readonly lessonCount = computed(
    () => this.course()?.curriculum.reduce((n, s) => n + s.lessons.length, 0) ?? 0,
  );
  protected readonly discount = computed(() => {
    const c = this.course();
    return c ? Math.round((1 - c.price / c.originalPrice) * 100) : 0;
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
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    const course = await this.courseService.getBySlug(slug);
    if (slug !== this.slug()) return;

    this.course.set(course);
    this.openSections.set(new Set([0]));
    this.loading.set(false);

    if (course) {
      this.title.setTitle(`${course.title} | Course Academy`);
      this.meta.updateTag({ name: 'description', content: course.subtitle });
    } else {
      this.title.setTitle('Course not found | Course Academy');
    }
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
    this.toast.show(`“${title}” — free preview playback will be available when the video platform is connected.`);
  }

  protected share(): void {
    navigator.clipboard
      ?.writeText(location.href)
      .then(() => this.toast.success('Course link copied to clipboard.'))
      .catch(() => this.toast.error('Could not copy the link.'));
  }
}
