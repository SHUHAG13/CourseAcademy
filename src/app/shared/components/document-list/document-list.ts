import { Component, input } from '@angular/core';
import { DocumentType, SectionDocument } from '../../../core/models/course.model';
import { TranslatePipe } from '../../pipes/translate.pipe';

const ICONS: Record<DocumentType, string> = {
  PDF: 'bi-file-earmark-pdf',
  DOCX: 'bi-file-earmark-word',
  PPTX: 'bi-file-earmark-ppt',
};

/** Documents attached to a video or section. Locked documents show their name only. */
@Component({
  selector: 'app-document-list',
  imports: [TranslatePipe],
  template: `
    <ul class="docs">
      @for (d of documents(); track d.id) {
        <li>
          <i class="bi" [class]="icons[d.type]" [class.type-pdf]="d.type === 'PDF'" [class.type-docx]="d.type === 'DOCX'"
            [class.type-pptx]="d.type === 'PPTX'" aria-hidden="true"></i>
          <span class="flex-grow-1 min-w-0">{{ d.title | t }}</span>
          <span class="tag">{{ d.type }}</span>
          @if (locked()) {
            <i class="bi bi-lock text-muted" [title]="'Enroll to open' | t" aria-hidden="true"></i>
            <span class="visually-hidden">{{ 'Enroll to open' | t }}</span>
          } @else {
            <a [href]="d.url" target="_blank" rel="noopener noreferrer" class="open"
              [attr.aria-label]="'Open (demo link): {0}' | t: [d.title]">{{ 'Open' | t }}</a>
          }
        </li>
      }
    </ul>
  `,
  styles: `
    .docs { display: grid; gap: 0.35rem; margin: 0; padding: 0; list-style: none; }
    li {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.4rem 0.65rem;
      border: 1px dashed var(--ca-border-strong);
      border-radius: var(--ca-radius-sm);
      background: var(--ca-surface);
      font-size: 0.86rem;
      color: var(--ca-text-2);
    }
    .bi { font-size: 1.05rem; }
    .type-pdf { color: var(--ca-danger); }
    .type-docx { color: var(--ca-blue); }
    .type-pptx { color: var(--ca-orange-600); }
    .tag { font-size: 0.7rem; }
    .open { font-weight: 600; white-space: nowrap; }
  `,
})
export class DocumentList {
  readonly documents = input.required<SectionDocument[]>();
  readonly locked = input(false);

  protected readonly icons = ICONS;
}
