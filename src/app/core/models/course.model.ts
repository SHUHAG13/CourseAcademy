export type CategoryId = 'video-editing' | 'sports-journalism' | 'software-development';

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
  title: string;
  minutes: number;
  preview?: boolean;
}

export interface CurriculumSection {
  title: string;
  lessons: Lesson[];
}

export interface Course {
  id: number;
  slug: string;
  title: string;
  subtitle: string;
  category: CategoryId;
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
  badge?: 'Bestseller' | 'New' | 'Popular';
  featured?: boolean;
  updated: string;
  outcomes: string[];
  requirements: string[];
  curriculum: CurriculumSection[];
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
  sort: CourseSort;
}
