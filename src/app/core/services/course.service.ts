import { Injectable, computed, inject } from '@angular/core';
import { CATEGORIES, REVIEWS } from '../data/courses.data';
import { COURSE_REPOSITORY, INSTRUCTOR_REPOSITORY } from '../data/repositories';
import { EntityStore } from '../data/repository';
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
  formats: [],
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
export function filterCourses(
  courses: Course[],
  filter: CourseFilter,
  /** Also matches the translated text, so people can search in the language they see. */
  translate: (text: string) => string = (text) => text,
): Course[] {
  const term = filter.search.trim().toLowerCase();
  const matches = (text: string) => text.toLowerCase().includes(term) || translate(text).toLowerCase().includes(term);

  const result = courses.filter(
    (c) =>
      (filter.category === 'all' || c.category === filter.category) &&
      inPriceRange(c.price, filter.price) &&
      (filter.levels.length === 0 || filter.levels.includes(c.level)) &&
      (filter.formats.length === 0 || filter.formats.includes(c.format)) &&
      (!term || matches(c.title) || matches(c.subtitle) || matches(c.coverLabel)),
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

/** Percentage saved. Safe for free courses and courses without a discount. */
export function discountPercent(course: Pick<Course, 'price' | 'originalPrice'>): number {
  return course.originalPrice > 0 ? Math.round((1 - course.price / course.originalPrice) * 100) : 0;
}

export function lessonCount(course: Course): number {
  return course.curriculum.reduce((n, s) => n + s.lessons.length, 0);
}

@Injectable({ providedIn: 'root' })
export class CourseService {
  private readonly courseStore = new EntityStore(inject(COURSE_REPOSITORY));
  private readonly instructorStore = new EntityStore(inject(INSTRUCTOR_REPOSITORY));

  readonly categories: Category[] = CATEGORIES;

  readonly ready = Promise.all([this.courseStore.ready, this.instructorStore.ready]);
  readonly recovered = computed(() => this.courseStore.recovered() || this.instructorStore.recovered());

  /** Every course, including unpublished ones. For the admin area. */
  readonly courses = this.courseStore.items;
  /** What visitors can see. */
  readonly published = computed(() => this.courses().filter((c) => c.published));
  readonly instructors = this.instructorStore.items;

  /** Simulated wait for pages that show a loading state before rendering a course. */
  async whenLoaded(): Promise<void> {
    await delay(MOCK_LATENCY_MS);
    await this.ready;
  }

  bySlug(slug: string): Course | undefined {
    return this.courses().find((c) => c.slug === slug);
  }

  byId(id: number): Course | undefined {
    return this.courses().find((c) => c.id === id);
  }

  featured(): Course[] {
    return this.published()
      .filter((c) => c.featured)
      .slice(0, 4);
  }

  category(id: CategoryId): Category {
    return CATEGORIES.find((c) => c.id === id)!;
  }

  isCategory(id: string | undefined | null): id is CategoryId {
    return CATEGORIES.some((c) => c.id === id);
  }

  countByCategory(id: CategoryId): number {
    return this.published().filter((c) => c.category === id).length;
  }

  instructor(id: string): Instructor | undefined {
    return this.instructors().find((i) => i.id === id);
  }

  coursesByInstructor(id: string): number {
    return this.published().filter((c) => c.instructorId === id).length;
  }

  related(course: Course, count = 4): Course[] {
    const others = this.published().filter((c) => c.id !== course.id);
    const sameCategory = others.filter((c) => c.category === course.category);
    const otherCategories = others.filter((c) => c.category !== course.category);
    return [...sameCategory, ...otherCategories].slice(0, count);
  }

  /** Deterministic subset of the demo review pool for a course. */
  reviews(course: Course, count = 3): Review[] {
    const start = course.id % REVIEWS.length;
    return Array.from({ length: count }, (_, i) => REVIEWS[(start + i) % REVIEWS.length]);
  }

  /** Demo star distribution (percentages for 5★ … 1★) derived from the average rating. */
  ratingBreakdown(course: Course): number[] {
    const five = Math.max(0, Math.round((course.rating - 4) * 85));
    const four = Math.max(0, 92 - five);
    return [five, four, 5, 2, 1].map((v) => Math.min(100, v));
  }

  // ── Admin ───────────────────────────────────────────────────

  /** Time-based so a deleted course's id is never reused by payment records that mention it. */
  nextCourseId(): number {
    return Date.now();
  }

  isSlugTaken(slug: string, exceptId?: number): boolean {
    return this.courses().some((c) => c.slug === slug && c.id !== exceptId);
  }

  saveCourse(course: Course): Promise<Course> {
    return this.courseStore.save(course);
  }

  removeCourse(id: number): Promise<void> {
    return this.courseStore.remove(id);
  }

  saveInstructor(instructor: Instructor): Promise<Instructor> {
    return this.instructorStore.save(instructor);
  }

  removeInstructor(id: string): Promise<void> {
    return this.instructorStore.remove(id);
  }

  /** Courses that would lose their instructor, whether published or not. */
  assignedCourses(instructorId: string): Course[] {
    return this.courses().filter((c) => c.instructorId === instructorId);
  }

  async reset(): Promise<void> {
    await Promise.all([this.courseStore.reset(), this.instructorStore.reset()]);
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
