import { Injectable, computed, inject } from '@angular/core';
import { ENROLLMENT_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { Enrollment, EnrollmentStatus } from '../models/enrollment.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private readonly store = new EntityStore(inject(ENROLLMENT_REPOSITORY));
  private readonly auth = inject(AuthService);

  readonly ready = this.store.ready;
  readonly recovered = this.store.recovered;
  readonly enrollments = this.store.items;

  /** The signed-in user's enrollments, newest first. */
  readonly mine = computed(() => {
    const id = this.auth.user()?.id;
    return this.enrollments()
      .filter((e) => e.userId === id)
      .sort((a, b) => b.enrolledAt.localeCompare(a.enrolledAt));
  });

  isEnrolled(courseId: number): boolean {
    return this.mine().some((e) => e.courseId === courseId && e.status === 'active');
  }

  /** Whether the signed-in user may open the course's learning area. */
  hasAccess(courseId: number): boolean {
    return this.auth.isAdmin() || this.isEnrolled(courseId);
  }

  countForCourse(courseId: number): number {
    return this.enrollments().filter((e) => e.courseId === courseId && e.status === 'active').length;
  }

  countAllForCourse(courseId: number): number {
    return this.enrollments().filter((e) => e.courseId === courseId).length;
  }

  /** Activates a course for a user. `transactionId` is absent for free courses. */
  grant(userId: string, courseId: number, transactionId?: string): Promise<Enrollment> {
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

  async revokeForTransaction(transactionId: string): Promise<void> {
    const enrollment = this.enrollments().find((e) => e.transactionId === transactionId);
    if (enrollment) await this.setStatus(enrollment.id, 'revoked');
  }

  async setStatus(id: string, status: EnrollmentStatus): Promise<void> {
    const enrollment = this.enrollments().find((e) => e.id === id);
    if (enrollment) await this.store.save({ ...enrollment, status });
  }

  async removeForCourse(courseId: number): Promise<void> {
    for (const e of this.enrollments().filter((x) => x.courseId === courseId)) {
      await this.store.remove(e.id);
    }
  }

  reset(): Promise<void> {
    return this.store.reset();
  }
}
