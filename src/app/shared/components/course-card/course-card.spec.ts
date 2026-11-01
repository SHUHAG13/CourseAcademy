import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore } from '../../../core/data/browser-storage';
import { COURSES } from '../../../core/data/courses.data';
import { CourseService } from '../../../core/services/course.service';
import { CourseCard } from './course-card';

describe('CourseCard', () => {
  it('renders title, instructor, prices in BDT and a link to the course page', async () => {
    localStore.clear();
    await TestBed.configureTestingModule({
      imports: [CourseCard],
      providers: [provideRouter([])],
    }).compileComponents();
    await TestBed.inject(CourseService).ready;

    const course = COURSES[0];
    const fixture = TestBed.createComponent(CourseCard);
    fixture.componentRef.setInput('course', course);
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.title')?.textContent).toContain(course.title);
    expect(el.querySelector('.instructor')?.textContent).toContain('Tanvir Hasan');
    expect(el.querySelector('.price-now')?.textContent?.trim()).toBe('৳4,999');
    expect(el.querySelector('.price-was')?.textContent?.trim()).toBe('৳8,000');
    expect(el.querySelector('app-format-badge')?.textContent).toContain('Pre-recorded');
    expect(el.querySelector('.title a')?.getAttribute('href')).toBe(`/courses/${course.slug}`);
  });
});
