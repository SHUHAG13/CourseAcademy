import {
  CategoryId,
  Course,
  CourseFormat,
  CourseModule,
  DocumentType,
  Instructor,
  Level,
  MaterialType,
  MediaFile,
  MediaSource,
  MediaStatus,
  Section,
} from '../models/course.model';

// TypeScript shapes of the API's catalogue DTOs, and their conversion to and from the app's models.
// The API's numeric ids become strings in the app (except course ids, which are numbers in both).

export interface InstructorDto {
  id: number;
  name: string;
  title: string;
  bio: string;
  expertise: string[];
  experienceYears: number;
  students: number;
  rating: number;
}

/** An uploaded file; only sent to people who manage the course. */
export interface MediaAssetDto {
  id: number;
  courseId: number;
  kindCode: 'video' | 'document';
  statusCode: MediaStatus;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  durationSeconds: number | null;
  errorMessage: string | null;
}

export interface VideoDto {
  id: number;
  title: string;
  minutes: number;
  isPreview: boolean;
  sourceType: 'external' | 'upload' | null;
  /** Null when locked for this caller, or for uploads (played through the media endpoints). */
  url: string | null;
  hasMedia: boolean;
  isLocked: boolean;
  media: MediaAssetDto | null;
}

export interface DocumentDto {
  id: number;
  title: string;
  typeCode: DocumentType;
  videoId: number | null;
  sourceType: 'external' | 'upload' | null;
  url: string | null;
  hasMedia: boolean;
  isLocked: boolean;
  media: MediaAssetDto | null;
}

export interface SectionDto {
  id: number;
  title: string;
  depth: number;
  videos: VideoDto[];
  documents: DocumentDto[];
  topics: SectionDto[];
}

export interface ModuleDto {
  id: number;
  title: string;
  submodules: { id: number; title: string; sections: SectionDto[] }[];
}

export interface CourseDetailDto {
  id: number;
  slug: string;
  title: string;
  subtitle: string;
  description: string;
  categoryCode: CategoryId;
  formatCode: CourseFormat;
  levelCode: Level;
  badgeCode: Course['badge'] | null;
  isPublished: boolean;
  isFeatured: boolean;
  instructorId: number;
  language: string;
  durationHours: number;
  price: number;
  originalPrice: number;
  discountPercent: number;
  coverLabel: string;
  coverIcon: string;
  imageUrl: string | null;
  rating: number;
  ratingCount: number;
  students: number;
  contentUpdatedMonth: string;
  sectionCount: number;
  outcomes: string[];
  requirements: string[];
  modules: ModuleDto[];
  /** startsAt is Bangladesh local time "YYYY-MM-DDTHH:mm". */
  schedule: { id: number; title: string; startsAt: string; durationMinutes: number; meetingUrl: string | null }[];
  materials: {
    id: number;
    title: string;
    typeCode: MaterialType;
    sourceType: 'external' | 'upload' | null;
    url: string | null;
    hasMedia: boolean;
    isLocked: boolean;
    media: MediaAssetDto | null;
  }[];
  hasContentAccess: boolean;
  /** Only for people who manage the course. */
  activeEnrollmentCount: number | null;
  enrollmentCount: number | null;
}

export function fromInstructorDto(i: InstructorDto): Instructor {
  return { ...i, id: String(i.id) };
}

export function fromMediaAssetDto(m: MediaAssetDto): MediaFile {
  return {
    id: m.id,
    status: m.statusCode,
    fileName: m.fileName,
    sizeBytes: m.sizeBytes,
    durationSeconds: m.durationSeconds ?? undefined,
    errorMessage: m.errorMessage ?? undefined,
  };
}

/** Where an item's media lives. `mediaId` is only known to managers (it comes with `media`). */
function fromSource(item: { sourceType: 'external' | 'upload' | null; media: MediaAssetDto | null }): MediaSource {
  return {
    source: item.sourceType ?? undefined,
    mediaId: item.media?.id,
    media: item.media ? fromMediaAssetDto(item.media) : undefined,
  };
}

function fromSection(s: SectionDto): Section {
  return {
    id: String(s.id),
    title: s.title,
    videos: s.videos.map((v) => ({
      id: String(v.id),
      title: v.title,
      url: v.url ?? '',
      minutes: v.minutes,
      preview: v.isPreview,
      ...fromSource(v),
    })),
    documents: s.documents.map((d) => ({
      id: String(d.id),
      title: d.title,
      type: d.typeCode,
      url: d.url ?? '',
      videoId: d.videoId === null ? undefined : String(d.videoId),
      ...fromSource(d),
    })),
    topics: s.topics.map(fromSection),
  };
}

export function fromCourseDto(c: CourseDetailDto): Course {
  return {
    id: c.id,
    slug: c.slug,
    title: c.title,
    subtitle: c.subtitle,
    description: c.description,
    category: c.categoryCode,
    format: c.formatCode,
    published: c.isPublished,
    instructorId: String(c.instructorId),
    rating: c.rating,
    ratingCount: c.ratingCount,
    students: c.students,
    durationHours: c.durationHours,
    level: c.levelCode,
    language: c.language,
    price: c.price,
    originalPrice: c.originalPrice,
    coverLabel: c.coverLabel,
    coverIcon: c.coverIcon,
    image: c.imageUrl ?? undefined,
    badge: c.badgeCode ?? undefined,
    featured: c.isFeatured,
    updated: c.contentUpdatedMonth,
    outcomes: c.outcomes,
    requirements: c.requirements,
    modules: c.modules.map((m) => ({
      id: String(m.id),
      title: m.title,
      submodules: m.submodules.map((sm) => ({ id: String(sm.id), title: sm.title, sections: sm.sections.map(fromSection) })),
    })),
    schedule: c.schedule.map((s) => ({ id: String(s.id), title: s.title, startsAt: s.startsAt, durationMinutes: s.durationMinutes, meetingUrl: s.meetingUrl ?? '' })),
    materials: c.materials.map((m) => ({ id: String(m.id), title: m.title, type: m.typeCode, url: m.url ?? '', ...fromSource(m) })),
    enrollmentCounts:
      c.activeEnrollmentCount === null || c.enrollmentCount === null
        ? undefined
        : { active: c.activeEnrollmentCount, total: c.enrollmentCount },
  };
}

/** A stored row's id as the API's number, or null for rows created in the browser (ids like "section-lx2k…"). */
function storedId(id: string): number | null {
  return /^\d+$/.test(id) ? Number(id) : null;
}

/** Body of POST/PUT /admin/courses. */
export function toCourseUpsert(course: Course) {
  return {
    title: course.title,
    slug: course.slug,
    subtitle: course.subtitle,
    description: course.description,
    categoryCode: course.category,
    formatCode: course.format,
    levelCode: course.level,
    badgeCode: course.badge ?? null,
    instructorId: Number(course.instructorId),
    language: course.language,
    durationHours: course.durationHours,
    originalPrice: course.originalPrice,
    price: course.price,
    coverLabel: course.coverLabel,
    coverIcon: course.coverIcon,
    imageUrl: course.image ?? null,
    isFeatured: !!course.featured,
    isPublished: course.published,
    outcomes: course.outcomes,
    requirements: course.requirements,
    schedule: course.schedule.map((s) => ({
      id: storedId(s.id),
      title: s.title,
      startsAt: s.startsAt,
      durationMinutes: s.durationMinutes,
      meetingUrl: s.meetingUrl,
    })),
    materials: course.materials.map((m) => ({
      id: storedId(m.id),
      title: m.title,
      typeCode: m.type,
      url: m.mediaId ? null : m.url || null,
      mediaAssetId: m.mediaId ?? null,
    })),
  };
}

function toSectionInput(s: Section): unknown {
  return {
    id: storedId(s.id),
    title: s.title,
    // New videos carry their browser id as a key, so new documents can point at them in the same save.
    // An upload is sent as its file id; an older video keeps sending its unchanged link.
    videos: s.videos.map((v) => ({
      id: storedId(v.id),
      tempKey: storedId(v.id) === null ? v.id : null,
      title: v.title,
      url: v.mediaId ? null : v.url || null,
      minutes: v.minutes,
      isPreview: v.preview,
      mediaAssetId: v.mediaId ?? null,
    })),
    documents: s.documents.map((d) => ({
      id: storedId(d.id),
      title: d.title,
      typeCode: d.type,
      url: d.mediaId ? null : d.url || null,
      videoId: d.videoId ? storedId(d.videoId) : null,
      videoTempKey: d.videoId && storedId(d.videoId) === null ? d.videoId : null,
      mediaAssetId: d.mediaId ?? null,
    })),
    topics: s.topics.map(toSectionInput),
  };
}

/** Body of PUT /admin/courses/{id}/curriculum. */
export function toCurriculum(modules: CourseModule[]) {
  return {
    modules: modules.map((m) => ({
      id: storedId(m.id),
      title: m.title,
      submodules: m.submodules.map((sm) => ({ id: storedId(sm.id), title: sm.title, sections: sm.sections.map(toSectionInput) })),
    })),
  };
}

/** Body of POST/PUT /instructors. */
export function toInstructorSave(i: Instructor) {
  return { name: i.name, title: i.title, bio: i.bio, expertise: i.expertise, experienceYears: i.experienceYears, students: i.students };
}

export function isStoredId(id: string): boolean {
  return storedId(id) !== null;
}
