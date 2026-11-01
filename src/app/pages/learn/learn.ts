import { Component, computed, effect, inject, input, linkedSignal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/i18n/language.service';
import { CourseService } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { SessionList } from '../../shared/components/session-list/session-list';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** The course area for enrolled users: lessons, live classes and materials. */
@Component({
  selector: 'app-learn',
  imports: [TranslatePipe, RouterLink, EmptyState, FormatBadge, SessionList],
  templateUrl: './learn.html',
  styleUrl: './learn.scss',
})
export class Learn {
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);

  /** Bound from the `:slug` route param. */
  readonly slug = input.required<string>();

  protected readonly course = computed(() => this.courses.bySlug(this.slug()));
  protected readonly hasAccess = computed(() => {
    const c = this.course();
    return !!c && this.enrollments.hasAccess(c.id);
  });
  protected readonly instructor = computed(() => {
    const c = this.course();
    return c ? this.courses.instructor(c.instructorId) : undefined;
  });
  protected readonly lessons = computed(() => this.course()?.curriculum.flatMap((s) => s.lessons) ?? []);

  /** Keeps the current lesson if an admin edit leaves it in place, otherwise starts at the first. */
  private readonly lessonId = linkedSignal<string | undefined>(() => this.lessons()[0]?.id);
  protected readonly lesson = computed(() => this.lessons().find((l) => l.id === this.lessonId()) ?? this.lessons()[0]);

  constructor() {
    const title = inject(Title);
    const language = inject(LanguageService);
    effect(() => {
      const c = this.course();
      title.setTitle(language.t('{0} | Course Academy', [c?.title ?? 'Course']));
    });
  }

  protected selectLesson(id: string): void {
    this.lessonId.set(id);
  }
}
