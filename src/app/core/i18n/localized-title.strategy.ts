import { Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { LanguageService } from './language.service';

/** Translates route titles, written as `Page | Site`, and updates them when the language changes. */
@Injectable()
export class LocalizedTitleStrategy extends TitleStrategy {
  private readonly language = inject(LanguageService);
  private readonly title = inject(Title);
  private readonly routeTitle = signal<string | undefined>(undefined);

  constructor() {
    super();
    effect(() => {
      const value = this.routeTitle();
      if (value === undefined) return;
      this.title.setTitle(
        value
          .split(' | ')
          .map((part) => this.language.t(part))
          .join(' | '),
      );
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.routeTitle.set(this.buildTitle(snapshot));
  }
}
