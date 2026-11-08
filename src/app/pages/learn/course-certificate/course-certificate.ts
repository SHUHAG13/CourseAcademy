import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Certificate } from '../../../core/models/certificate.model';
import { Course } from '../../../core/models/course.model';
import { downloadCertificatePdf } from '../../../core/services/certificate-pdf';
import { CertificateService } from '../../../core/services/certificate.service';
import { failureMessage } from '../../../core/services/permission.service';
import { ToastService } from '../../../core/services/toast.service';
import { LocalDatePipe, TranslatePipe } from '../../../shared/pipes/translate.pipe';

/** The course's certificate: what it needs, the central exam hand-in, and the download once earned. */
@Component({
  selector: 'app-course-certificate',
  imports: [TranslatePipe, LocalDatePipe, FormsModule, RouterLink],
  template: `
    @if (status(); as s) {
      @if (s.rule.enabled) {
        <section class="cert ca-card" aria-labelledby="cert-title">
          <div class="d-flex align-items-center gap-2 mb-2">
            <span class="icon-tile sm orange"><i class="bi bi-award" aria-hidden="true"></i></span>
            <h2 id="cert-title" class="h5 mb-0">{{ 'Certificate' | t }}</h2>
          </div>

          @if (s.certificate; as cert) {
            <div class="earned">
              <p class="mb-1 fw-semibold"><i class="bi bi-patch-check-fill" aria-hidden="true"></i> {{ 'You have earned your certificate!' | t }}</p>
              <p class="small text-muted mb-2">
                {{ 'Issued {0} · Certificate ID {1}' | t: [(cert.issuedAt | ldate: 'd MMM y'), cert.code] }}
                @if (cert.scorePercent !== undefined) {
                  · {{ 'Exam score {0}%' | t: [cert.scorePercent] }}
                }
              </p>
              <div class="d-flex flex-wrap gap-2">
                <button type="button" class="btn btn-primary btn-sm" [disabled]="downloading()" (click)="download(cert)">
                  <i class="bi bi-file-earmark-arrow-down" aria-hidden="true"></i> {{ 'Download PDF' | t }}
                </button>
                <a class="btn btn-outline btn-sm" [routerLink]="['/verify', cert.code]">
                  <i class="bi bi-shield-check" aria-hidden="true"></i> {{ 'Verification page' | t }}
                </a>
              </div>
            </div>
          } @else {
            <p class="small text-muted">
              @if (s.rule.mode === 'exam') {
                {{ 'Complete {0}% of the course, then pass the central exam to get your certificate.' | t: [s.rule.minProgress] }}
              } @else {
                {{ 'Complete {0}% of the course to get your certificate.' | t: [s.rule.minProgress] }}
              }
            </p>

            <ol class="steps">
              <li [class.done]="s.progressMet">
                <i class="bi" [class]="s.progressMet ? 'bi-check-circle-fill' : 'bi-circle'" aria-hidden="true"></i>
                <span>{{ 'Course progress: {0}% of {1}% needed' | t: [s.progress, s.rule.minProgress] }}</span>
              </li>
              @if (s.rule.mode === 'exam') {
                <li>
                  <i class="bi bi-circle" aria-hidden="true"></i>
                  <span>
                    {{ 'Central exam: {0} ({1})' | t: [s.rule.examTitle, examLabel()] }}
                    <small class="d-block text-muted">
                      {{ 'Pass mark {0} of {1}' | t: [s.rule.passingScore, s.rule.maxScore] }} ·
                      @if (s.attemptsLeft === null) {
                        {{ 'Unlimited attempts' | t }}
                      } @else {
                        {{ '{0} attempt(s) left' | t: [s.attemptsLeft] }}
                      }
                    </small>
                  </span>
                </li>
              }
            </ol>

            @for (sub of s.submissions; track sub.id) {
              <div class="submission" [class.failed]="sub.status === 'failed'">
                <div class="d-flex flex-wrap justify-content-between gap-2">
                  <strong class="small">
                    @switch (sub.status) {
                      @case ('submitted') { <i class="bi bi-hourglass-split" aria-hidden="true"></i> {{ 'Waiting for review' | t }} }
                      @case ('failed') { <i class="bi bi-x-circle" aria-hidden="true"></i> {{ 'Not passed: {0} of {1}' | t: [sub.score, s.rule.maxScore] }} }
                      @case ('passed') { <i class="bi bi-check-circle" aria-hidden="true"></i> {{ 'Passed: {0} of {1}' | t: [sub.score, s.rule.maxScore] }} }
                    }
                  </strong>
                  <small class="text-muted">{{ 'Handed in {0}' | t: [(sub.submittedAt | ldate: 'd MMM y, h:mm a')] }}</small>
                </div>
                <a class="small" [href]="sub.url" target="_blank" rel="noopener noreferrer">{{ sub.url }}</a>
                @if (sub.feedback) {
                  <p class="small mb-0 mt-1"><strong>{{ 'Feedback' | t }}:</strong> {{ sub.feedback }}</p>
                }
              </div>
            }

            @if (s.canSubmit) {
              <form class="mt-3" (ngSubmit)="submit()" novalidate>
                @if (s.rule.examInstructions) {
                  <p class="small instructions">{{ s.rule.examInstructions | t }}</p>
                }
                <label for="exam-url" class="form-label">{{ (s.rule.examType === 'presentation' ? 'Link to your slides or recording' : 'Link to your project') | t }}</label>
                <input id="exam-url" name="url" type="url" class="form-control form-control-sm mb-2" [(ngModel)]="url"
                  placeholder="https://" required />
                <label for="exam-notes" class="form-label">{{ 'Notes for the reviewer (optional)' | t }}</label>
                <textarea id="exam-notes" name="notes" class="form-control form-control-sm" rows="2" [(ngModel)]="notes" maxlength="1000"></textarea>
                <button type="submit" class="btn btn-primary btn-sm mt-2" [disabled]="busy() || !url.trim()">
                  <i class="bi bi-send" aria-hidden="true"></i> {{ 'Hand in for review' | t }}
                </button>
              </form>
            } @else if (s.rule.mode === 'completion' && s.progressMet) {
              <button type="button" class="btn btn-primary btn-sm" [disabled]="busy()" (click)="claim()">{{ 'Get my certificate' | t }}</button>
            } @else if (s.rule.mode === 'exam' && !s.progressMet) {
              <p class="small text-muted mb-0">{{ 'The central exam opens when you reach the required progress.' | t }}</p>
            } @else if (outOfAttempts()) {
              <p class="small text-muted mb-0">{{ 'You have used all your attempts. Please contact your instructor.' | t }}</p>
            }
          }
        </section>
      }
    }
  `,
  styles: `
    .cert { padding: 1.1rem 1.25rem; }
    .earned { padding: 0.85rem 1rem; border-radius: var(--ca-radius); background: var(--ca-success-50); }
    .earned .bi-patch-check-fill { color: var(--ca-success); }
    .steps { display: grid; gap: 0.5rem; margin: 0 0 0.75rem; padding: 0; list-style: none; font-size: 0.9rem; }
    .steps li { display: flex; gap: 0.5rem; align-items: flex-start; }
    .steps .bi { color: var(--ca-muted); margin-top: 0.15rem; }
    .steps .done .bi { color: var(--ca-success); }
    .submission { margin-top: 0.5rem; padding: 0.6rem 0.8rem; border: 1px solid var(--ca-border); border-radius: var(--ca-radius-sm); overflow-wrap: anywhere; }
    .submission.failed { border-color: var(--ca-danger); background: var(--ca-danger-50); }
    .instructions { padding: 0.6rem 0.8rem; border-left: 3px solid var(--ca-orange); background: var(--ca-orange-50); }
  `,
})
export class CourseCertificate {
  private readonly certificates = inject(CertificateService);
  private readonly toast = inject(ToastService);

  readonly course = input.required<Course>();

  protected readonly status = computed(() => this.certificates.status(this.course()));
  protected readonly examLabel = computed(() => (this.status()?.rule.examType === 'presentation' ? 'Presentation' : 'Project'));
  /** Every attempt used and none waiting for review. */
  protected readonly outOfAttempts = computed(() => {
    const s = this.status();
    return s?.attemptsLeft === 0 && !s.submissions.some((sub) => sub.status === 'submitted');
  });
  protected readonly busy = signal(false);
  protected readonly downloading = signal(false);
  protected url = '';
  protected notes = '';

  protected async submit(): Promise<void> {
    this.busy.set(true);
    try {
      await this.certificates.submitExam(this.course(), this.url, this.notes);
      this.url = '';
      this.notes = '';
      this.toast.success('Your work was handed in. You will see the result here after review.');
    } catch (error) {
      this.toast.error(failureMessage(error, (error as Error).message));
    } finally {
      this.busy.set(false);
    }
  }

  protected async claim(): Promise<void> {
    this.busy.set(true);
    try {
      if (await this.certificates.evaluate(this.course())) this.toast.success('Congratulations! Your certificate is ready.');
    } catch {
      this.toast.error('The certificate could not be saved. Browser storage may be full or blocked.');
    } finally {
      this.busy.set(false);
    }
  }

  protected async download(certificate: Certificate): Promise<void> {
    this.downloading.set(true);
    try {
      await downloadCertificatePdf(certificate, `${location.origin}/verify/${certificate.code}`);
    } catch {
      this.toast.error('The PDF could not be created. Please try again.');
    } finally {
      this.downloading.set(false);
    }
  }
}
