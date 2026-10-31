import { Injectable } from '@angular/core';
import { CATEGORIES, COURSES, INSTRUCTORS, REVIEWS } from '../data/courses.data';
import {
  Category,
  CategoryId,
  Course,
  CourseFilter,
  Instructor,
  PriceRange,
  Review,
} from '../models/course.model';

/** Simulated network latency so loading states are visible with mock data. */
const MOCK_LATENCY_MS = 350;

export const DEFAULT_FILTER: CourseFilter = {
  search: '',
  category: 'all',
  price: 'all',
  levels: [],
  sort: 'popular',
};

export const PRICE_RANGES: { id: PriceRange; label: string }[] = [
  { id: 'all', label: 'Any price' },
  { id: 'under-2500', label: 'Under ৳2,500' },
  { id: '2500-4500', label: '৳2,500 – ৳4,500' },
  { id: 'over-4500', label: 'Over ৳4,500' },
];

function inPriceRange(price: number, range: PriceRange): boolean {
  switch (range) {
    case 'under-2500':
      return price < 2500;
    case '2500-4500':
      return price >= 2500 && price <= 4500;
    case 'over-4500':
      return price > 4500;
    default:
      return true;
  }
}

/** Pure filter + sort, kept separate from the service so it is easy to test. */
export function filterCourses(courses: Course[], filter: CourseFilter): Course[] {
  const term = filter.search.trim().toLowerCase();

  const result = courses.filter(
    (c) =>
      (filter.category === 'all' || c.category === filter.category) &&
      inPriceRange(c.price, filter.price) &&
      (filter.levels.length === 0 || filter.levels.includes(c.level)) &&
      (!term ||
        c.title.toLowerCase().includes(term) ||
        c.subtitle.toLowerCase().includes(term) ||
        c.coverLabel.toLowerCase().includes(term)),
  );

  switch (filter.sort) {
    case 'rating':
      return result.sort((a, b) => b.rating - a.rating || b.ratingCount - a.ratingCount);
    case 'price-asc':
      return result.sort((a, b) => a.price - b.price);
    case 'price-desc':
      return result.sort((a, b) => b.price - a.price);
    case 'newest':
      return result.sort((a, b) => b.updated.localeCompare(a.updated));
    default:
      return result.sort((a, b) => b.students - a.students);
  }
}

@Injectable({ providedIn: 'root' })
export class CourseService {
  readonly categories: Category[] = CATEGORIES;

  /** Synchronous access for static sections (home page, footer). */
  readonly all: Course[] = COURSES;

  async list(): Promise<Course[]> {
    await delay(MOCK_LATENCY_MS);
    return COURSES;
  }

  async getBySlug(slug: string): Promise<Course | undefined> {
    await delay(MOCK_LATENCY_MS);
    return COURSES.find((c) => c.slug === slug);
  }

  featured(): Course[] {
    return COURSES.filter((c) => c.featured).slice(0, 4);
  }

  category(id: CategoryId): Category {
    return CATEGORIES.find((c) => c.id === id)!;
  }

  isCategory(id: string | undefined | null): id is CategoryId {
    return CATEGORIES.some((c) => c.id === id);
  }

  countByCategory(id: CategoryId): number {
    return COURSES.filter((c) => c.category === id).length;
  }

  instructor(id: string): Instructor {
    return INSTRUCTORS.find((i) => i.id === id)!;
  }

  coursesByInstructor(id: string): number {
    return COURSES.filter((c) => c.instructorId === id).length;
  }

  related(course: Course, count = 4): Course[] {
    const sameCategory = COURSES.filter((c) => c.category === course.category && c.id !== course.id);
    const others = COURSES.filter((c) => c.category !== course.category && c.featured);
    return [...sameCategory, ...others].slice(0, count);
  }

  /** Deterministic subset of the demo review pool for a course. */
  reviews(course: Course, count = 3): Review[] {
    const start = course.id % REVIEWS.length;
    return Array.from({ length: count }, (_, i) => REVIEWS[(start + i) % REVIEWS.length]);
  }

  /** Demo star distribution (percentages for 5★ … 1★) derived from the average rating. */
  ratingBreakdown(course: Course): number[] {
    const five = Math.round((course.rating - 4) * 85);
    const four = Math.max(0, 92 - five);
    return [five, four, 5, 2, 1].map((v) => Math.min(100, v));
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
