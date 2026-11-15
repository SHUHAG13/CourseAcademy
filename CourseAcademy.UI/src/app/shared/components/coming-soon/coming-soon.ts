import { Component } from '@angular/core';
import { EmptyState } from '../empty-state/empty-state';

/** Shown in API mode for features whose data has not moved to the server yet. */
@Component({
  selector: 'app-coming-soon',
  imports: [EmptyState],
  template: `
    <app-empty-state icon="bi-hourglass-split" title="Coming soon"
      message="This part is being moved to the new server and will be available again soon." />
  `,
})
export class ComingSoon {}
