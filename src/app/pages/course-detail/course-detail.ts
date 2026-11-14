import { API_MODE } from '../../core/api/api-mode';
import { ComingSoon } from '../../shared/components/coming-soon/coming-soon';
import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/i18n/language.service';
import { CONTACT } from '../../core/data/site.data';
import { CourseCommunity } from './course-community/course-community';
import { AuthService } from '../../core/services/auth.service';
import { CourseModule, Section, SectionDocument } from '../../core/models/course.model';
import { CourseService, allSections, discountPercent, moduleMinutes, sectionCount } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { PermissionService } from '../../core/services/permission.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { CourseCard } from '../../shared/components/course-card/course-card';
import { CourseCover } from '../../shared/components/course-cover/course-cover';
import { DocumentList } from '../../shared/components/document-list/document-list';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { RatingStars } from '../../shared/components/rating-stars/rating-stars';
import { SessionList } from '../../shared/components/session-list/session-list';
import { VideoEmbed } from '../../shared/components/video-embed/video-embed';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';
import { TranslatePipe, LocalNumberPipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';

const FORMAT_INTRO = {
  'Pre-recorded': 'Self-paced recorded lessons you can watch anytime.',
  Live: 'Instructor-led live online classes on a fixed schedule.',
  Hybrid: 'Self-paced recorded lessons plus scheduled live classes.',
};

@Component({
  selector: 'app-course-detail',
  imports: [ComingSoon, 
    TranslatePipe, LocalNumberPipe, LocalDatePipe,
    RouterLink,
    BdtPipe,
    Avatar,
    CourseCard,
    CourseCommunity,
    CourseCover,
    DocumentList,
    EmptyState,
    FormatBadge,
    RatingStars,
    SessionList,
    VideoEmbed,
    NgTemplateOutlet,
  ],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail {
  private readonly courseService = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  protected readonly auth = inject(AuthService);
  /** Course communities have not moved to the API yet. */
  protected readonly apiMode = inject(API_MODE);
  private readonly permissions = inject(PermissionService);
  private readonly toast = inject(ToastService);
  private readonly language = inject(LanguageService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  /** Bound from the `:slug` route param. */
  readonly slug = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly openSections = signal<Set<number>>(new Set([0]));
  /** The free preview video playing inline, if any. */
  protected readonly previewId = signal<string | null>(null);

  /** Follows the store, so admin edits appear immediately. Only admins can open unpublished courses. */
  protected readonly course = computed(() => {
    const c = this.courseService.bySlug(this.slug());
    return c && (c.published || this.permissions.can('courses.view', c.id)) ? c : undefined;
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
  protected readonly sectionCount = computed(() => {
    const c = this.course();
    return c ? sectionCount(c) : 0;
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
    () => this.openSections().size === (this.course()?.modules.length ?? 0),
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

  protected moduleMinutes(module: CourseModule): number {
    return moduleMinutes(module);
  }

  /** Sections in a module, topics included. */
  protected moduleSections(module: CourseModule): number {
    return allSections(module).length;
  }

  /** A section with one video of its own name shows as that video's row only. */
  protected isSingleVideo(section: Section): boolean {
    return section.videos.length === 1 && section.videos[0].title === section.title;
  }

  /** Documents attached to `videoId`, or with no id, those for the whole section. */
  protected documentsFor(section: Section, videoId?: string): SectionDocument[] {
    return section.documents.filter((d) => d.videoId === videoId);
  }

  protected togglePreview(videoId: string): void {
    this.previewId.set(this.previewId() === videoId ? null : videoId);
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
    const count = this.course()?.modules.length ?? 0;
    this.openSections.set(this.allOpen() ? new Set() : new Set(Array.from({ length: count }, (_, i) => i)));
  }

  /** Opens the first free preview video in the curriculum. */
  protected playPreview(): void {
    const modules = this.course()?.modules ?? [];
    const index = modules.findIndex((m) => m.submodules.some((s) => s.sections.some((sec) => sec.videos.some((v) => v.preview))));
    if (index === -1) {
      this.toast.show('This course has no free preview videos yet.');
      return;
    }
    const video = allSections(modules[index]).flatMap((s) => s.videos).find((v) => v.preview)!;
    this.openSections.update((open) => new Set(open).add(index));
    this.previewId.set(video.id);
    document.getElementById('curriculum-title')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected share(): void {
    navigator.clipboard
      ?.writeText(location.href)
      .then(() => this.toast.success('Course link copied to clipboard.'))
      .catch(() => this.toast.error('Could not copy the link.'));
  }
}
