import { Component, computed, effect, inject, input, linkedSignal, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { API_MODE } from '../../core/api/api-mode';
import { Certificate } from '../../core/models/certificate.model';
import { CertificateService } from '../../core/services/certificate.service';
import { LocalDatePipe, TranslatePipe } from '../../shared/pipes/translate.pipe';

/** Public page where anyone can check a certificate by its ID. */
@Component({
  selector: 'app-verify',
  imports: [TranslatePipe, LocalDatePipe, FormsModule],
  template: `
    <section class="page-hero">
      <div class="container">
        <h1>{{ 'Verify a certificate' | t }}</h1>
        <p class="mb-0">{{ 'Enter the certificate ID printed at the bottom of a Course Academy certificate.' | t }}</p>
      </div>
    </section>

    <section class="section-tight">
      <div class="container verify">
        <form class="ca-card search" (ngSubmit)="check()" novalidate>
          <label for="cert-code" class="form-label">{{ 'Certificate ID' | t }}</label>
          <div class="d-flex gap-2">
            <input id="cert-code" name="code" class="form-control" [(ngModel)]="draft" placeholder="CA-2026-XXXXXX" autocomplete="off" />
            <button type="submit" class="btn btn-primary" [disabled]="!draft().trim()">{{ 'Verify' | t }}</button>
          </div>
        </form>

        @if (code()) {
          @if (!checked()) {
            <p class="small text-muted mt-3" role="status">{{ 'Checking the certificate…' | t }}</p>
          } @else if (failed()) {
            <div class="result ca-card missing" role="status">
              <p class="verdict mb-0"><i class="bi bi-wifi-off" aria-hidden="true"></i> {{ 'The certificate could not be checked. Please try again.' | t }}</p>
            </div>
          } @else if (certificate(); as c) {
            <div class="result ca-card" [class.valid]="!c.revoked" [class.revoked]="c.revoked" role="status">
              <p class="verdict">
                <i class="bi" [class]="c.revoked ? 'bi-x-octagon-fill' : 'bi-patch-check-fill'" aria-hidden="true"></i>
                {{ (c.revoked ? 'This certificate has been revoked' : 'This certificate is valid') | t }}
              </p>
              @if (c.revoked && c.revokedReason) {
                <p class="small mb-3">{{ 'Reason' | t }}: {{ c.revokedReason }}</p>
              }
              <dl>
                <dt>{{ 'Awarded to' | t }}</dt><dd>{{ c.userName | t }}</dd>
                <dt>{{ 'Course' | t }}</dt><dd>{{ c.courseTitle | t }}</dd>
                <dt>{{ 'Type' | t }}</dt>
                <dd>
                  @if (c.mode === 'exam') {
                    {{ (c.examType === 'presentation' ? 'Central exam: presentation' : 'Central exam: project') | t }}
                    @if (c.scorePercent !== undefined) { · {{ 'score {0}%' | t: [c.scorePercent] }} }
                  } @else {
                    {{ 'Course completion' | t }}
                  }
                </dd>
                @if (c.instructorName) {
                  <dt>{{ 'Instructor' | t }}</dt><dd>{{ c.instructorName | t }}</dd>
                }
                <dt>{{ 'Issued' | t }}</dt><dd>{{ c.issuedAt | ldate: 'd MMMM y' }}</dd>
                <dt>{{ 'Certificate ID' | t }}</dt><dd><code>{{ c.code }}</code></dd>
              </dl>
            </div>
          } @else {
            <div class="result ca-card missing" role="status">
              <p class="verdict"><i class="bi bi-question-octagon-fill" aria-hidden="true"></i> {{ 'No certificate found with ID {0}' | t: [code()] }}</p>
              <p class="small mb-0">{{ 'Check the ID for typing mistakes. It looks like CA-2026-K7Q2XM.' | t }}</p>
            </div>
          }
        }

        @if (!apiMode) {
          <p class="small text-muted mt-3">
            {{ 'Frontend demo: certificates are stored in this browser, so only certificates issued here can be verified.' | t }}
          </p>
        }
      </div>
    </section>
  `,
  styles: `
    .verify { max-width: 720px; }
    .search, .result { padding: 1.25rem; }
    .result { margin-top: 1rem; border-left: 5px solid var(--ca-border-strong); }
    .result.valid { border-left-color: var(--ca-success); }
    .result.valid .verdict .bi { color: var(--ca-success); }
    .result.revoked { border-left-color: var(--ca-danger); background: var(--ca-danger-50); }
    .result.revoked .verdict .bi, .result.missing .verdict .bi { color: var(--ca-danger); }
    .verdict { display: flex; gap: 0.5rem; align-items: center; margin-bottom: 0.75rem; font-size: 1.1rem; font-weight: 700; }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: 0.4rem 1.25rem; margin: 0; }
    dt { font-weight: 500; color: var(--ca-muted); }
    dd { margin: 0; font-weight: 600; }
  `,
})
export class Verify {
  private readonly certificates = inject(CertificateService);
  private readonly router = inject(Router);
  protected readonly apiMode = inject(API_MODE);

  /** Bound from the optional `:code` route param. */
  readonly code = input('');

  protected readonly draft = linkedSignal(() => this.code());
  /** API mode: the server's answer for a code. */
  private readonly answer = signal<{ code: string; certificate?: Certificate; failed?: boolean } | null>(null);
  protected readonly checked = computed(() => !this.apiMode || this.answer()?.code === this.code());
  protected readonly failed = computed(() => this.apiMode && !!this.answer()?.failed);
  protected readonly certificate = computed(() =>
    this.apiMode ? this.answer()?.certificate : this.certificates.findByCode(this.code()),
  );

  constructor() {
    effect(() => {
      const code = this.code();
      if (!this.apiMode || !code) return;
      untracked(() =>
        this.certificates
          .verify(code)
          .then((certificate) => this.answer.set({ code, certificate }))
          .catch(() => this.answer.set({ code, failed: true })),
      );
    });
  }

  protected check(): void {
    const code = this.draft().trim().toUpperCase();
    if (code) this.router.navigate(['/verify', code]);
  }
}
