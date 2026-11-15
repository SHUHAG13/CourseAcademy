import { TestBed } from '@angular/core/testing';
import { localStore } from '../data/browser-storage';
import { COURSES, INSTRUCTORS } from '../data/courses.data';
import { BN_TRANSLATIONS } from './bn.translations';
import { flattenSections } from '../models/course.model';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  function create(saved?: string): LanguageService {
    localStore.clear();
    if (saved) localStore.setItem('ca-language', saved);
    TestBed.resetTestingModule();
    return TestBed.inject(LanguageService);
  }

  it('defaults to Bangla and marks the page language', () => {
    const language = create();
    TestBed.tick();
    expect(language.language()).toBe('bn');
    expect(language.t('Log in')).toBe('লগ ইন');
    expect(document.documentElement.lang).toBe('bn');
  });

  it('remembers the chosen language', () => {
    create().setLanguage('en');
    TestBed.resetTestingModule();
    expect(TestBed.inject(LanguageService).language()).toBe('en');
  });

  it('shows English keys as written in English mode', () => {
    const language = create('en');
    expect(language.t('Log in')).toBe('Log in');
    expect(language.t('{0} students', [1200])).toBe('1,200 students');
  });

  it('shows text with no translation as written, such as admin-entered courses', () => {
    expect(create().t('Demo Podcast Production')).toBe('Demo Podcast Production');
  });

  it('fills placeholders, translating text and using Bangla digits for numbers', () => {
    const language = create();
    expect(language.t('{0} students', [1200])).toBe('১,২০০ জন শিক্ষার্থী');
    expect(language.t('More in {0}', ['Video Editing'])).toBe('ভিডিও এডিটিং-এ আরও কোর্স');
  });

  it('formats numbers and dates in Bangla', () => {
    const language = create();
    expect(language.number(4.8, '1.1-1')).toBe('৪.৮');
    expect(language.date('2026-11-06T20:00', 'd MMM y')).not.toMatch(/[0-9A-Za-z]/);
    expect(language.date('2026-11-06T20:00', 'h:mm a')).toBe('৮:০০ পিএম');
  });

  it('switches number and date formats back to English', () => {
    const language = create('en');
    expect(language.number(1200)).toBe('1,200');
    expect(language.date('2026-11-06T20:00', 'd MMM y')).toBe('6 Nov 2026');
  });

  it('has a Bangla translation for all default course content', () => {
    const missing: string[] = [];
    const need = (text: string) => {
      if (!BN_TRANSLATIONS[text]) missing.push(text);
    };
    for (const c of COURSES) {
      [c.title, c.subtitle, c.description, c.coverLabel, ...c.outcomes, ...c.requirements].forEach(need);
      for (const m of c.modules) {
        need(m.title);
        for (const sub of m.submodules) {
          need(sub.title);
          flattenSections(sub.sections).forEach((sec) =>
            [sec.title, ...sec.videos.map((v) => v.title), ...sec.documents.map((d) => d.title)].forEach(need),
          );
        }
      }
      c.schedule.forEach((s) => need(s.title));
      c.materials.forEach((m) => need(m.title));
    }
    for (const i of INSTRUCTORS) [i.name, i.title, i.bio].forEach(need);
    expect(missing).toEqual([]);
  });
});
