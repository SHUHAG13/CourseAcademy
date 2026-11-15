import { Injectable, computed, inject } from '@angular/core';
import { CERTIFICATE_REPOSITORY, CERTIFICATION_RULE_REPOSITORY, SUBMISSION_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { Certificate, CertificationRule, ExamSubmission, defaultRule } from '../models/certificate.model';
import { Course } from '../models/course.model';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';
import { ProgressService } from './progress.service';

const CODE_ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
const LINK_PATTERN = /^https:\/\/\S+\.\S+$/i;
const NOTES_MAX = 1000;

/** Where the signed-in user stands with one course's certificate. */
export interface CertificationStatus {
  rule: CertificationRule;
  progress: number;
  progressMet: boolean;
  submissions: ExamSubmission[];
  /** Null when attempts are unlimited. */
  attemptsLeft: number | null;
  canSubmit: boolean;
  certificate?: Certificate;
}

/**
 * Certification: per-course rules, the central exam (a project or presentation
 * reviewed by an instructor), issuing certificates and verifying them by code.
 */
@Injectable({ providedIn: 'root' })
export class CertificateService {
  private readonly ruleStore = new EntityStore(inject(CERTIFICATION_RULE_REPOSITORY));
  private readonly submissionStore = new EntityStore(inject(SUBMISSION_REPOSITORY));
  private readonly certificateStore = new EntityStore(inject(CERTIFICATE_REPOSITORY));
  private readonly auth = inject(AuthService);
  private readonly courses = inject(CourseService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly permissions = inject(PermissionService);
  private readonly progress = inject(ProgressService);

  readonly ready = Promise.all([this.ruleStore.ready, this.submissionStore.ready, this.certificateStore.ready]);
  readonly recovered = computed(
    () => this.ruleStore.recovered() || this.submissionStore.recovered() || this.certificateStore.recovered(),
  );
  readonly submissions = this.submissionStore.items;
  readonly certificates = this.certificateStore.items;

  /** The signed-in user's certificates, newest first. */
  readonly mine = computed(() => {
    const id = this.auth.user()?.id;
    return this.certificates()
      .filter((c) => c.userId === id)
      .sort((a, b) => b.issuedAt.localeCompare(a.issuedAt));
  });

  rule(courseId: number): CertificationRule {
    return this.ruleStore.items().find((r) => r.id === courseId) ?? defaultRule(courseId);
  }

  canManage(courseId: number): boolean {
    return this.permissions.can('certificates.manage', courseId);
  }

  /** The user's valid certificate for a course, if any. */
  activeCertificate(userId: string, courseId: number): Certificate | undefined {
    return this.certificates().find((c) => c.userId === userId && c.courseId === courseId && !c.revoked);
  }

  submissionsOf(userId: string, courseId: number): ExamSubmission[] {
    return this.submissions()
      .filter((s) => s.userId === userId && s.courseId === courseId)
      .sort((a, b) => b.submittedAt.localeCompare(a.submittedAt));
  }

  /** Public lookup for the verification page. Codes are not case-sensitive. */
  findByCode(code: string): Certificate | undefined {
    const normalised = code.trim().toUpperCase();
    return this.certificates().find((c) => c.code === normalised);
  }

  status(course: Course): CertificationStatus | null {
    const user = this.auth.user();
    if (!user) return null;
    const rule = this.rule(course.id);
    const progress = this.progress.percentFor(user.id, course);
    const submissions = this.submissionsOf(user.id, course.id);
    const certificate = this.activeCertificate(user.id, course.id);
    const attemptsLeft = rule.maxAttempts ? Math.max(0, rule.maxAttempts - submissions.length) : null;
    const canSubmit =
      rule.enabled &&
      rule.mode === 'exam' &&
      progress >= rule.minProgress &&
      !certificate &&
      !submissions.some((s) => s.status !== 'failed') &&
      attemptsLeft !== 0;
    return { rule, progress, progressMet: progress >= rule.minProgress, submissions, attemptsLeft, canSubmit, certificate };
  }

  // ── Students ────────────────────────────────────────────────

  /** Issues a completion certificate when the course's requirement is met. Safe to call repeatedly. */
  async evaluate(course: Course): Promise<Certificate | undefined> {
    const user = this.auth.user();
    if (!user || !this.enrollments.isEnrolled(course.id)) return undefined;
    const rule = this.rule(course.id);
    if (!rule.enabled || rule.mode !== 'completion') return this.activeCertificate(user.id, course.id);
    if (this.progress.percentFor(user.id, course) < rule.minProgress) return undefined;
    return this.issue(user.id, course);
  }

  async submitExam(course: Course, url: string, notes: string): Promise<ExamSubmission> {
    const user = this.auth.user();
    if (!user || !this.permissions.can('learn.view') || !this.enrollments.isEnrolled(course.id)) {
      throw new PermissionDeniedError();
    }
    const status = this.status(course)!;
    if (!status.canSubmit) {
      throw new Error(
        !status.progressMet
          ? 'Complete the course before the central exam.'
          : status.attemptsLeft === 0
            ? 'You have used all your attempts.'
            : 'You cannot hand in the exam right now.',
      );
    }
    const link = url.trim();
    if (!LINK_PATTERN.test(link)) throw new Error('Enter a link that starts with https://.');
    if (notes.length > NOTES_MAX) throw new Error('This is too long. Please shorten it.');

    return this.submissionStore.save({
      id: uid('submission'),
      userId: user.id,
      courseId: course.id,
      examType: status.rule.examType,
      url: link,
      notes: notes.trim(),
      submittedAt: new Date().toISOString(),
      status: 'submitted',
    });
  }

  // ── Instructors and admins ──────────────────────────────────

  async saveRule(rule: CertificationRule): Promise<CertificationRule> {
    if (!this.canManage(rule.id)) throw new PermissionDeniedError();
    const whole = (n: number) => Number.isInteger(n);
    if (!whole(rule.minProgress) || rule.minProgress < 1 || rule.minProgress > 100) {
      throw new Error('Required progress must be between 1 and 100%.');
    }
    if (rule.mode === 'exam') {
      if (!rule.examTitle.trim()) throw new Error('Give the central exam a title.');
      if (!whole(rule.maxScore) || rule.maxScore < 1) throw new Error('The maximum score must be 1 or more.');
      if (!whole(rule.passingScore) || rule.passingScore < 1 || rule.passingScore > rule.maxScore) {
        throw new Error('The pass mark must be between 1 and the maximum score.');
      }
      if (!whole(rule.maxAttempts) || rule.maxAttempts < 0) throw new Error('Attempts must be 0 (unlimited) or more.');
    }
    return this.ruleStore.save({ ...rule, examTitle: rule.examTitle.trim(), examInstructions: rule.examInstructions.trim() });
  }

  /** Scores a submission. A passing score issues the certificate straight away. */
  async review(submissionId: string, score: number, feedback: string): Promise<ExamSubmission> {
    const submission = this.submissions().find((s) => s.id === submissionId);
    if (!submission) throw new Error('Submission not found.');
    if (!this.canManage(submission.courseId)) throw new PermissionDeniedError();
    if (submission.status !== 'submitted') throw new Error('This submission has already been reviewed.');

    const rule = this.rule(submission.courseId);
    if (!Number.isFinite(score) || score < 0 || score > rule.maxScore) {
      throw new Error('Enter a score between 0 and the maximum score.');
    }
    const passed = score >= rule.passingScore;
    const reviewed = await this.submissionStore.save({
      ...submission,
      status: passed ? 'passed' : 'failed',
      score,
      feedback: feedback.trim(),
      reviewedBy: this.auth.user()!.id,
      reviewedAt: new Date().toISOString(),
    });

    const course = this.courses.byId(submission.courseId);
    if (passed && course) {
      await this.issue(submission.userId, course, { scorePercent: Math.round((score / rule.maxScore) * 100), examType: rule.examType });
    }
    return reviewed;
  }

  async setRevoked(certificateId: string, revoked: boolean, reason = ''): Promise<Certificate> {
    const certificate = this.certificates().find((c) => c.id === certificateId);
    if (!certificate) throw new Error('Certificate not found.');
    if (!this.canManage(certificate.courseId)) throw new PermissionDeniedError();
    if (!revoked && this.activeCertificate(certificate.userId, certificate.courseId)) {
      throw new Error('This student already has a valid certificate for the course.');
    }
    return this.certificateStore.save({ ...certificate, revoked, revokedReason: revoked ? reason.trim() || undefined : undefined });
  }

  async reset(): Promise<void> {
    await Promise.all([this.ruleStore.reset(), this.submissionStore.reset(), this.certificateStore.reset()]);
  }

  /** Creates the certificate, or returns the valid one the user already has. */
  private async issue(
    userId: string,
    course: Course,
    exam: Pick<Certificate, 'scorePercent' | 'examType'> = {},
  ): Promise<Certificate> {
    const existing = this.activeCertificate(userId, course.id);
    if (existing) return existing;
    return this.certificateStore.save({
      id: uid('certificate'),
      code: this.newCode(),
      userId,
      courseId: course.id,
      userName: this.auth.users().find((u) => u.id === userId)?.name ?? 'Student',
      courseTitle: course.title,
      instructorName: this.courses.instructor(course.instructorId)?.name ?? '',
      mode: exam.examType ? 'exam' : 'completion',
      ...exam,
      issuedAt: new Date().toISOString(),
      revoked: false,
    });
  }

  private newCode(): string {
    let code: string;
    do {
      const bytes = crypto.getRandomValues(new Uint8Array(6));
      const suffix = Array.from(bytes, (b) => CODE_ALPHABET[b % CODE_ALPHABET.length]).join('');
      code = `CA-${new Date().getFullYear()}-${suffix}`;
    } while (this.findByCode(code));
    return code;
  }
}
