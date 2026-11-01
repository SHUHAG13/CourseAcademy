import { Component, computed, input } from '@angular/core';
import { LiveSession } from '../../../core/models/course.model';
import { TranslatePipe, LocalDatePipe } from '../../pipes/translate.pipe';

/** Live class schedule. Meeting links are shown only to people with course access. */
@Component({
  selector: 'app-session-list',
  imports: [TranslatePipe, LocalDatePipe],
  template: `
    <ul class="sessions">
      @for (s of sorted(); track s.id) {
        <li [class.past]="isPast(s)">
          <span class="when">
            <strong>{{ s.startsAt | ldate: 'd MMM' }}</strong>
            <small>{{ s.startsAt | ldate: 'h:mm a' }}</small>
          </span>
          <span class="what">
            <span class="name">{{ s.title | t }}</span>
            <small>{{ '{0} · {1} min' | t: [(s.startsAt | ldate: 'EEEE, y'), s.durationMinutes] }}</small>
          </span>
          @if (isPast(s)) {
            <span class="tag">{{ 'Completed' | t }}</span>
          } @else if (showLinks()) {
            <a class="btn btn-sm btn-outline" [href]="s.meetingUrl" target="_blank" rel="noopener noreferrer"
              [attr.aria-label]="'Join (demo link): {0}' | t: [s.title]">
              <i class="bi bi-camera-video" aria-hidden="true"></i> {{ 'Join (demo)' | t }}
            </a>
          }
        </li>
      }
    </ul>
  `,
  styles: `
    .sessions { display: grid; gap: 0.6rem; margin: 0; padding: 0; list-style: none; }
    li {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.75rem 1rem;
      padding: 0.75rem 1rem;
      border: 1px solid var(--ca-border);
      border-radius: var(--ca-radius);
      background: var(--ca-surface);
    }
    li.past { opacity: 0.65; }
    .when {
      display: flex;
      flex-direction: column;
      min-width: 74px;
      padding-right: 1rem;
      border-right: 1px solid var(--ca-border);
    }
    .when strong { color: var(--ca-text); }
    small { font-size: 0.8rem; color: var(--ca-muted); }
    .what { display: flex; flex: 1; flex-direction: column; min-width: 180px; }
    .name { font-weight: 600; color: var(--ca-text); }
  `,
})
export class SessionList {
  readonly sessions = input.required<LiveSession[]>();
  readonly showLinks = input(false);

  protected readonly sorted = computed(() => [...this.sessions()].sort((a, b) => a.startsAt.localeCompare(b.startsAt)));

  protected isPast(session: LiveSession): boolean {
    return new Date(session.startsAt).getTime() + session.durationMinutes * 60_000 < Date.now();
  }
}
