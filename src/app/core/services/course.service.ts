import { Injectable, computed, inject } from '@angular/core';
import { CATEGORIES, REVIEWS } from '../data/courses.data';
import { COURSE_REPOSITORY, INSTRUCTOR_REPOSITORY } from '../data/repositories';
import { EntityStore } from '../data/repository';
import {
  Category,
  CategoryId,
  Course,
  CourseFilter,
  CourseModule,
  DocumentType,
  MAX_SECTIONS_PER_SUBMODULE,
  MAX_TOPIC_DEPTH,
  MIN_SECTIONS_PER_SUBMODULE,
  Section,
  flattenSections,
  Instructor,
  PriceRange,
  Review,
} from '../models/course.model';
import { PermissionService } from './permission.service';

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

/** Every section and topic of a course or one module, in curriculum order. */
export function allSections(from: Pick<Course, 'modules'> | CourseModule): Section[] {
  const modules = 'modules' in from ? from.modules : [from];
  return modules.flatMap((m) => m.submodules.flatMap((s) => flattenSections(s.sections)));
}

export function sectionCount(course: Pick<Course, 'modules'>): number {
  return allSections(course).length;
}

export function sectionMinutes(section: Section): number {
  return section.videos.reduce((n, v) => n + v.minutes, 0);
}

/** Video minutes of a module, topics included. */
export function moduleMinutes(module: CourseModule): number {
  return allSections(module).reduce((n, sec) => n + sectionMinutes(sec), 0);
}

const DOCUMENT_EXTENSIONS: Record<DocumentType, string> = { PDF: '.pdf', DOCX: '.docx', PPTX: '.pptx' };

/** Whether a document link ends in the extension of its type, ignoring any `?query` or `#hash`. */
export function documentUrlMatchesType(url: string, type: DocumentType): boolean {
  return url.split(/[?#]/)[0].toLowerCase().endsWith(DOCUMENT_EXTENSIONS[type]);
}

/** The first rule a curriculum breaks, as English text for a message, or null if it is valid. */
export function curriculumError(modules: CourseModule[]): string | null {
  for (const m of modules) {
    if (!m.title.trim()) return 'Every module needs a title.';
    for (const sub of m.submodules) {
      if (!sub.title.trim()) return 'Every submodule needs a title.';
      if (sub.sections.length < MIN_SECTIONS_PER_SUBMODULE || sub.sections.length > MAX_SECTIONS_PER_SUBMODULE) {
        return 'Each submodule needs 2 or 3 sections.';
      }
      const error = sectionsError(sub.sections, 0);
      if (error) return error;
    }
  }
  return null;
}

/** Checks sections and, level by level, their topics. */
function sectionsError(sections: Section[], depth: number): string | null {
  if (sections.length && depth > MAX_TOPIC_DEPTH) return 'Topics can be nested at most 3 levels deep.';
  for (const sec of sections) {
    if (!sec.title.trim()) return depth ? 'Every topic needs a title.' : 'Every section needs a title.';
    if (sec.documents.some((d) => !documentUrlMatchesType(d.url, d.type))) {
      return 'A document link must end in .pdf, .docx or .pptx to match its type.';
    }
    if (sec.documents.some((d) => d.videoId && !sec.videos.some((v) => v.id === d.videoId))) {
      return 'A document is attached to a video that is not in its section.';
    }
    const error = sectionsError(sec.topics, depth + 1);
    if (error) return error;
  }
  return null;
}

@Injectable({ providedIn: 'root' })
export class CourseService {
  private readonly courseStore = new EntityStore(inject(COURSE_REPOSITORY));
  private readonly instructorStore = new EntityStore(inject(INSTRUCTOR_REPOSITORY));
  private readonly permissions = inject(PermissionService);

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

  async saveCourse(course: Course): Promise<Course> {
    if (this.byId(course.id)) {
      this.permissions.assert('courses.edit', course.id);
    } else {
      this.permissions.assert('courses.create');
    }
    const error = curriculumError(course.modules);
    if (error) throw new Error(error);
    return this.courseStore.save(course);
  }

  async removeCourse(id: number): Promise<void> {
    this.permissions.assert('courses.delete', id);
    return this.courseStore.remove(id);
  }

  async saveInstructor(instructor: Instructor): Promise<Instructor> {
    this.permissions.assert(this.instructor(instructor.id) ? 'instructors.edit' : 'instructors.create');
    return this.instructorStore.save(instructor);
  }

  async removeInstructor(id: string): Promise<void> {
    this.permissions.assert('instructors.delete');
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
