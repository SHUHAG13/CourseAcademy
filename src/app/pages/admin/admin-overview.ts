import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PaymentService } from '../../core/payments/payment.service';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { CourseService } from '../../core/services/course.service';
import { DemoDataService } from '../../core/services/demo-data.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { ToastService } from '../../core/services/toast.service';
import { StatusPill } from '../../shared/components/status-pill/status-pill';
import { BdtPipe } from '../../shared/pipes/bdt.pipe';

@Component({
  selector: 'app-admin-overview',
  imports: [DatePipe, RouterLink, BdtPipe, StatusPill],
  template: `
    <h1 class="h3 mb-4">Overview</h1>

    <div class="row g-3 mb-4">
      @for (s of stats(); track s.label) {
        <div class="col-sm-6 col-xxl-3">
          <a class="stat-tile ca-card ca-card-hover" [routerLink]="s.link">
            <span class="icon-tile" [class]="s.tone"><i class="bi" [class]="s.icon" aria-hidden="true"></i></span>
            <div>
              <span class="value">{{ s.value }}</span>
              <span class="label">{{ s.label }}</span>
            </div>
          </a>
        </div>
      }
    </div>

    <section class="ca-card mb-4" aria-labelledby="recent-title">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 p-3">
        <h2 id="recent-title" class="h6 mb-0">Recent transactions</h2>
        <a routerLink="/admin/transactions" class="small fw-semibold">View all</a>
      </div>
      <div class="table-responsive">
        <table class="ca-table">
          <thead>
            <tr><th scope="col">Transaction</th><th scope="col">Student</th><th scope="col">Course</th><th scope="col">Amount</th><th scope="col">Status</th></tr>
          </thead>
          <tbody>
            @for (t of recent(); track t.id) {
              <tr>
                <td><span class="primary">{{ t.id }}</span><span class="secondary">{{ t.createdAt | date: 'd MMM y' }}</span></td>
                <td>{{ t.userName }}</td>
                <td>{{ t.courseTitle }}</td>
                <td class="text-nowrap">{{ t.amount | bdt }}</td>
                <td><app-status-pill [status]="t.status" /></td>
              </tr>
            } @empty {
              <tr><td colspan="5" class="text-center text-muted py-4">No transactions yet.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    <section class="panel ca-card" aria-labelledby="reset-title">
      <h2 id="reset-title" class="panel-title">Reset demo data</h2>
      <p class="text-muted small">
        Restores the four default courses, instructors, demo accounts, enrollments and transactions. Everything created
        or changed in this browser is lost.
      </p>
      <button type="button" class="btn btn-outline" [disabled]="resetting()" (click)="reset()">
        <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i> Reset demo data
      </button>
    </section>
  `,
  styles: `
    a.stat-tile { color: inherit; }
  `,
})
export class AdminOverview {
  private readonly courses = inject(CourseService);
  private readonly auth = inject(AuthService);
  private readonly enrollments = inject(EnrollmentService);
  private readonly payments = inject(PaymentService);
  private readonly demoData = inject(DemoDataService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  protected readonly resetting = signal(false);

  protected readonly stats = computed(() => {
    const paid = this.payments.transactions().filter((t) => t.status === 'success');
    const collected = paid.reduce((sum, t) => sum + t.amount, 0);
    return [
      {
        label: `Courses (${this.courses.published().length} published)`,
        value: this.courses.courses().length.toLocaleString('en-US'),
        icon: 'bi-journal-text',
        tone: '',
        link: '/admin/courses',
      },
      {
        label: 'Student accounts',
        value: this.auth.users().filter((u) => u.role === 'user').length.toLocaleString('en-US'),
        icon: 'bi-people',
        tone: 'navy',
        link: '/admin/users',
      },
      {
        label: 'Active enrollments',
        value: this.enrollments.enrollments().filter((e) => e.status === 'active').length.toLocaleString('en-US'),
        icon: 'bi-card-checklist',
        tone: 'orange',
        link: '/admin/enrollments',
      },
      {
        label: 'Demo payments collected',
        value: `৳${collected.toLocaleString('en-US')}`,
        icon: 'bi-credit-card',
        tone: 'navy',
        link: '/admin/transactions',
      },
    ];
  });

  protected readonly recent = computed(() =>
    [...this.payments.transactions()].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 5),
  );

  protected async reset(): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: 'Reset all demo data?',
      message: 'All courses, accounts, enrollments and transactions return to their defaults. This cannot be undone.',
      confirmLabel: 'Reset demo data',
      danger: true,
    });
    if (!confirmed) return;

    this.resetting.set(true);
    try {
      await this.demoData.reset();
      this.toast.success('Demo data has been reset.');
    } catch {
      this.toast.error('The demo data could not be reset. Browser storage may be blocked.');
    } finally {
      this.resetting.set(false);
    }
  }
}
