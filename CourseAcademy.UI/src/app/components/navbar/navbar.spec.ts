import { ComponentFixture, TestBed } from '@angular/core/testing';

import { provideRouter } from '@angular/router';
import { LanguageService } from '../../core/i18n/language.service';
import { Navbar } from './navbar';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [provideRouter([])]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('has a language switch in the navbar that defaults to Bangla', async () => {
    const language = TestBed.inject(LanguageService);
    language.setLanguage('bn');
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    const english = el.querySelector<HTMLButtonElement>('.nav-actions .lang-switch button[lang=en]')!;
    expect(el.querySelector('.lang-switch .active')?.textContent).toBe('BN');

    english.click();
    await fixture.whenStable();
    expect(language.language()).toBe('en');
    expect(english.classList.contains('active')).toBe(true);
    expect(english.getAttribute('aria-pressed')).toBe('true');
  });
});
