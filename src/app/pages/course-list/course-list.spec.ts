import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore } from '../../core/data/browser-storage';
import { COURSES } from '../../core/data/courses.data';
import { CourseService } from '../../core/services/course.service';
import { CourseList } from './course-list';

describe('CourseList', () => {
  beforeEach(async () => {
    localStore.clear();
    await TestBed.configureTestingModule({
      imports: [CourseList],
      providers: [provideRouter([])],
    }).compileComponents();

    // Resolve immediately instead of simulating network latency.
    const service = TestBed.inject(CourseService);
    await service.ready;
    vi.spyOn(service, 'whenLoaded').mockResolvedValue();
  });

  async function render(inputs: Record<string, string> = {}) {
    const fixture = TestBed.createComponent(CourseList);
    for (const [k, v] of Object.entries(inputs)) fixture.componentRef.setInput(k, v);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows all four demo courses without a load-more button', async () => {
    const el = await render();
    expect(el.querySelectorAll('app-course-card').length).toBe(4);
    expect(el.textContent).not.toContain('Load more courses');
  });

  it('applies the category from the query param', async () => {
    const el = await render({ category: 'software-development' });
    expect(el.querySelector('h1')?.textContent).toContain('Software Development');
    const count = COURSES.filter((c) => c.category === 'software-development').length;
    expect(el.querySelectorAll('app-course-card').length).toBe(count);
  });

  it('shows the empty state when nothing matches the search', async () => {
    const el = await render({ q: 'zzz-not-a-course' });
    expect(el.querySelector('app-empty-state')).not.toBeNull();
    expect(el.querySelectorAll('app-course-card').length).toBe(0);
  });

  it('hides unpublished courses as soon as an admin unpublishes one', async () => {
    const service = TestBed.inject(CourseService);
    const fixture = TestBed.createComponent(CourseList);
    await fixture.whenStable();

    await service.saveCourse({ ...COURSES[0], published: false });
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('app-course-card').length).toBe(3);
  });
});
