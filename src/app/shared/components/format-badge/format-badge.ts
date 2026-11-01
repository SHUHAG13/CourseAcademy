import { Component, computed, input } from '@angular/core';
import { CourseFormat } from '../../../core/models/course.model';

const STYLES: Record<CourseFormat, { icon: string; tone: string }> = {
  'Pre-recorded': { icon: 'bi-play-btn', tone: 'blue' },
  Live: { icon: 'bi-broadcast', tone: 'orange' },
  Hybrid: { icon: 'bi-intersect', tone: 'green' },
};

@Component({
  selector: 'app-format-badge',
  template: `<span [class]="'tag ' + style().tone"><i class="bi" [class]="style().icon" aria-hidden="true"></i>{{ format() }}</span>`,
})
export class FormatBadge {
  readonly format = input.required<CourseFormat>();
  protected readonly style = computed(() => STYLES[this.format()]);
}
