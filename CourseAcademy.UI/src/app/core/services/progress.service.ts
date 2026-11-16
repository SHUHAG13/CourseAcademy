import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import { CourseProgressDto, CourseProgressSummaryDto } from '../api/learning.models';
import { PROGRESS_REPOSITORY } from '../data/repositories';
import { EntityStore } from '../data/repository';
import { Course } from '../models/course.model';
import { progressId } from '../models/progress.model';
import { AuthService } from './auth.service';
import { allSections } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

/**
 * The signed-in user's learning progress.
 * - API mode: kept on the server per lesson. A lesson is a video, or a section without videos (BR-LRN-15).
 * - Demo mode: completed section ids in browser storage.
 */
@Injectable({ providedIn: 'root' })
export class ProgressService {
  private readonly store = new EntityStore(inject(PROGRESS_REPOSITORY));
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);
  private readonly api = inject(ApiService);
  /** In API mode lessons are tracked per video, so the learning page shows a button per video. */
  readonly apiMode = inject(API_MODE);

  // API mode: totals per enrolled course (dashboard), and the full progress of courses opened in the learning page.
  private readonly apiSummaries = signal(new Map<number, CourseProgressSummaryDto>());
  private readonly apiCourses = signal(new Map<number, CourseProgressDto>());
  /** Videos whose position report is on its way. */
  private readonly reporting = new Set<string>();

  readonly ready: Promise<unknown> = this.apiMode ? this.auth.ready.then(() => this.loadMine()) : this.store.ready;
  readonly recovered = computed(() => !this.apiMode && this.store.recovered());

  /** Demo mode: the signed-in user's completed section ids, by course id. */
  private readonly mine = computed(() => {
    const userId = this.auth.user()?.id;
    return new Map(
      this.store
        .items()
        .filter((p) => p.userId === userId)
        .map((p) => [p.courseId, new Set(p.completedSectionIds)]),
    );
  });

  constructor() {
    if (this.apiMode) {
      let loadedFor: number | null | undefined;
      effect(() => {
        const id = this.auth.currentSession()?.id ?? null;
        untracked(() => {
          if (loadedFor !== undefined && loadedFor !== id) {
            this.apiCourses.set(new Map());
            void this.loadMine();
          }
          loadedFor = id;
        });
      });
    }
  }

  /** Whether the section is done: in API mode, all of its lessons. */
  isComplete(courseId: number, sectionId: string): boolean {
    if (this.apiMode) return this.apiCourses().get(courseId)?.completedSectionIds.includes(Number(sectionId)) ?? false;
    return this.mine().get(courseId)?.has(sectionId) ?? false;
  }

  /** API mode only. */
  isVideoComplete(courseId: number, videoId: string): boolean {
    return this.videoState(courseId, videoId)?.isCompleted ?? false;
  }

  /** API mode: where to resume a video, in seconds. */
  position(courseId: number, videoId: string): number {
    return this.videoState(courseId, videoId)?.positionSeconds ?? 0;
  }

  /** Completed lessons (API) or sections (demo) that are still in the course, so removed ones don't count. */
  completedCount(course: Course): number {
    if (this.apiMode) return this.serverTotals(course.id)?.completedLessons ?? 0;
    const done = this.mine().get(course.id);
    return done ? allSections(course).filter((s) => done.has(s.id)).length : 0;
  }

  /** Lessons (API) or sections (demo) that count towards the percentage. */
  totalCount(course: Course): number {
    if (this.apiMode) {
      return this.serverTotals(course.id)?.totalLessons ?? allSections(course).reduce((n, s) => n + Math.max(1, s.videos.length), 0);
    }
    return allSections(course).length;
  }

  percent(course: Course): number {
    if (this.apiMode) return this.serverTotals(course.id)?.percent ?? 0;
    const total = allSections(course).length;
    return total ? Math.round((this.completedCount(course) / total) * 100) : 0;
  }

  /** Demo mode: any user's progress, for checks such as certificate criteria. */
  percentFor(userId: string, course: Course): number {
    const done = new Set(this.store.items().find((p) => p.id === progressId(userId, course.id))?.completedSectionIds);
    const sections = allSections(course);
    return sections.length ? Math.round((sections.filter((s) => done.has(s.id)).length / sections.length) * 100) : 0;
  }

  /** API mode: loads the full progress of one course, for the learning page. */
  async loadCourse(course: Course): Promise<void> {
    if (!this.apiMode || !this.auth.currentSession()) return;
    this.keep(await this.api.get<CourseProgressDto>(`progress/courses/${encodeURIComponent(course.slug)}`));
  }

  /** API mode: reloads the totals of every enrolled course. */
  async loadMine(): Promise<void> {
    if (!this.apiMode) return;
    if (!this.auth.currentSession()) {
      this.apiSummaries.set(new Map());
      return;
    }
    try {
      const list = await this.api.get<CourseProgressSummaryDto[]>('progress/mine');
      this.apiSummaries.set(new Map(list.map((s) => [s.courseId, s])));
    } catch {
      // Keep what is shown; the next load tries again.
    }
  }

  /** Marks a section done or not. In API mode only for sections without videos. */
  async setComplete(course: Course, sectionId: string, complete: boolean): Promise<void> {
    const user = this.auth.user();
    if (!user || !this.permissions.can('learn.view') || !this.enrollments.hasAccess(course.id)) {
      throw new PermissionDeniedError();
    }
    if (!allSections(course).some((s) => s.id === sectionId)) throw new Error('Section not found.');

    if (this.apiMode) {
      this.keep(await this.api.put<CourseProgressDto>(`progress/sections/${sectionId}/complete`, { completed: complete }));
      return;
    }
    const done = new Set(this.mine().get(course.id));
    if (complete) {
      done.add(sectionId);
    } else {
      done.delete(sectionId);
    }
    await this.store.save({
      id: progressId(user.id, course.id),
      userId: user.id,
      courseId: course.id,
      completedSectionIds: [...done],
      updatedAt: new Date().toISOString(),
    });
  }

  /** API mode: marks a video done or not. */
  async setVideoComplete(course: Course, videoId: string, complete: boolean): Promise<void> {
    if (!this.apiMode) throw new Error('Videos are tracked one by one only in API mode.');
    this.keep(await this.api.put<CourseProgressDto>(`progress/videos/${videoId}/complete`, { completed: complete }));
  }

  /**
   * API mode: saves where the user is in a video file. Returns true when this completed the video (90% watched).
   * Failures are ignored: the next report saves the position again.
   */
  async savePosition(course: Course, videoId: string, positionSeconds: number, durationSeconds: number): Promise<boolean> {
    // At the end of a video the player sends "pause" and "ended" together; one report is enough.
    if (!this.apiMode || !this.auth.currentSession() || this.reporting.has(videoId)) return false;
    const wasComplete = this.isVideoComplete(course.id, videoId);
    this.reporting.add(videoId);
    try {
      this.keep(await this.api.put<CourseProgressDto>(`progress/videos/${videoId}/position`, { positionSeconds, durationSeconds }));
    } catch {
      return false;
    } finally {
      this.reporting.delete(videoId);
    }
    return !wasComplete && this.isVideoComplete(course.id, videoId);
  }

  reset(): Promise<void> {
    // Progress in API mode lives on the server; demo reset does not touch it.
    return this.apiMode ? Promise.resolve() : this.store.reset();
  }

  private videoState(courseId: number, videoId: string) {
    return this.apiCourses().get(courseId)?.videos.find((v) => v.videoId === Number(videoId));
  }

  private serverTotals(courseId: number): CourseProgressSummaryDto | undefined {
    return this.apiCourses().get(courseId) ?? this.apiSummaries().get(courseId);
  }

  /** Keeps a course's progress from the server and updates its dashboard totals with it. */
  private keep(progress: CourseProgressDto): void {
    this.apiCourses.update((map) => new Map(map).set(progress.courseId, progress));
    const { courseId, completedLessons, totalLessons, percent } = progress;
    this.apiSummaries.update((map) => new Map(map).set(courseId, { courseId, completedLessons, totalLessons, percent }));
  }
}
