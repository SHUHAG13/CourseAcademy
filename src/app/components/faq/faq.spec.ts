import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Faq } from './faq';

describe('Faq', () => {
  it('opens the first question by default and toggles others', async () => {
    await TestBed.configureTestingModule({
      imports: [Faq],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(Faq);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const buttons = el.querySelectorAll<HTMLButtonElement>('.faq-q');

    expect(buttons[0].getAttribute('aria-expanded')).toBe('true');
    expect(buttons[1].getAttribute('aria-expanded')).toBe('false');

    buttons[1].click();
    await fixture.whenStable();

    expect(buttons[0].getAttribute('aria-expanded')).toBe('false');
    expect(buttons[1].getAttribute('aria-expanded')).toBe('true');
  });
});
