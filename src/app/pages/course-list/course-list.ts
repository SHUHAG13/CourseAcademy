import { DOCUMENT, NgTemplateOutlet } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  COURSE_FORMATS,
  CategoryId,
  CourseFilter,
  CourseFormat,
  CourseSort,
  Level,
  PriceRange,
} from '../../core/models/course.model';
import { CourseService, DEFAULT_FILTER, PRICE_RANGES, filterCourses } from '../../core/services/course.service';
import { CourseCard } from '../../shared/components/course-card/course-card';
import { CourseCardSkeleton } from '../../shared/components/course-card/course-card-skeleton';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

const PAGE_SIZE = 6;

@Component({
  selector: 'app-course-list',
  imports: [FormsModule, NgTemplateOutlet, RouterLink, CourseCard, CourseCardSkeleton, EmptyState],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss',
  host: { '(document:keydown.escape)': 'filtersOpen.set(false)' },
})
export class CourseList {
  private readonly courseService = inject(CourseService);
  private readonly router = inject(Router);

  /** Bound from the `?category=` and `?q=` query params. */
  readonly category = input<string>();
  readonly q = input<string>();

  protected readonly categories = this.courseService.categories;
  protected readonly priceRanges = PRICE_RANGES;
  protected readonly levelOptions: Level[] = ['Beginner', 'Intermediate', 'Advanced'];
  protected readonly formatOptions = COURSE_FORMATS;
  protected readonly sortOptions: { id: CourseSort; label: string }[] = [
    { id: 'popular', label: 'Most popular' },
    { id: 'rating', label: 'Highest rated' },
    { id: 'newest', label: 'Newest' },
    { id: 'price-asc', label: 'Price: low to high' },
    { id: 'price-desc', label: 'Price: high to low' },
  ];

  protected readonly loading = signal(true);
  protected readonly courses = this.courseService.published;
  protected readonly filtersOpen = signal(false);

  protected readonly activeCategory = computed<CategoryId | 'all'>(() => {
    const c = this.category();
    return this.courseService.isCategory(c) ? c : 'all';
  });
  protected readonly search = linkedSignal(() => this.q() ?? '');
  protected readonly price = signal<PriceRange>(DEFAULT_FILTER.price);
  protected readonly levels = signal<Level[]>([]);
  protected readonly formats = signal<CourseFormat[]>([]);
  protected readonly sort = signal<CourseSort>(DEFAULT_FILTER.sort);

  protected readonly filter = computed<CourseFilter>(() => ({
    search: this.search(),
    category: this.activeCategory(),
    price: this.price(),
    levels: this.levels(),
    formats: this.formats(),
    sort: this.sort(),
  }));

  protected readonly results = computed(() => filterCourses(this.courses(), this.filter()));
  protected readonly visibleCount = linkedSignal({ source: this.filter, computation: () => PAGE_SIZE });
  protected readonly visible = computed(() => this.results().slice(0, this.visibleCount()));
  protected readonly remaining = computed(() => this.results().length - this.visible().length);

  protected readonly heading = computed(() => {
    const id = this.activeCategory();
    return id === 'all' ? null : this.courseService.category(id);
  });

  protected readonly activeFilterCount = computed(
    () =>
      (this.price() !== 'all' ? 1 : 0) +
      this.levels().length +
      this.formats().length +
      (this.activeCategory() !== 'all' ? 1 : 0),
  );

  protected readonly skeletons = Array.from({ length: 4 });

  constructor() {
    this.courseService.whenLoaded().then(() => this.loading.set(false));

    // Lock page scroll while the mobile filter drawer is open.
    const body = inject(DOCUMENT).body;
    effect(() => {
      body.style.overflow = this.filtersOpen() ? 'hidden' : '';
    });
    inject(DestroyRef).onDestroy(() => (body.style.overflow = ''));
  }

  protected countFor(category: CategoryId | 'all'): number {
    const all = this.courses();
    return category === 'all' ? all.length : all.filter((c) => c.category === category).length;
  }

  protected levelCount(level: Level): number {
    return this.courses().filter(
      (c) => c.level === level && (this.activeCategory() === 'all' || c.category === this.activeCategory()),
    ).length;
  }

  protected formatCount(format: CourseFormat): number {
    return this.courses().filter(
      (c) => c.format === format && (this.activeCategory() === 'all' || c.category === this.activeCategory()),
    ).length;
  }

  protected setCategory(id: CategoryId | 'all'): void {
    this.router.navigate(['/courses'], {
      queryParams: { category: id === 'all' ? null : id },
      queryParamsHandling: 'merge',
    });
  }

  protected toggleLevel(level: Level): void {
    this.levels.update((list) => (list.includes(level) ? list.filter((l) => l !== level) : [...list, level]));
  }

  protected toggleFormat(format: CourseFormat): void {
    this.formats.update((list) => (list.includes(format) ? list.filter((f) => f !== format) : [...list, format]));
  }

  protected priceLabel(id: PriceRange): string {
    return this.priceRanges.find((p) => p.id === id)!.label;
  }

  protected clearAll(): void {
    this.search.set('');
    this.price.set('all');
    this.levels.set([]);
    this.formats.set([]);
    this.sort.set('popular');
    if (this.activeCategory() !== 'all' || this.q()) {
      this.router.navigate(['/courses']);
    }
  }

  protected loadMore(): void {
    this.visibleCount.update((n) => n + PAGE_SIZE);
  }
}
