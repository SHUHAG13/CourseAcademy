import { API_MODE } from '../../core/api/api-mode';
import { ApiService } from '../../core/api/api.service';
import { AdminOverviewDto } from '../../core/api/learning.models';
import { LanguageService } from '../../core/i18n/language.service';
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
import { TranslatePipe, LocalDatePipe } from '../../shared/pipes/translate.pipe';
import { PermissionService, failureMessage } from '../../core/services/permission.service';

@Component({
  selector: 'app-admin-overview',
  imports: [TranslatePipe, LocalDatePipe, RouterLink, BdtPipe, StatusPill],
  template: `
    <h1 class="h3 mb-4">{{ 'Overview' | t }}</h1>

    <div class="row g-3 mb-4">
      @for (s of stats(); track s.label) {
        <div class="col-sm-6 col-xxl-3">
          <a class="stat-tile ca-card ca-card-hover" [routerLink]="s.link">
            <span class="icon-tile" [class]="s.tone"><i class="bi" [class]="s.icon" aria-hidden="true"></i></span>
            <div>
              <span class="value">{{ s.value }}</span>
              <span class="label">{{ s.label | t }}</span>
            </div>
          </a>
        </div>
      }
    </div>

    <section class="ca-card mb-4" aria-labelledby="recent-title">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 p-3">
        <h2 id="recent-title" class="h6 mb-0">{{ 'Recent transactions' | t }}</h2>
        <a routerLink="/admin/transactions" class="small fw-semibold">{{ 'View all' | t }}</a>
      </div>
      <div class="table-responsive">
        <table class="ca-table">
          <thead>
            <tr><th scope="col">{{ 'Transaction' | t }}</th><th scope="col">{{ 'Student' | t }}</th><th scope="col">{{ 'Course' | t }}</th><th scope="col">{{ 'Amount' | t }}</th><th scope="col">{{ 'Status' | t }}</th></tr>
          </thead>
          <tbody>
            @for (t of recent(); track t.id) {
              <tr>
                <td><span class="primary">{{ t.id }}</span><span class="secondary">{{ t.createdAt | ldate: 'd MMM y' }}</span></td>
                <td>{{ t.userName }}</td>
                <td>{{ t.courseTitle | t }}</td>
                <td class="text-nowrap">{{ t.amount | bdt }}</td>
                <td><app-status-pill [status]="t.status" /></td>
              </tr>
            } @empty {
              <tr><td colspan="5" class="text-center text-muted py-4">{{ 'No transactions yet.' | t }}</td></tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    <!-- Demo mode only: with the API there is no browser demo data to reset (BR-ADM-06). -->
    @if (!apiMode && permissions.can('system.reset')) {
      <section class="panel ca-card" aria-labelledby="reset-title">
        <h2 id="reset-title" class="panel-title">{{ 'Reset demo data' | t }}</h2>
        <p class="text-muted small">
          {{ 'Restores the four default courses, instructors, demo accounts, enrollments and transactions. Everything created or changed in this browser is lost.' | t }}
        </p>
        <button type="button" class="btn btn-outline" [disabled]="resetting()" (click)="reset()">
          <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i> {{ 'Reset demo data' | t }}
        </button>
      </section>
    }
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
  private readonly language = inject(LanguageService);
  protected readonly permissions = inject(PermissionService);

  protected readonly resetting = signal(false);
  /** API mode: the totals are counted by the server, so the page doesn't need every record. */
  private readonly apiMode = inject(API_MODE);
  private readonly overview = signal<AdminOverviewDto | null>(null);

  constructor() {
    if (this.apiMode) {
      inject(ApiService)
        .get<AdminOverviewDto>('admin/overview')
        .then((o) => this.overview.set(o))
        .catch((error: unknown) => this.toast.error(failureMessage(error, 'The overview could not be loaded. Please refresh the page.')));
    }
  }

  private readonly totals = computed(() => {
    if (this.apiMode) {
      const o = this.overview();
      return {
        courses: o?.courseCount ?? 0,
        published: o?.publishedCourseCount ?? 0,
        students: o?.studentCount ?? 0,
        activeEnrollments: o?.activeEnrollmentCount ?? 0,
        collected: o?.collectedAmount ?? 0,
      };
    }
    return {
      courses: this.courses.courses().length,
      published: this.courses.published().length,
      students: this.auth.users().filter((u) => u.role === 'user').length,
      activeEnrollments: this.enrollments.enrollments().filter((e) => e.status === 'active').length,
      collected: this.payments.transactions().filter((t) => t.status === 'success').reduce((sum, t) => sum + t.amount, 0),
    };
  });

  protected readonly stats = computed(() => {
    const t = this.totals();
    return [
      {
        label: this.language.t('Courses ({0} published)', [t.published]),
        value: this.language.number(t.courses),
        icon: 'bi-journal-text',
        tone: '',
        link: '/admin/courses',
      },
      {
        label: 'Student accounts',
        value: this.language.number(t.students),
        icon: 'bi-people',
        tone: 'navy',
        link: '/admin/users',
      },
      {
        label: 'Active enrollments',
        value: this.language.number(t.activeEnrollments),
        icon: 'bi-card-checklist',
        tone: 'orange',
        link: '/admin/enrollments',
      },
      {
        label: this.apiMode ? 'Payments collected' : 'Demo payments collected',
        value: `৳${this.language.number(t.collected)}`,
        icon: 'bi-credit-card',
        tone: 'navy',
        link: '/admin/transactions',
      },
    ];
  });

  protected readonly recent = computed(() => {
    if (this.apiMode) {
      return (this.overview()?.recentTransactions ?? []).map((t) => ({
        id: t.transactionNo,
        createdAt: t.createdAt,
        userName: t.userName,
        courseTitle: t.courseTitle,
        amount: t.amount,
        status: t.statusCode,
      }));
    }
    return [...this.payments.transactions()].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 5);
  });

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
    } catch (error) {
      this.toast.error(failureMessage(error, 'The demo data could not be reset. Browser storage may be blocked.'));
    } finally {
      this.resetting.set(false);
    }
  }
}
