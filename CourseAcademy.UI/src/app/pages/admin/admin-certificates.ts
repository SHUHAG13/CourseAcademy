import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Certificate, ExamSubmission, SubmissionStatus } from '../../core/models/certificate.model';
import { AuthService } from '../../core/services/auth.service';
import { downloadCertificatePdf } from '../../core/services/certificate-pdf';
import { CertificateService } from '../../core/services/certificate.service';
import { CourseService } from '../../core/services/course.service';
import { failureMessage } from '../../core/services/permission.service';
import { ToastService } from '../../core/services/toast.service';
import { LocalDatePipe, TranslatePipe } from '../../shared/pipes/translate.pipe';

/** Central exam reviews and issued certificates, for the courses this user may certify. */
@Component({
  selector: 'app-admin-certificates',
  imports: [TranslatePipe, LocalDatePipe, FormsModule, RouterLink],
  template: `
    <h1 class="h3 mb-3">{{ 'Certificates' | t }}</h1>

    <section class="mb-4" aria-labelledby="exams-title">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
        <h2 id="exams-title" class="h6 mb-0">{{ 'Central exam submissions' | t }}</h2>
        <label class="visually-hidden" for="sub-status">{{ 'Filter by status' | t }}</label>
        <select id="sub-status" class="form-select form-select-sm w-auto" [ngModel]="submissionStatus()" (ngModelChange)="submissionStatus.set($event)">
          <option value="submitted">{{ 'Waiting for review' | t }}</option>
          <option value="passed">{{ 'Passed' | t }}</option>
          <option value="failed">{{ 'Not passed' | t }}</option>
          <option value="all">{{ 'All statuses' | t }}</option>
        </select>
      </div>
      <div class="ca-card table-responsive">
        <table class="ca-table">
          <thead>
            <tr>
              <th scope="col">{{ 'Student' | t }}</th><th scope="col">{{ 'Course' | t }}</th><th scope="col">{{ 'Work' | t }}</th>
              <th scope="col">{{ 'Result' | t }}</th><th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
            </tr>
          </thead>
          <tbody>
            @for (s of submissions(); track s.id) {
              <tr>
                <td><span class="primary">{{ userName(s.userId) | t }}</span><span class="secondary">{{ s.submittedAt | ldate: 'd MMM y, h:mm a' }}</span></td>
                <td>
                  {{ courseTitle(s.courseId) | t }}
                  <span class="secondary">{{ certificates.rule(s.courseId).examTitle | t }} · {{ (s.examType === 'presentation' ? 'Presentation' : 'Project') | t }}</span>
                </td>
                <td class="work">
                  <a [href]="s.url" target="_blank" rel="noopener noreferrer">{{ s.url }}</a>
                  @if (s.notes) {
                    <span class="secondary">{{ s.notes }}</span>
                  }
                </td>
                <td class="text-nowrap">
                  @switch (s.status) {
                    @case ('submitted') { <span class="tag">{{ 'Waiting for review' | t }}</span> }
                    @case ('passed') { <span class="tag blue">{{ 'Passed: {0} of {1}' | t: [s.score, certificates.rule(s.courseId).maxScore] }}</span> }
                    @case ('failed') { <span class="tag red">{{ 'Not passed: {0} of {1}' | t: [s.score, certificates.rule(s.courseId).maxScore] }}</span> }
                  }
                </td>
                <td class="actions">
                  @if (s.status === 'submitted') {
                    <button type="button" class="btn btn-sm btn-primary" (click)="startReview(s)" [attr.aria-expanded]="reviewing() === s.id">
                      {{ 'Review' | t }}
                    </button>
                  }
                </td>
              </tr>
              @if (reviewing() === s.id) {
                <tr class="review-row">
                  <td colspan="5">
                    <form class="d-flex flex-wrap align-items-end gap-2" (ngSubmit)="saveReview(s)" novalidate>
                      <div>
                        <label class="form-label small" [for]="'score-' + s.id">
                          {{ 'Score (pass mark {0} of {1})' | t: [certificates.rule(s.courseId).passingScore, certificates.rule(s.courseId).maxScore] }}
                        </label>
                        <input [id]="'score-' + s.id" name="score" type="number" min="0" [max]="certificates.rule(s.courseId).maxScore"
                          class="form-control form-control-sm score" [(ngModel)]="score" />
                      </div>
                      <div class="flex-grow-1">
                        <label class="form-label small" [for]="'feedback-' + s.id">{{ 'Feedback for the student' | t }}</label>
                        <input [id]="'feedback-' + s.id" name="feedback" class="form-control form-control-sm" [(ngModel)]="feedback" maxlength="1000" />
                      </div>
                      <button type="button" class="btn btn-sm btn-outline" (click)="reviewing.set(null)">{{ 'Cancel' | t }}</button>
                      <button type="submit" class="btn btn-sm btn-primary" [disabled]="busy() || score === null">{{ 'Save result' | t }}</button>
                    </form>
                    @if (score !== null) {
                      <p class="small mt-2 mb-0" [class.text-success]="passes(s)" [class.text-danger]="!passes(s)">
                        {{ (passes(s) ? 'This score passes. The certificate is issued when you save.' : 'This score does not pass. The student can try again if attempts are left.') | t }}
                      </p>
                    }
                  </td>
                </tr>
              }
            } @empty {
              <tr><td colspan="5" class="text-center text-muted py-4">{{ 'No submissions here.' | t }}</td></tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    <section aria-labelledby="issued-title">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
        <h2 id="issued-title" class="h6 mb-0">{{ 'Issued certificates' | t }}</h2>
        <label class="visually-hidden" for="cert-q">{{ 'Search certificates' | t }}</label>
        <input id="cert-q" type="search" class="form-control form-control-sm w-auto" [placeholder]="'Search by ID, student or course' | t"
          [ngModel]="search()" (ngModelChange)="search.set($event)" />
      </div>
      <div class="ca-card table-responsive">
        <table class="ca-table">
          <thead>
            <tr>
              <th scope="col">{{ 'Certificate ID' | t }}</th><th scope="col">{{ 'Student' | t }}</th><th scope="col">{{ 'Course' | t }}</th>
              <th scope="col">{{ 'Status' | t }}</th><th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
            </tr>
          </thead>
          <tbody>
            @for (c of issued(); track c.id) {
              <tr>
                <td><code>{{ c.code }}</code><span class="secondary">{{ c.issuedAt | ldate: 'd MMM y' }}</span></td>
                <td>{{ c.userName | t }}</td>
                <td>
                  {{ c.courseTitle | t }}
                  <span class="secondary">
                    {{ (c.mode === 'exam' ? 'Central exam' : 'Course completion') | t }}
                    @if (c.scorePercent !== undefined) { · {{ c.scorePercent }}% }
                  </span>
                </td>
                <td>
                  @if (c.revoked) {
                    <span class="tag red">{{ 'Revoked' | t }}</span>
                    @if (c.revokedReason) { <span class="secondary">{{ c.revokedReason }}</span> }
                  } @else {
                    <span class="tag blue">{{ 'Valid' | t }}</span>
                  }
                </td>
                <td class="actions text-nowrap">
                  <button type="button" class="icon-btn" (click)="download(c)" [title]="'Download PDF' | t" [attr.aria-label]="'Download {0}' | t: [c.code]">
                    <i class="bi bi-file-earmark-arrow-down" aria-hidden="true"></i>
                  </button>
                  <a class="icon-btn ms-1" [routerLink]="['/verify', c.code]" [title]="'Verification page' | t" [attr.aria-label]="'Verify {0}' | t: [c.code]">
                    <i class="bi bi-shield-check" aria-hidden="true"></i>
                  </a>
                  @if (c.revoked) {
                    <button type="button" class="btn btn-sm btn-outline ms-1" (click)="restore(c)">{{ 'Restore' | t }}</button>
                  } @else {
                    <button type="button" class="btn btn-sm btn-outline ms-1" (click)="revoking.set(c.id); reason = ''">{{ 'Revoke' | t }}</button>
                  }
                </td>
              </tr>
              @if (revoking() === c.id) {
                <tr class="review-row">
                  <td colspan="5">
                    <form class="d-flex flex-wrap align-items-end gap-2" (ngSubmit)="revoke(c)" novalidate>
                      <div class="flex-grow-1">
                        <label class="form-label small" [for]="'reason-' + c.id">{{ 'Reason (shown on the verification page)' | t }}</label>
                        <input [id]="'reason-' + c.id" name="reason" class="form-control form-control-sm" [(ngModel)]="reason" maxlength="200" />
                      </div>
                      <button type="button" class="btn btn-sm btn-outline" (click)="revoking.set(null)">{{ 'Cancel' | t }}</button>
                      <button type="submit" class="btn btn-sm btn-primary" [disabled]="busy()">{{ 'Revoke certificate' | t }}</button>
                    </form>
                  </td>
                </tr>
              }
            } @empty {
              <tr><td colspan="5" class="text-center text-muted py-4">{{ 'No certificates found.' | t }}</td></tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: `
    .work { max-width: 280px; overflow-wrap: anywhere; }
    .score { width: 110px; }
    .review-row td { background: var(--ca-surface-muted); }
  `,
})
export class AdminCertificates {
  protected readonly certificates = inject(CertificateService);
  private readonly courses = inject(CourseService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  protected readonly submissionStatus = signal<SubmissionStatus | 'all'>('submitted');
  protected readonly search = signal('');
  protected readonly reviewing = signal<string | null>(null);
  protected readonly revoking = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected score: number | null = null;
  protected feedback = '';
  protected reason = '';

  protected readonly submissions = computed(() =>
    this.certificates
      .submissions()
      .filter((s) => this.certificates.canManage(s.courseId))
      .filter((s) => this.submissionStatus() === 'all' || s.status === this.submissionStatus())
      .sort((a, b) => b.submittedAt.localeCompare(a.submittedAt)),
  );

  protected readonly issued = computed(() => {
    const term = this.search().trim().toLowerCase();
    return this.certificates
      .certificates()
      .filter((c) => this.certificates.canManage(c.courseId))
      .filter((c) => !term || [c.code, c.userName, c.courseTitle].some((v) => v.toLowerCase().includes(term)))
      .sort((a, b) => b.issuedAt.localeCompare(a.issuedAt));
  });

  protected userName(id: string): string {
    return this.auth.users().find((u) => u.id === id)?.name ?? 'Former member';
  }

  protected courseTitle(id: number): string {
    return this.courses.byId(id)?.title ?? 'Deleted course';
  }

  protected passes(submission: ExamSubmission): boolean {
    return this.score !== null && this.score >= this.certificates.rule(submission.courseId).passingScore;
  }

  protected startReview(submission: ExamSubmission): void {
    this.reviewing.set(this.reviewing() === submission.id ? null : submission.id);
    this.score = null;
    this.feedback = '';
  }

  protected async saveReview(submission: ExamSubmission): Promise<void> {
    const passed = this.passes(submission);
    await this.run(async () => {
      await this.certificates.review(submission.id, Number(this.score), this.feedback);
      this.reviewing.set(null);
    }, passed ? 'Passed. The certificate was issued to {0}.' : 'The result was saved for {0}.', [this.userName(submission.userId)]);
  }

  protected async revoke(certificate: Certificate): Promise<void> {
    await this.run(async () => {
      await this.certificates.setRevoked(certificate.id, true, this.reason);
      this.revoking.set(null);
    }, 'Certificate {0} was revoked.', [certificate.code]);
  }

  protected async restore(certificate: Certificate): Promise<void> {
    await this.run(() => this.certificates.setRevoked(certificate.id, false), 'Certificate {0} is valid again.', [certificate.code]);
  }

  protected async download(certificate: Certificate): Promise<void> {
    try {
      await downloadCertificatePdf(certificate, `${location.origin}/verify/${certificate.code}`);
    } catch {
      this.toast.error('The PDF could not be created. Please try again.');
    }
  }

  private async run(action: () => Promise<unknown>, success: string, params: unknown[]): Promise<void> {
    this.busy.set(true);
    try {
      await action();
      this.toast.success(success, params);
    } catch (error) {
      this.toast.error(failureMessage(error, (error as Error).message));
    } finally {
      this.busy.set(false);
    }
  }
}
