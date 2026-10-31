import { COURSES } from '../data/courses.data';
import { CourseFilter } from '../models/course.model';
import { DEFAULT_FILTER, filterCourses } from './course.service';

const run = (patch: Partial<CourseFilter>) => filterCourses(COURSES, { ...DEFAULT_FILTER, ...patch });

describe('filterCourses', () => {
  it('returns every course with the default filter, most popular first', () => {
    const result = run({});
    expect(result.length).toBe(COURSES.length);
    for (let i = 1; i < result.length; i++) {
      expect(result[i - 1].students).toBeGreaterThanOrEqual(result[i].students);
    }
  });

  it('filters by category', () => {
    const result = run({ category: 'video-editing' });
    expect(result.length).toBeGreaterThan(0);
    expect(result.every((c) => c.category === 'video-editing')).toBe(true);
  });

  it('searches title, subtitle and cover label case-insensitively', () => {
    expect(run({ search: 'PREMIERE' }).map((c) => c.slug)).toContain('premiere-pro-professional-video-editing');
    expect(run({ search: 'cricket' }).length).toBeGreaterThan(0);
    expect(run({ search: 'no-such-course-xyz' })).toEqual([]);
  });

  it('filters by price range', () => {
    expect(run({ price: 'under-2500' }).every((c) => c.price < 2500)).toBe(true);
    expect(run({ price: '2500-4500' }).every((c) => c.price >= 2500 && c.price <= 4500)).toBe(true);
    expect(run({ price: 'over-4500' }).every((c) => c.price > 4500)).toBe(true);
  });

  it('filters by one or more levels', () => {
    const result = run({ levels: ['Intermediate', 'Advanced'] });
    expect(result.length).toBeGreaterThan(0);
    expect(result.every((c) => c.level !== 'Beginner')).toBe(true);
  });

  it('sorts by price both ways and by rating', () => {
    const asc = run({ sort: 'price-asc' }).map((c) => c.price);
    expect(asc).toEqual([...asc].sort((a, b) => a - b));

    const desc = run({ sort: 'price-desc' }).map((c) => c.price);
    expect(desc).toEqual([...desc].sort((a, b) => b - a));

    const rating = run({ sort: 'rating' }).map((c) => c.rating);
    expect(rating).toEqual([...rating].sort((a, b) => b - a));
  });

  it('does not mutate the source array', () => {
    const before = COURSES.map((c) => c.id);
    run({ sort: 'price-asc' });
    expect(COURSES.map((c) => c.id)).toEqual(before);
  });
});

describe('course data', () => {
  it('has unique slugs and a discount on every course', () => {
    const slugs = new Set(COURSES.map((c) => c.slug));
    expect(slugs.size).toBe(COURSES.length);
    expect(COURSES.every((c) => c.price < c.originalPrice)).toBe(true);
  });
});
