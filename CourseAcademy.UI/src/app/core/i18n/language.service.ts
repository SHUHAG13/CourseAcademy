import { DOCUMENT, formatDate, registerLocaleData } from '@angular/common';
import localeBn from '@angular/common/locales/bn';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { localStore } from '../data/browser-storage';
import { BN_TRANSLATIONS } from './bn.translations';

export type Language = 'bn' | 'en';

const STORAGE_KEY = 'ca-language';
const BANGLA_DIGITS = '০১২৩৪৫৬৭৮৯';

registerLocaleData(localeBn);

/**
 * Runtime translation, so the language can switch instantly without a reload.
 * The English text is the key: anything missing from the dictionary, such as a
 * course an admin has just typed, is simply shown as written.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly language = signal<Language>(localStore.getItem(STORAGE_KEY) === 'en' ? 'en' : 'bn');
  readonly isBangla = computed(() => this.language() === 'bn');
  private readonly numberLocale = computed(() => (this.isBangla() ? 'bn-BD' : 'en-US'));

  constructor() {
    const document = inject(DOCUMENT);
    effect(() => {
      document.documentElement.lang = this.language();
    });
  }

  setLanguage(language: Language): void {
    this.language.set(language);
    try {
      localStore.setItem(STORAGE_KEY, language);
    } catch {
      // The choice still applies until the page is closed.
    }
  }

  /** Translates text and fills `{0}`, `{1}` placeholders. Number values are formatted, text values translated. */
  t(text: string | null | undefined, params: readonly unknown[] = []): string {
    if (text === null || text === undefined) return '';
    const template = this.isBangla() ? (BN_TRANSLATIONS[text] ?? text) : text;
    return template.replace(/\{(\d+)\}/g, (_, index: string) => this.param(params[Number(index)]));
  }

  /** Formats a number. `digitsInfo` uses Angular's `minInt.minFrac-maxFrac` form, e.g. `1.1-1`. */
  number(value: number | null | undefined, digitsInfo?: string): string {
    if (value === null || value === undefined) return '';
    const [, min, max] = digitsInfo?.match(/\.(\d+)-(\d+)$/) ?? [];
    return new Intl.NumberFormat(this.numberLocale(), {
      minimumFractionDigits: min ? Number(min) : undefined,
      maximumFractionDigits: max ? Number(max) : undefined,
    }).format(value);
  }

  /** Formats a date using Angular's date format patterns. */
  date(value: string | number | Date | null | undefined, format: string): string {
    if (value === null || value === undefined || value === '') return '';
    if (!this.isBangla()) return formatDate(value, format, 'en-US');
    return this.banglaDigits(formatDate(value, format, 'bn').replace('AM', 'এএম').replace('PM', 'পিএম'));
  }

  private param(value: unknown): string {
    if (typeof value === 'number') return this.number(value);
    return typeof value === 'string' ? this.t(value) : '';
  }

  private banglaDigits(text: string): string {
    return text.replace(/\d/g, (digit) => BANGLA_DIGITS[Number(digit)]);
  }
}
