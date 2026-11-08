import { Component, computed, input, model } from '@angular/core';
import { SCREENS, SCREEN_ACTIONS, Screen, ScreenAction, ScreenArea, screenPermissions } from '../../../core/data/screens.data';
import { Permission } from '../../../core/models/permission.model';
import { TranslatePipe } from '../../pipes/translate.pipe';

const ACTION_LABELS: Record<ScreenAction, string> = { view: 'View', create: 'Create', edit: 'Edit', delete: 'Delete' };

const AREAS: { id: ScreenArea; label: string }[] = [
  { id: 'admin', label: 'Admin screens' },
  { id: 'student', label: 'Student screens' },
];

/**
 * Grid of screens and the actions allowed on each, bound with `[(value)]`.
 * An action needs View, so ticking one also ticks View and unticking View clears the row.
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
              @for (a of actions; track a) {
                <th scope="col" class="text-center">{{ actionLabels[a] | t }}</th>
              }
              <th scope="col">{{ 'Other' | t }}</th>
              @if (!readonly()) {
                <th scope="col"><span class="visually-hidden">{{ 'Actions' | t }}</span></th>
              }
            </tr>
          </thead>
          <tbody>
            @for (s of screensIn(area.id); track s.id) {
              <tr>
                <td><span class="primary"><i class="bi me-2" [class]="s.icon" aria-hidden="true"></i>{{ s.label | t }}</span></td>
                @for (a of actions; track a) {
                  <td class="text-center">
                    @if (s[a]; as p) {
                      <input class="form-check-input" type="checkbox" [checked]="selected().has(p)" [disabled]="readonly()"
                        (change)="toggle(s, p)" [attr.aria-label]="'{0}: {1}' | t: [s.label, actionLabels[a]]" />
                    }
                  </td>
                }
                <td>
                  @for (e of s.extra ?? []; track e.permission) {
                    <label class="form-check-label small d-flex align-items-center gap-2 text-nowrap">
                      <input class="form-check-input mt-0" type="checkbox" [checked]="selected().has(e.permission)"
                        [disabled]="readonly()" (change)="toggle(s, e.permission)" />
                      {{ e.label | t }}
                    </label>
                  }
                </td>
                @if (!readonly()) {
                  <td class="actions">
                    <button type="button" class="btn btn-sm btn-outline" (click)="toggleRow(s)"
                      [attr.aria-label]="(rowFull(s) ? 'Clear all for {0}' : 'Allow all for {0}') | t: [s.label]">
                      {{ (rowFull(s) ? 'None' : 'All') | t }}
                    </button>
                  </td>
                }
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
  protected readonly actions = SCREEN_ACTIONS;
  protected readonly actionLabels = ACTION_LABELS;
  protected readonly selected = computed(() => new Set(this.value()));

  protected screensIn(area: ScreenArea): Screen[] {
    return SCREENS.filter((s) => s.area === area);
  }

  protected rowFull(s: Screen): boolean {
    return screenPermissions(s).every((p) => this.selected().has(p));
  }

  protected toggle(s: Screen, permission: Permission): void {
    const next = new Set(this.selected());
    if (next.has(permission)) {
      // Without View, none of the screen's actions can be used.
      for (const p of permission === s.view ? screenPermissions(s) : [permission]) next.delete(p);
    } else {
      next.add(permission).add(s.view);
    }
    this.value.set([...next]);
  }

  protected toggleRow(s: Screen): void {
    const next = new Set(this.selected());
    const full = this.rowFull(s);
    for (const p of screenPermissions(s)) {
      if (full) next.delete(p);
      else next.add(p);
    }
    this.value.set([...next]);
  }
}
