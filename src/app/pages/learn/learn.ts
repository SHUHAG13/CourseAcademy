import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/i18n/language.service';
import { Section, SectionDocument } from '../../core/models/course.model';
import { CertificateService } from '../../core/services/certificate.service';
import { CommunityService } from '../../core/services/community.service';
import { CourseService, allSections, sectionMinutes } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { failureMessage } from '../../core/services/permission.service';
import { ProgressService } from '../../core/services/progress.service';
import { ToastService } from '../../core/services/toast.service';
import { DocumentList } from '../../shared/components/document-list/document-list';
import { CourseCertificate } from './course-certificate/course-certificate';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { SessionList } from '../../shared/components/session-list/session-list';
import { VideoEmbed } from '../../shared/components/video-embed/video-embed';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** The course area for enrolled users: sections with their videos and documents, live classes and materials. */
@Component({
  selector: 'app-learn',
  imports: [TranslatePipe, RouterLink, NgTemplateOutlet, EmptyState, FormatBadge, SessionList, VideoEmbed, DocumentList, CourseCertificate],
  templateUrl: './learn.html',
  styleUrl: './learn.scss',
})
export class Learn {
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly toast = inject(ToastService);
  protected readonly progress = inject(ProgressService);
  protected readonly community = inject(CommunityService);
  private readonly certificates = inject(CertificateService);

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
  protected readonly sections = computed(() => {
    const c = this.course();
    return c ? allSections(c) : [];
  });

  /** Keeps the current section and video if an admin edit leaves them in place, otherwise starts at the first. */
  private readonly sectionId = linkedSignal<string | undefined>(() => this.sections()[0]?.id);
  protected readonly section = computed(() => this.sections().find((s) => s.id === this.sectionId()) ?? this.sections()[0]);
  private readonly videoId = linkedSignal<string | undefined>(() => this.section()?.videos[0]?.id);
  protected readonly video = computed(() => this.section()?.videos.find((v) => v.id === this.videoId()) ?? this.section()?.videos[0]);

  /** Module, submodule and parent section titles of the current section, for the heading above it. */
  protected readonly trail = computed(() => {
    const id = this.section()?.id;
    if (!id) return [];
    for (const m of this.course()?.modules ?? []) {
      for (const sub of m.submodules) {
        const path = pathTo(sub.sections, id);
        if (path) return [m.title, sub.title, ...path.slice(0, -1).map((s) => s.title)];
      }
    }
    return [];
  });
  protected readonly saving = signal(false);

  constructor() {
    const title = inject(Title);
    const language = inject(LanguageService);
    effect(() => {
      const c = this.course();
      title.setTitle(language.t('{0} | Course Academy', [c?.title ?? 'Course']));
    });
  }

  protected selectSection(id: string): void {
    this.sectionId.set(id);
  }

  protected selectVideo(id: string): void {
    this.videoId.set(id);
  }

  protected minutes(section: Section): number {
    return sectionMinutes(section);
  }

  /** Documents attached to `videoId`, or with no id, those for the whole section. */
  protected documentsFor(section: Section, videoId?: string): SectionDocument[] {
    return section.documents.filter((d) => d.videoId === videoId);
  }

  protected isComplete(sectionId: string): boolean {
    const c = this.course();
    return !!c && this.progress.isComplete(c.id, sectionId);
  }

  /** Marks the section done and moves on to the next one, or undoes it. */
  protected async toggleComplete(section: Section): Promise<void> {
    const c = this.course();
    if (!c) return;
    const complete = !this.isComplete(section.id);
    this.saving.set(true);
    try {
      await this.progress.setComplete(c, section.id, complete);
      const next = this.sections()[this.sections().indexOf(section) + 1];
      if (complete && next) this.sectionId.set(next.id);
      if (complete && this.progress.percent(c) === 100) this.toast.success('You have completed every section of {0}!', [c.title]);
      // Completion certificates are issued as soon as the requirement is met.
      const hadCertificate = !!this.certificates.status(c)?.certificate;
      if (complete && !hadCertificate && (await this.certificates.evaluate(c))) {
        this.toast.success('Congratulations! Your certificate is ready.');
      }
    } catch (error) {
      this.toast.error(failureMessage(error, 'Your progress could not be saved. Browser storage may be full or blocked.'));
    } finally {
      this.saving.set(false);
    }
  }
}

/** The sections from the top level down to the one with `id`, or null if it is not among them. */
function pathTo(sections: Section[], id: string): Section[] | null {
  for (const s of sections) {
    if (s.id === id) return [s];
    const below = pathTo(s.topics, id);
    if (below) return [s, ...below];
  }
  return null;
}
