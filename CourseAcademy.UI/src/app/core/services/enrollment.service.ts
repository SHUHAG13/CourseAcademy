import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import { PagedResult } from '../api/api.models';
import { EnrollmentDto, fromEnrollmentDto } from '../api/commerce.models';
import { ENROLLMENT_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { Course } from '../models/course.model';
import { Enrollment, EnrollmentStatus } from '../models/enrollment.model';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { PermissionDeniedError, PermissionService } from './permission.service';

const PAGE_SIZE = 100;

/**
 * Who has access to which course.
 * - API mode: the API grants access (free enrollment, verified payments) and keeps the records.
 * - Demo mode: records live in browser storage.
 */
@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private readonly store = new EntityStore(inject(ENROLLMENT_REPOSITORY));
  private readonly auth = inject(AuthService);
  private readonly permissions = inject(PermissionService);
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);
  private readonly courses = inject(CourseService);

  // API mode: the signed-in user's enrollments, and every enrollment for people who may see them.
  private readonly apiMine = signal<Enrollment[]>([]);
  private readonly apiAll = signal<Enrollment[] | null>(null);

  readonly ready: Promise<unknown> = this.apiMode ? this.auth.ready.then(() => this.load()) : this.store.ready;
  readonly recovered = computed(() => !this.apiMode && this.store.recovered());

  /** Every enrollment this user may see: all of them for staff, otherwise their own. */
  readonly enrollments = computed(() => (this.apiMode ? (this.apiAll() ?? this.apiMine()) : this.store.items()));

  /** The signed-in user's enrollments, newest first. */
  readonly mine = computed(() => {
    if (this.apiMode) return this.apiMine();
    const id = this.auth.user()?.id;
    return this.enrollments()
      .filter((e) => e.userId === id)
      .sort((a, b) => b.enrolledAt.localeCompare(a.enrolledAt));
  });

  constructor() {
    if (this.apiMode) {
      let loadedFor: number | null | undefined;
      effect(() => {
        const id = this.auth.currentSession()?.id ?? null;
        untracked(() => {
          if (loadedFor !== undefined && loadedFor !== id) void this.load();
          loadedFor = id;
        });
      });
    }
  }

  isEnrolled(courseId: number): boolean {
    return this.mine().some((e) => e.courseId === courseId && e.status === 'active');
  }

  /** Whether the signed-in user may open the course's learning area. */
  hasAccess(courseId: number): boolean {
    return this.permissions.can('courses.view', courseId) || this.isEnrolled(courseId);
  }

  countForCourse(courseId: number): number {
    const counted = this.apiMode ? this.courses.byId(courseId)?.enrollmentCounts : undefined;
    return counted?.active ?? this.enrollments().filter((e) => e.courseId === courseId && e.status === 'active').length;
  }

  countAllForCourse(courseId: number): number {
    const counted = this.apiMode ? this.courses.byId(courseId)?.enrollmentCounts : undefined;
    return counted?.total ?? this.enrollments().filter((e) => e.courseId === courseId).length;
  }

  /** API mode: reloads the user's enrollments, and every enrollment for people who may see them. */
  async load(): Promise<void> {
    if (!this.apiMode) return;
    if (!this.auth.currentSession()) {
      this.apiMine.set([]);
      this.apiAll.set(null);
      return;
    }
    try {
      this.apiMine.set((await this.api.get<EnrollmentDto[]>('enrollments/mine')).map(fromEnrollmentDto));
      if (this.permissions.can('enrollments.view')) {
        const all: Enrollment[] = [];
        for (let page = 1; ; page++) {
          const result = await this.api.get<PagedResult<EnrollmentDto>>('admin/enrollments', { page, pageSize: PAGE_SIZE });
          all.push(...result.items.map(fromEnrollmentDto));
          if (all.length >= result.total || result.items.length === 0) break;
        }
        this.apiAll.set(all);
      } else {
        this.apiAll.set(null);
      }
    } catch {
      // Leave the lists as they were; the pages show what they have and the next load tries again.
    }
  }

  /** Enrolls the signed-in user in a free course (BR-PAY-03). */
  async enrollFree(course: Course): Promise<void> {
    if (this.apiMode) {
      await this.api.post(`enrollments/free/${encodeURIComponent(course.slug)}`);
      // The course's lessons and links open up for this user, so both lists change.
      await Promise.all([this.load(), this.courses.loadCatalog()]);
      return;
    }
    const user = this.auth.user();
    if (!user) throw new PermissionDeniedError();
    await this.grant(user.id, course.id);
  }

  /** Demo mode: activates a course for a user. `transactionId` is absent for free courses. In API mode the server grants access. */
  async grant(userId: string, courseId: number, transactionId?: string): Promise<Enrollment> {
    // Users enroll themselves through checkout. A real API would only do this after verifying the payment.
    const forSelf = userId === this.auth.user()?.id;
    if (!forSelf && !this.permissions.can('enrollments.edit') && !this.permissions.can('transactions.edit')) {
      throw new PermissionDeniedError();
    }
    const existing = this.enrollments().find((e) => e.userId === userId && e.courseId === courseId);
    return this.store.save({
      id: existing?.id ?? uid('enrollment'),
      userId,
      courseId,
      status: 'active',
      enrolledAt: new Date().toISOString(),
      transactionId,
    });
  }

  /** Demo mode: revokes the access a refunded payment granted. In API mode the server does this with the refund. */
  async revokeForTransaction(transactionId: string): Promise<void> {
    this.permissions.assert('transactions.edit');
    const enrollment = this.enrollments().find((e) => e.transactionId === transactionId);
    if (enrollment) await this.store.save({ ...enrollment, status: 'revoked' });
  }

  async setStatus(id: string, status: EnrollmentStatus): Promise<void> {
    this.permissions.assert('enrollments.edit');
    if (this.apiMode) {
      await this.api.patch(`admin/enrollments/${id}/status`, { statusCode: status });
      await Promise.all([this.load(), this.courses.loadCatalog()]);
      return;
    }
    const enrollment = this.enrollments().find((e) => e.id === id);
    if (enrollment) await this.store.save({ ...enrollment, status });
  }

  /** Clean-up when a course is deleted. In API mode the server removes them with the course. */
  async removeForCourse(courseId: number): Promise<void> {
    this.permissions.assert('courses.delete', courseId);
    if (this.apiMode) {
      this.apiAll.update((list) => list?.filter((e) => e.courseId !== courseId) ?? null);
      this.apiMine.update((list) => list.filter((e) => e.courseId !== courseId));
      return;
    }
    for (const e of this.enrollments().filter((x) => x.courseId === courseId)) {
      await this.store.remove(e.id);
    }
  }

  reset(): Promise<void> {
    // Enrollments in API mode live on the server; demo reset does not touch them.
    return this.apiMode ? Promise.resolve() : this.store.reset();
  }
}
