import { Injectable, computed, inject } from '@angular/core';
import { PROGRESS_REPOSITORY } from '../data/repositories';
import { EntityStore } from '../data/repository';
import { Course } from '../models/course.model';
import { progressId } from '../models/progress.model';
import { AuthService } from './auth.service';
import { allSections } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

/** Which sections the signed-in user has completed. */
@Injectable({ providedIn: 'root' })
export class ProgressService {
  private readonly store = new EntityStore(inject(PROGRESS_REPOSITORY));
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);

  readonly ready = this.store.ready;
  readonly recovered = this.store.recovered;

  /** The signed-in user's completed section ids, by course id. */
  private readonly mine = computed(() => {
    const userId = this.auth.user()?.id;
    return new Map(
      this.store
        .items()
        .filter((p) => p.userId === userId)
        .map((p) => [p.courseId, new Set(p.completedSectionIds)]),
    );
  });

  isComplete(courseId: number, sectionId: string): boolean {
    return this.mine().get(courseId)?.has(sectionId) ?? false;
  }

  /** Completed sections that are still in the course, so removed sections don't count. */
  completedCount(course: Course): number {
    const done = this.mine().get(course.id);
    return done ? allSections(course).filter((s) => done.has(s.id)).length : 0;
  }

  percent(course: Course): number {
    const total = allSections(course).length;
    return total ? Math.round((this.completedCount(course) / total) * 100) : 0;
  }

  /** Any user's progress, for checks such as certificate criteria. */
  percentFor(userId: string, course: Course): number {
    const done = new Set(this.store.items().find((p) => p.id === progressId(userId, course.id))?.completedSectionIds);
    const sections = allSections(course);
    return sections.length ? Math.round((sections.filter((s) => done.has(s.id)).length / sections.length) * 100) : 0;
  }

  async setComplete(course: Course, sectionId: string, complete: boolean): Promise<void> {
    const user = this.auth.user();
    if (!user || !this.permissions.can('learn.view') || !this.enrollments.hasAccess(course.id)) {
      throw new PermissionDeniedError();
    }
    if (!allSections(course).some((s) => s.id === sectionId)) throw new Error('Section not found.');

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

  reset(): Promise<void> {
    return this.store.reset();
  }
}
