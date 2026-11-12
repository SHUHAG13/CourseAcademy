import { Component, computed, input, model } from '@angular/core';
import { SCREENS, Screen, ScreenArea, expandScreens, screenPermissions } from '../../../core/data/screens.data';
import { Permission } from '../../../core/models/permission.model';
import { TranslatePipe } from '../../pipes/translate.pipe';

const AREAS: { id: ScreenArea; label: string }[] = [
  { id: 'admin', label: 'Admin screens' },
  { id: 'student', label: 'Student screens' },
];

/**
 * One checkbox per screen, bound with `[(value)]`. A ticked screen gives every
 * permission of that screen; a screen counts as ticked when its View is present.
 */
@Component({
  selector: 'app-access-matrix',
  imports: [TranslatePipe],
  template: `
    @for (area of areas; track area.id) {
      <div class="table-responsive mb-3">
        <table class="ca-table matrix">
          <caption class="visually-hidden">{{ area.label | t }}</caption>
          <thead>
            <tr>
              <th scope="col">{{ area.label | t }}</th>
              <th scope="col" class="text-center">{{ 'Screen access' | t }}</th>
            </tr>
          </thead>
          <tbody>
            @for (s of screensIn(area.id); track s.id) {
              <tr>
                <td><span class="primary"><i class="bi me-2" [class]="s.icon" aria-hidden="true"></i>{{ s.label | t }}</span></td>
                <td class="text-center">
                  <input class="form-check-input" type="checkbox" [checked]="selected().has(s.view)" [disabled]="readonly()"
                    (change)="toggle(s)" [attr.aria-label]="'Allow all for {0}' | t: [s.label]" />
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: `
    .matrix th:first-child { min-width: 200px; }
  `,
})
export class AccessMatrix {
  readonly value = model.required<Permission[]>();
  readonly readonly = input(false);

  protected readonly areas = AREAS;
  protected readonly selected = computed(() => new Set(this.value()));

  protected screensIn(area: ScreenArea): Screen[] {
    return SCREENS.filter((s) => s.area === area);
  }

  protected toggle(s: Screen): void {
    const next = new Set(expandScreens(this.value()));
    const on = !next.has(s.view);
    for (const p of screenPermissions(s)) {
      if (on) next.add(p);
      else next.delete(p);
    }
    this.value.set([...next]);
  }
}
