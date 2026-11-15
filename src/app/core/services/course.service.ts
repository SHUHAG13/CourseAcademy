import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { API_MODE } from '../api/api-mode';
import { ApiService } from '../api/api.service';
import {
  CourseDetailDto,
  InstructorDto,
  fromCourseDto,
  fromInstructorDto,
  isStoredId,
  toCourseUpsert,
  toCurriculum,
  toInstructorSave,
} from '../api/catalog.models';
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
import { AuthService } from './auth.service';
import { PermissionService } from './permission.service';
import { ToastService } from './toast.service';

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
    // A document without a link is allowed: its file is added later.
    if (sec.documents.some((d) => d.url && !documentUrlMatchesType(d.url, d.type))) {
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
  private readonly apiMode = inject(API_MODE);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly courseStore = new EntityStore(inject(COURSE_REPOSITORY));
  private readonly instructorStore = new EntityStore(inject(INSTRUCTOR_REPOSITORY));
  private readonly permissions = inject(PermissionService);

  // API mode: the catalogue the signed-in user may see, kept in memory like the demo data.
  private readonly apiCourses = signal<Course[]>([]);
  private readonly apiInstructors = signal<Instructor[]>([]);
  /** The user the API catalogue was loaded for; drafts and links depend on who is signed in. */
  private loadedFor: number | null | undefined;

  readonly categories: Category[] = CATEGORIES;

  readonly ready: Promise<unknown> = this.apiMode
    ? this.auth.ready.then(() => this.loadCatalog())
    : Promise.all([this.courseStore.ready, this.instructorStore.ready]);
  readonly recovered = computed(() => !this.apiMode && (this.courseStore.recovered() || this.instructorStore.recovered()));

  /** Every course this user may see, including unpublished ones they manage. For the admin area. */
  readonly courses = computed(() => (this.apiMode ? this.apiCourses() : this.courseStore.items()));
  /** What visitors can see. */
  readonly published = computed(() => this.courses().filter((c) => c.published));
  readonly instructors = computed(() => (this.apiMode ? this.apiInstructors() : this.instructorStore.items()));

  constructor() {
    if (this.apiMode) {
      // Signing in or out changes which drafts and lesson links the API returns.
      effect(() => {
        const id = this.auth.currentSession()?.id ?? null;
        untracked(() => {
          if (this.loadedFor !== undefined && this.loadedFor !== id) void this.loadCatalog();
        });
      });
    }
  }

  /** API mode: (re)loads courses and instructors. A failure leaves an empty catalogue and tells the user. */
  async loadCatalog(): Promise<void> {
    if (!this.apiMode) return;
    this.loadedFor = this.auth.currentSession()?.id ?? null;
    try {
      const [courses, instructors] = await Promise.all([
        this.api.get<CourseDetailDto[]>('courses/all'),
        this.api.get<InstructorDto[]>('instructors'),
      ]);
      this.apiCourses.set(courses.map(fromCourseDto));
      this.apiInstructors.set(instructors.map(fromInstructorDto));
    } catch (error) {
      this.toast.error((error as Error).message);
    }
  }

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

  /** Saves the course details. In API mode the curriculum is saved separately with `saveCurriculum`. */
  async saveCourse(course: Course): Promise<Course> {
    const existing = this.byId(course.id);
    if (existing) {
      this.permissions.assert('courses.edit', course.id);
    } else {
      this.permissions.assert('courses.create');
    }
    const error = curriculumError(course.modules);
    if (error) throw new Error(error);
    if (this.apiMode) {
      const saved = existing
        ? await this.api.put<CourseDetailDto>(`admin/courses/${course.id}`, toCourseUpsert(course))
        : await this.api.post<CourseDetailDto>('admin/courses', toCourseUpsert(course));
      return this.storeApiCourse(saved);
    }
    return this.courseStore.save(course);
  }

  /** Saves the curriculum of a course (modules, submodules, sections, videos, documents). */
  async saveCurriculum(course: Course, modules: CourseModule[]): Promise<Course> {
    this.permissions.assert('courses.edit', course.id);
    const error = curriculumError(modules);
    if (error) throw new Error(error);
    if (this.apiMode) {
      return this.storeApiCourse(await this.api.put<CourseDetailDto>(`admin/courses/${course.id}/curriculum`, toCurriculum(modules)));
    }
    return this.courseStore.save({ ...course, modules });
  }

  async setPublished(course: Course, published: boolean): Promise<Course> {
    this.permissions.assert('courses.edit', course.id);
    if (this.apiMode) {
      return this.storeApiCourse(await this.api.patch<CourseDetailDto>(`admin/courses/${course.id}/publish`, { isPublished: published }));
    }
    return this.courseStore.save({ ...course, published });
  }

  async removeCourse(id: number): Promise<void> {
    this.permissions.assert('courses.delete', id);
    if (this.apiMode) {
      await this.api.delete(`admin/courses/${id}`);
      this.apiCourses.update((list) => list.filter((c) => c.id !== id));
      return;
    }
    return this.courseStore.remove(id);
  }

  async saveInstructor(instructor: Instructor): Promise<Instructor> {
    const existing = this.instructor(instructor.id);
    this.permissions.assert(existing ? 'instructors.edit' : 'instructors.create');
    if (this.apiMode) {
      const saved =
        existing && isStoredId(instructor.id)
          ? await this.api.put<InstructorDto>(`instructors/${instructor.id}`, toInstructorSave(instructor))
          : await this.api.post<InstructorDto>('instructors', toInstructorSave(instructor));
      const mapped = fromInstructorDto(saved);
      this.apiInstructors.update((list) =>
        list.some((i) => i.id === mapped.id) ? list.map((i) => (i.id === mapped.id ? mapped : i)) : [...list, mapped],
      );
      return mapped;
    }
    return this.instructorStore.save(instructor);
  }

  async removeInstructor(id: string): Promise<void> {
    this.permissions.assert('instructors.delete');
    if (this.apiMode) {
      await this.api.delete(`instructors/${id}`);
      this.apiInstructors.update((list) => list.filter((i) => i.id !== id));
      return;
    }
    return this.instructorStore.remove(id);
  }

  /** Puts a course returned by the API into the in-memory catalogue. */
  private storeApiCourse(dto: CourseDetailDto): Course {
    const course = fromCourseDto(dto);
    this.apiCourses.update((list) =>
      list.some((c) => c.id === course.id) ? list.map((c) => (c.id === course.id ? course : c)) : [...list, course],
    );
    return course;
  }

  /** Courses that would lose their instructor, whether published or not. */
  assignedCourses(instructorId: string): Course[] {
    return this.courses().filter((c) => c.instructorId === instructorId);
  }

  async reset(): Promise<void> {
    // Courses in API mode live on the server; demo reset does not touch them.
    if (this.apiMode) return;
    await Promise.all([this.courseStore.reset(), this.instructorStore.reset()]);
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
