import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../../core/i18n/language.service';

/** Formats a number as Bangladeshi Taka, e.g. 2499 → "৳2,499" or "৳২,৪৯৯". */
@Pipe({ name: 'bdt', pure: false })
export class BdtPipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: number | null | undefined): string {
    return value == null ? '' : `৳${this.language.number(value, '1.0-0')}`;
  }
}
