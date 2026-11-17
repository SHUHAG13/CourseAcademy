import { DOCUMENT, formatDate, registerLocaleData } from '@angular/common';
import localeBn from '@angular/common/locales/bn';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { localStore } from '../data/browser-storage';

export type Language = 'bn' | 'en';

const STORAGE_KEY = 'ca-language';
const BANGLA_DIGITS = '০১২৩৪৫৬৭৮৯';

registerLocaleData(localeBn);

/**
 * Runtime translation, so the language can switch without a reload.
 * The English text is the key: anything missing from the dictionary, such as a
 * course an admin has just typed, is simply shown as written.
 * The Bangla dictionary is a separate file, loaded only when Bangla is used: at start-up
 * (see `ready`) when it is the chosen language, otherwise when the user switches to it.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly language = signal<Language>(localStore.getItem(STORAGE_KEY) === 'en' ? 'en' : 'bn');
  readonly isBangla = computed(() => this.language() === 'bn');
  private readonly numberLocale = computed(() => (this.isBangla() ? 'bn-BD' : 'en-US'));
  private readonly dictionary = signal<Readonly<Record<string, string>> | null>(null);
  private loading: Promise<void> | null = null;

  constructor() {
    const document = inject(DOCUMENT);
    effect(() => {
      document.documentElement.lang = this.language();
    });
  }

  /** Resolves once the chosen language can be shown. Run at start-up, so Bangla pages never flash English. */
  ready(): Promise<void> {
    return this.isBangla() ? this.loadDictionary() : Promise.resolve();
  }

  /** Switches language. Bangla waits for its dictionary, so the page changes all at once. */
  async setLanguage(language: Language): Promise<void> {
    if (language === 'bn') await this.loadDictionary();
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
    const dictionary = this.isBangla() ? this.dictionary() : null;
    const template = dictionary?.[text] ?? text;
    return template.replace(/\{(\d+)\}/g, (_, index: string) => this.param(params[Number(index)]));
  }

  /** Loads the Bangla dictionary once. If it can't be loaded, texts stay in English and the next switch tries again. */
  private loadDictionary(): Promise<void> {
    if (this.dictionary()) return Promise.resolve();
    this.loading ??= import('./bn.translations')
      .then((module) => this.dictionary.set(module.BN_TRANSLATIONS))
      .catch(() => {
        this.loading = null;
      });
    return this.loading;
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
