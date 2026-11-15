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

// Curriculum: Course → Module → Submodule → Section. Array order is display order.

export interface SectionVideo {
  id: string;
  title: string;
  /** YouTube, Vimeo or a direct .mp4/.webm link. Anything else shows a placeholder player. */
  url: string;
  minutes: number;
  /** Visitors who are not enrolled can watch it, and open its documents. */
  preview: boolean;
}

export type DocumentType = 'PDF' | 'DOCX' | 'PPTX';

export const DOCUMENT_TYPES: DocumentType[] = ['PDF', 'DOCX', 'PPTX'];

export interface SectionDocument {
  id: string;
  title: string;
  type: DocumentType;
  /** Must end in the file extension of `type`. */
  url: string;
  /** The video this document belongs to, shown right under it. Absent for documents for the whole section. */
  videoId?: string;
}

export interface Section {
  id: string;
  title: string;
  videos: SectionVideo[];
  documents: SectionDocument[];
  /** Sub-topics, e.g. "Static variables" under "Variables, conditions and loops". Each is a section of its own. */
  topics: Section[];
}

/** How many levels of topics a section can have below it. */
export const MAX_TOPIC_DEPTH = 3;

/** Sections and all their topics, in reading order: each section, then its topics. */
export function flattenSections(sections: Section[]): Section[] {
  return sections.flatMap((s) => [s, ...flattenSections(s.topics)]);
}

export const MIN_SECTIONS_PER_SUBMODULE = 2;
export const MAX_SECTIONS_PER_SUBMODULE = 3;

export interface Submodule {
  id: string;
  title: string;
  /** 2 or 3 sections. */
  sections: Section[];
}

export interface CourseModule {
  id: string;
  title: string;
  submodules: Submodule[];
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
  modules: CourseModule[];
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
