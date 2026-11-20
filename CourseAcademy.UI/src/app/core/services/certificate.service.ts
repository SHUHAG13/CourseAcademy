import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { API_MODE } from '../api/api-mode';
import { ApiError } from '../api/api-error';
import { ApiService } from '../api/api.service';
import { PagedResult } from '../api/api.models';
import {
  CertificateDto,
  CertificationStatusDto,
  ReviewResultDto,
  RuleDto,
  SubmissionDto,
  VerifyCertificateDto,
  fromCertificateDto,
  fromRuleDto,
  fromSubmissionDto,
  fromVerifyDto,
  toRuleRequest,
} from '../api/certification.models';
import { CERTIFICATE_REPOSITORY, CERTIFICATION_RULE_REPOSITORY, SUBMISSION_REPOSITORY } from '../data/repositories';
import { EntityStore, uid } from '../data/repository';
import { Certificate, CertificationRule, ExamSubmission, SubmissionExam, defaultRule } from '../models/certificate.model';
import { Course } from '../models/course.model';
import { AuthService } from './auth.service';
import { CourseService } from './course.service';
import { EnrollmentService } from './enrollment.service';
import { PermissionDeniedError, PermissionService } from './permission.service';
import { ProgressService } from './progress.service';

const CODE_ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
const LINK_PATTERN = /^https:\/\/\S+\.\S+$/i;
const NOTES_MAX = 1000;
const PAGE_SIZE = 100;

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
  /** The user's revoked certificate when none is valid. Only Restore makes it valid again (BR-CERT-17). */
  revokedCertificate?: Certificate;
}

/**
 * Certification: per-course rules, the central exam (a project or presentation
 * reviewed by an instructor), issuing certificates and verifying them by code.
 * - API mode: the API keeps rules, hand-ins and certificates and issues them; pages load what they show
 *   (`loadStatus`, `loadRule`, `loadAdmin`, `verify`).
 * - Demo mode: everything lives in browser storage.
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
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);

  // API mode: what the pages loaded.
  private readonly apiRules = signal(new Map<number, CertificationRule>());
  private readonly apiStatus = signal(new Map<number, CertificationStatus>());
  private readonly apiMine = signal<Certificate[]>([]);
  private readonly apiSubmissions = signal<ExamSubmission[]>([]);
  private readonly apiCertificates = signal<Certificate[]>([]);

  readonly ready: Promise<unknown> = this.apiMode
    ? this.auth.ready.then(() => this.loadMine())
    : Promise.all([this.ruleStore.ready, this.submissionStore.ready, this.certificateStore.ready]);
  readonly recovered = computed(
    () => !this.apiMode && (this.ruleStore.recovered() || this.submissionStore.recovered() || this.certificateStore.recovered()),
  );
  /** Hand-ins of the courses this user may certify (API mode: after `loadAdmin`). */
  readonly submissions = computed(() => (this.apiMode ? this.apiSubmissions() : this.submissionStore.items()));
  /** Issued certificates of the courses this user may certify (API mode: after `loadAdmin`). */
  readonly certificates = computed(() => (this.apiMode ? this.apiCertificates() : this.certificateStore.items()));

  /** The signed-in user's certificates, newest first. */
  readonly mine = computed(() => {
    if (this.apiMode) return this.apiMine();
    const id = this.auth.user()?.id;
    return this.certificates()
      .filter((c) => c.userId === id)
      .sort((a, b) => b.issuedAt.localeCompare(a.issuedAt));
  });

  constructor() {
    if (this.apiMode) {
      // Another account signs in: forget what was loaded for the previous one.
      let loadedFor: number | null | undefined;
      effect(() => {
        const id = this.auth.currentSession()?.id ?? null;
        untracked(() => {
          if (loadedFor !== undefined && loadedFor !== id) {
            this.apiStatus.set(new Map());
            this.apiSubmissions.set([]);
            this.apiCertificates.set([]);
            void this.loadMine();
          }
          loadedFor = id;
        });
      });
    }
  }

  /** The course's rule. API mode: the one loaded with `loadRule` or `loadStatus`, else the default. */
  rule(courseId: number): CertificationRule {
    if (this.apiMode) return this.apiRules().get(courseId) ?? defaultRule(courseId);
    return this.ruleStore.items().find((r) => r.id === courseId) ?? defaultRule(courseId);
  }

  /** Reads the course's rule; in API mode from the server (managers only). */
  async loadRule(courseId: number): Promise<CertificationRule> {
    if (!this.apiMode) return this.rule(courseId);
    const rule = fromRuleDto(await this.api.get<RuleDto>(`admin/courses/${courseId}/certification-rule`));
    this.rememberRule(rule);
    return rule;
  }

  /** The exam settings a submission is reviewed against. */
  examOf(submission: ExamSubmission): SubmissionExam {
    if (submission.exam) return submission.exam;
    const rule = this.rule(submission.courseId);
    return { title: rule.examTitle, maxScore: rule.maxScore, passingScore: rule.passingScore };
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

  /** Demo mode: the user's revoked certificate for a course, when none is valid. */
  private revokedCertificate(userId: string, courseId: number): Certificate | undefined {
    if (this.activeCertificate(userId, courseId)) return undefined;
    return this.certificates().find((c) => c.userId === userId && c.courseId === courseId && c.revoked);
  }

  /** Demo mode: public lookup for the verification page. Codes are not case-sensitive. */
  findByCode(code: string): Certificate | undefined {
    const normalised = code.trim().toUpperCase();
    return this.certificates().find((c) => c.code === normalised);
  }

  /** Public lookup for the verification page (BR-CERT-13). Undefined when no certificate has the code. */
  async verify(code: string): Promise<Certificate | undefined> {
    const normalised = code.trim().toUpperCase();
    if (!normalised) return undefined;
    if (!this.apiMode) return this.findByCode(normalised);
    try {
      return fromVerifyDto(await this.api.get<VerifyCertificateDto>(`certificates/verify/${encodeURIComponent(normalised)}`));
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) return undefined;
      throw error;
    }
  }

  /** Where the signed-in user stands. API mode: what `loadStatus` read last, or null before that. */
  status(course: Course): CertificationStatus | null {
    if (this.apiMode) return this.apiStatus().get(course.id) ?? null;
    const user = this.auth.user();
    if (!user) return null;
    const rule = this.rule(course.id);
    const progress = this.progress.percentFor(user.id, course);
    const submissions = this.submissionsOf(user.id, course.id);
    const certificate = this.activeCertificate(user.id, course.id);
    const revokedCertificate = this.revokedCertificate(user.id, course.id);
    const attemptsLeft = rule.maxAttempts ? Math.max(0, rule.maxAttempts - submissions.length) : null;
    const canSubmit =
      rule.enabled &&
      rule.mode === 'exam' &&
      progress >= rule.minProgress &&
      !certificate &&
      !revokedCertificate &&
      !submissions.some((s) => s.status !== 'failed') &&
      attemptsLeft !== 0;
    return {
      rule,
      progress,
      progressMet: progress >= rule.minProgress,
      submissions,
      attemptsLeft,
      canSubmit,
      certificate,
      revokedCertificate,
    };
  }

  /** API mode: reads the signed-in user's status in one course. Null when the user has no access to it. */
  async loadStatus(course: Course): Promise<CertificationStatus | null> {
    if (!this.apiMode) return this.status(course);
    if (!this.auth.currentSession()) return null;
    try {
      const dto = await this.api.get<CertificationStatusDto>(`me/courses/${course.id}/certification`);
      const status: CertificationStatus = {
        rule: fromRuleDto(dto.rule),
        progress: dto.progress,
        progressMet: dto.progressMet,
        submissions: dto.submissions.map(fromSubmissionDto),
        attemptsLeft: dto.attemptsLeft,
        canSubmit: dto.canSubmit,
        certificate: dto.certificate ? fromCertificateDto(dto.certificate) : undefined,
        revokedCertificate: dto.revokedCertificate ? fromCertificateDto(dto.revokedCertificate) : undefined,
      };
      this.rememberRule(status.rule);
      this.apiStatus.update((map) => new Map(map).set(course.id, status));
      return status;
    } catch (error) {
      if (error instanceof ApiError && (error.status === 403 || error.status === 404)) return null;
      throw error;
    }
  }

  /** API mode: reloads the signed-in user's certificates. */
  async loadMine(): Promise<void> {
    if (!this.apiMode) return;
    if (!this.auth.currentSession()) {
      this.apiMine.set([]);
      return;
    }
    try {
      this.apiMine.set((await this.api.get<CertificateDto[]>('me/certificates')).map(fromCertificateDto));
    } catch {
      // Leave the list as it was; the next load tries again.
    }
  }

  /** API mode: loads every hand-in and certificate of the courses this user may certify, for the admin page. */
  async loadAdmin(): Promise<void> {
    if (!this.apiMode) return;
    const [submissions, certificates] = await Promise.all([
      this.allPages<SubmissionDto>('admin/exam-submissions', { status: 'all' }),
      this.allPages<CertificateDto>('admin/certificates', {}),
    ]);
    this.apiSubmissions.set(submissions.map(fromSubmissionDto));
    this.apiCertificates.set(certificates.map(fromCertificateDto));
  }

  // ── Students ────────────────────────────────────────────────

  /** Issues a completion certificate when the course's requirement is met. Safe to call repeatedly. */
  async evaluate(course: Course): Promise<Certificate | undefined> {
    if (this.apiMode) {
      const dto = await this.api.post<CertificateDto | null>(`me/courses/${course.id}/certificate`);
      await Promise.all([this.loadStatus(course), this.loadMine()]);
      return dto ? fromCertificateDto(dto) : undefined;
    }
    const user = this.auth.user();
    if (!user || !this.enrollments.isEnrolled(course.id)) return undefined;
    const rule = this.rule(course.id);
    if (!rule.enabled || rule.mode !== 'completion') return this.activeCertificate(user.id, course.id);
    if (this.progress.percentFor(user.id, course) < rule.minProgress) return undefined;
    return this.issue(user.id, course);
  }

  async submitExam(course: Course, url: string, notes: string): Promise<ExamSubmission> {
    if (this.apiMode) {
      const dto = await this.api.post<SubmissionDto>(`me/courses/${course.id}/exam-submissions`, { url, notes });
      await this.loadStatus(course);
      return fromSubmissionDto(dto);
    }
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
    if (notes.trim().length > NOTES_MAX) throw new Error('This is too long. Please shorten it.');

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
    if (this.apiMode) {
      const saved = fromRuleDto(await this.api.put<RuleDto>(`admin/courses/${rule.id}/certification-rule`, toRuleRequest(rule)));
      this.rememberRule(saved);
      return saved;
    }
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
    if (this.apiMode) {
      const result = await this.api.post<ReviewResultDto>(`admin/exam-submissions/${submissionId}/review`, { score, feedback });
      await this.loadAdmin();
      return fromSubmissionDto(result.submission);
    }
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
    if (this.apiMode) {
      const dto = revoked
        ? await this.api.post<CertificateDto>(`admin/certificates/${certificateId}/revoke`, { reason })
        : await this.api.post<CertificateDto>(`admin/certificates/${certificateId}/restore`);
      await Promise.all([this.loadAdmin(), this.loadMine()]);
      return fromCertificateDto(dto);
    }
    const certificate = this.certificates().find((c) => c.id === certificateId);
    if (!certificate) throw new Error('Certificate not found.');
    if (!this.canManage(certificate.courseId)) throw new PermissionDeniedError();
    if (!revoked && this.activeCertificate(certificate.userId, certificate.courseId)) {
      throw new Error('This student already has a valid certificate for the course.');
    }
    return this.certificateStore.save({ ...certificate, revoked, revokedReason: revoked ? reason.trim() || undefined : undefined });
  }

  async reset(): Promise<void> {
    // Certificates in API mode live on the server; demo reset does not touch them.
    if (this.apiMode) return;
    await Promise.all([this.ruleStore.reset(), this.submissionStore.reset(), this.certificateStore.reset()]);
  }

  private rememberRule(rule: CertificationRule): void {
    this.apiRules.update((map) => new Map(map).set(rule.id, rule));
  }

  private async allPages<T>(path: string, params: Record<string, string>): Promise<T[]> {
    const all: T[] = [];
    for (let page = 1; ; page++) {
      const result = await this.api.get<PagedResult<T>>(path, { ...params, page, pageSize: PAGE_SIZE });
      all.push(...result.items);
      if (all.length >= result.total || result.items.length === 0) return all;
    }
  }

  /**
   * Demo mode: creates the certificate, or returns the valid one the user already has.
   * Nothing is issued while the user's certificate is revoked (BR-CERT-17).
   */
  private async issue(
    userId: string,
    course: Course,
    exam: Pick<Certificate, 'scorePercent' | 'examType'> = {},
  ): Promise<Certificate | undefined> {
    const existing = this.activeCertificate(userId, course.id);
    if (existing) return existing;
    if (this.revokedCertificate(userId, course.id)) return undefined;
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
