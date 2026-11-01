import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../../core/i18n/language.service';

// These pipes are impure so templates update as soon as the language changes.

/** Translates text, e.g. `{{ 'Log in' | t }}` or `{{ '{0} lessons' | t: [count] }}`. */
@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: string | null | undefined, params?: readonly unknown[]): string {
    return this.language.t(value, params);
  }
}

/** Formats a number in the current language, e.g. `{{ rating | n: '1.1-1' }}`. */
@Pipe({ name: 'n', pure: false })
export class LocalNumberPipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: number | null | undefined, digitsInfo?: string): string {
    return this.language.number(value, digitsInfo);
  }
}

/** Formats a date in the current language, e.g. `{{ when | ldate: 'd MMM y' }}`. */
@Pipe({ name: 'ldate', pure: false })
export class LocalDatePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: string | number | Date | null | undefined, format: string): string {
    return this.language.date(value, format);
  }
}
