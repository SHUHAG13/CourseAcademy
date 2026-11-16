import { API_MODE } from '../../core/api/api-mode';
import { ComingSoon } from '../../shared/components/coming-soon/coming-soon';
import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, input, linkedSignal, signal, untracked } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../core/i18n/language.service';
import { Section, SectionDocument } from '../../core/models/course.model';
import { CertificateService } from '../../core/services/certificate.service';
import { CommunityService } from '../../core/services/community.service';
import { CourseService, allSections, sectionMinutes } from '../../core/services/course.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { MediaService } from '../../core/services/media.service';
import { failureMessage } from '../../core/services/permission.service';
import { ProgressService } from '../../core/services/progress.service';
import { ToastService } from '../../core/services/toast.service';
import { DocumentList } from '../../shared/components/document-list/document-list';
import { CourseCertificate } from './course-certificate/course-certificate';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { FormatBadge } from '../../shared/components/format-badge/format-badge';
import { SessionList } from '../../shared/components/session-list/session-list';
import { VideoEmbed, VideoPosition } from '../../shared/components/video-embed/video-embed';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** The course area for enrolled users: sections with their videos and documents, live classes and materials. */
@Component({
  selector: 'app-learn',
  imports: [ComingSoon, TranslatePipe, RouterLink, NgTemplateOutlet, EmptyState, FormatBadge, SessionList, VideoEmbed, DocumentList, CourseCertificate],
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
  /** Certificates and communities have not moved to the API yet. */
  protected readonly apiMode = inject(API_MODE);

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
  protected readonly openingMaterial = signal<string | null>(null);
  private readonly media = inject(MediaService);

  constructor() {
    const title = inject(Title);
    const language = inject(LanguageService);
    effect(() => {
      const c = this.course();
      title.setTitle(language.t('{0} | Course Academy', [c?.title ?? 'Course']));
    });
    // API mode: progress lives on the server; load it whenever the course (or its curriculum) changes.
    effect(() => {
      const c = this.course();
      if (!this.apiMode || !c || !this.hasAccess()) return;
      untracked(() => this.progress.loadCourse(c).catch(() => this.toast.error('Your progress could not be loaded. Please refresh the page.')));
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

  protected isVideoComplete(videoId: string): boolean {
    const c = this.course();
    return !!c && this.progress.isVideoComplete(c.id, videoId);
  }

  protected resumeAt(videoId: string): number {
    const c = this.course();
    return c ? this.progress.position(c.id, videoId) : 0;
  }

  /** In API mode each video is a lesson; otherwise (and for sections without videos) the section is. */
  private lessonVideo(section: Section) {
    return this.apiMode && section.videos.length ? this.video() : undefined;
  }

  /** Whether the lesson shown for `section` is done: its current video in API mode, otherwise the section. */
  protected isLessonComplete(section: Section): boolean {
    const v = this.lessonVideo(section);
    return v ? this.isVideoComplete(v.id) : this.isComplete(section.id);
  }

  /** Marks the lesson done and moves on to the next video or section, or undoes it. */
  protected async toggleComplete(section: Section): Promise<void> {
    const c = this.course();
    if (!c) return;
    const video = this.lessonVideo(section);
    const complete = !this.isLessonComplete(section);
    this.saving.set(true);
    try {
      if (video) {
        await this.progress.setVideoComplete(c, video.id, complete);
      } else {
        await this.progress.setComplete(c, section.id, complete);
      }
      const nextVideo = video ? section.videos[section.videos.indexOf(video) + 1] : undefined;
      const next = this.sections()[this.sections().indexOf(section) + 1];
      if (complete && nextVideo) {
        this.videoId.set(nextVideo.id);
      } else if (complete && next) {
        this.sectionId.set(next.id);
      }
      if (complete && this.progress.percent(c) === 100) this.toast.success('You have completed every section of {0}!', [c.title]);
      // Completion certificates are issued as soon as the requirement is met.
      const hadCertificate = !!this.certificates.status(c)?.certificate;
      if (!this.apiMode && complete && !hadCertificate && (await this.certificates.evaluate(c))) {
        this.toast.success('Congratulations! Your certificate is ready.');
      }
    } catch (error) {
      this.toast.error(failureMessage(error, 'Your progress could not be saved. Browser storage may be full or blocked.'));
    } finally {
      this.saving.set(false);
    }
  }

  /** API mode: an uploaded material opens through a short-lived link from the API. */
  protected async openMaterial(id: string): Promise<void> {
    this.openingMaterial.set(id);
    try {
      await this.media.open('materials', id);
    } catch (error) {
      this.toast.error(failureMessage(error, 'The file could not be opened. Please try again.'));
    } finally {
      this.openingMaterial.set(null);
    }
  }

  /** A video file reported its position (API mode). The server completes it at 90% watched. */
  protected async onWatched(videoId: string, watched: VideoPosition): Promise<void> {
    const c = this.course();
    if (!c || !this.apiMode) return;
    if (await this.progress.savePosition(c, videoId, watched.position, watched.duration)) {
      this.toast.success(this.progress.percent(c) === 100 ? 'You have completed every section of {0}!' : 'Lesson completed.', [c.title]);
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
