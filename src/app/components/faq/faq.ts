import { Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FAQS } from '../../core/data/site.data';

@Component({
  selector: 'app-faq',
  imports: [RouterLink],
  templateUrl: './faq.html',
  styleUrl: './faq.scss',
})
export class Faq {
  /** True on the standalone /faq route, where this section is the page. */
  readonly asPage = input(false);

  protected readonly faqs = FAQS;
  protected readonly open = signal<number | null>(0);

  protected toggle(index: number): void {
    this.open.update((current) => (current === index ? null : index));
  }
}
