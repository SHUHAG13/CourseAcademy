export type CategoryId = 'video-editing' | 'digital-marketing' | 'sports-journalism' | 'software-development';

export type CourseFormat = 'Pre-recorded' | 'Live' | 'Hybrid';

export const COURSE_FORMATS: CourseFormat[] = ['Pre-recorded', 'Live', 'Hybrid'];

export type Level = 'Beginner' | 'Intermediate' | 'Advanced';

export type CourseSort = 'popular' | 'rating' | 'price-asc' | 'price-desc' | 'newest';

export type PriceRange = 'all' | 'under-2500' | '2500-4500' | 'over-4500';

export interface Category {
  id: CategoryId;
  name: string;
  shortName: string;
  description: string;
  icon: string;
  image: string;
  skills: string[];
}

export interface Instructor {
  id: string;
  name: string;
  title: string;
  bio: string;
  expertise: string[];
  experienceYears: number;
  students: number;
  rating: number;
}

export interface Lesson {
  id: string;
  title: string;
  minutes: number;
  preview?: boolean;
}

export interface CurriculumSection {
  id: string;
  title: string;
  lessons: Lesson[];
}

export interface LiveSession {
  id: string;
  title: string;
  /** Local date-time in `YYYY-MM-DDTHH:mm` form, as produced by a datetime-local input. */
  startsAt: string;
  durationMinutes: number;
  /** Placeholder only: no meeting provider is connected. */
  meetingUrl: string;
}

export type MaterialType = 'PDF' | 'Slides' | 'Worksheet' | 'Link';

export const MATERIAL_TYPES: MaterialType[] = ['PDF', 'Slides', 'Worksheet', 'Link'];

export interface Material {
  id: string;
  title: string;
  type: MaterialType;
  url: string;
}

export interface Course {
  id: number;
  slug: string;
  title: string;
  subtitle: string;
  description: string;
  category: CategoryId;
  format: CourseFormat;
  /** Hidden from the public catalogue until true. */
  published: boolean;
  instructorId: string;
  rating: number;
  ratingCount: number;
  students: number;
  durationHours: number;
  level: Level;
  language: string;
  price: number;
  originalPrice: number;
  /** Short label printed on the generated course cover. */
  coverLabel: string;
  coverIcon: string;
  /** Optional photo. Courses without one keep the generated cover. */
  image?: string;
  badge?: 'Bestseller' | 'New' | 'Popular';
  featured?: boolean;
  updated: string;
  outcomes: string[];
  requirements: string[];
  curriculum: CurriculumSection[];
  schedule: LiveSession[];
  materials: Material[];
}

export interface Review {
  name: string;
  rating: number;
  date: string;
  text: string;
}

export interface CourseFilter {
  search: string;
  category: CategoryId | 'all';
  price: PriceRange;
  levels: Level[];
  formats: CourseFormat[];
  sort: CourseSort;
}
