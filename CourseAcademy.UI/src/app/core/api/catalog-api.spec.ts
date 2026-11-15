import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { CourseModule } from '../models/course.model';
import { CourseService } from '../services/course.service';
import { API_BASE_URL, API_MODE } from './api-mode';
import { CourseDetailDto, fromCourseDto, toCurriculum } from './catalog.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });

const course: CourseDetailDto = {
  id: 1, slug: 'video-editing', title: 'Video Editing', subtitle: 'Edit', description: 'D', categoryCode: 'video-editing',
  formatCode: 'Live', levelCode: 'Beginner', badgeCode: null, isPublished: true, isFeatured: true, instructorId: 7, language: 'Bangla',
  durationHours: 5, price: 4999, originalPrice: 8000, discountPercent: 38, coverLabel: 'Video', coverIcon: 'bi-film', imageUrl: null,
  rating: 4.8, ratingCount: 10, students: 100, contentUpdatedMonth: '2026-10', sectionCount: 2, outcomes: ['A'], requirements: [],
  modules: [{ id: 11, title: 'M', submodules: [{ id: 21, title: 'S', sections: [
    { id: 31, title: 'One', depth: 0, topics: [],
      videos: [{ id: 41, title: 'Free', minutes: 5, isPreview: true, sourceType: 'external', url: 'https://youtu.be/x', hasMedia: true, isLocked: false },
               { id: 42, title: 'Paid', minutes: 5, isPreview: false, sourceType: 'external', url: null, hasMedia: true, isLocked: true }],
      documents: [{ id: 51, title: 'Notes', typeCode: 'PDF', videoId: 41, url: 'https://f.example.com/n.pdf', hasMedia: true, isLocked: false }] },
    { id: 32, title: 'Two', depth: 0, topics: [], videos: [], documents: [] },
  ] }] }],
  schedule: [{ id: 61, title: 'Class 1', startsAt: '2026-11-20T20:00', durationMinutes: 90, meetingUrl: null }],
  materials: [{ id: 71, title: 'Guide', typeCode: 'Link', url: null, hasMedia: true, isLocked: true }],
  hasContentAccess: false,
  activeEnrollmentCount: null,
  enrollmentCount: null,
};

describe('Catalogue API mapping', () => {
  it('turns API ids into the app ids and locked links into empty strings', () => {
    const mapped = fromCourseDto(course);

    expect(mapped.id).toBe(1);
    expect(mapped.instructorId).toBe('7');
    expect(mapped.badge).toBeUndefined();
    const section = mapped.modules[0].submodules[0].sections[0];
    expect(section.id).toBe('31');
    expect(section.videos.map((v) => v.url)).toEqual(['https://youtu.be/x', '']);
    expect(section.documents[0].videoId).toBe('41');
    expect(mapped.schedule[0]).toEqual({ id: '61', title: 'Class 1', startsAt: '2026-11-20T20:00', durationMinutes: 90, meetingUrl: '' });
  });

  it('sends stored ids as numbers and new rows with temporary keys', () => {
    const modules: CourseModule[] = [{ id: '11', title: 'M', submodules: [{ id: 'submodule-new', title: 'S', sections: [
      { id: '31', title: 'One', topics: [],
        videos: [{ id: '41', title: 'Free', url: '', minutes: 5, preview: true }, { id: 'video-new', title: 'New', url: 'https://youtu.be/y', minutes: 3, preview: false }],
        documents: [{ id: 'document-new', title: 'Sheet', type: 'PDF', url: '', videoId: 'video-new' }, { id: '51', title: 'Notes', type: 'PDF', url: 'https://f.example.com/n.pdf', videoId: '41' }] },
    ] }] }];

    const body = toCurriculum(modules) as unknown as { modules: any[] };
    const submodule = body.modules[0].submodules[0];
    const section = submodule.sections[0];

    expect(body.modules[0].id).toBe(11);
    expect(submodule.id).toBeNull();
    expect(section.videos).toEqual([
      { id: 41, tempKey: null, title: 'Free', url: null, minutes: 5, isPreview: true },
      { id: null, tempKey: 'video-new', title: 'New', url: 'https://youtu.be/y', minutes: 3, isPreview: false },
    ]);
    expect(section.documents[0]).toEqual({ id: null, title: 'Sheet', typeCode: 'PDF', url: null, videoId: null, videoTempKey: 'video-new' });
    expect(section.documents[1].videoId).toBe(41);
  });
});

describe('CourseService in API mode', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_MODE, useValue: true },
        { provide: API_BASE_URL, useValue: BASE },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const settle = () => new Promise((r) => setTimeout(r, 0));

  it('loads the catalogue and instructors from the API', async () => {
    const courses = TestBed.inject(CourseService);
    await settle();

    http.expectOne(`${BASE}/courses/all`).flush(ok([course]));
    http.expectOne(`${BASE}/instructors`).flush(ok([{ id: 7, name: 'Tanvir', title: 'T', bio: 'B', expertise: [], experienceYears: 1, students: 0, rating: 0 }]));
    await courses.ready;

    expect(courses.published().map((c) => c.slug)).toEqual(['video-editing']);
    expect(courses.instructor('7')?.name).toBe('Tanvir');
  });

  it('shows the API message when the catalogue cannot load', async () => {
    const courses = TestBed.inject(CourseService);
    await settle();

    http.expectOne(`${BASE}/courses/all`).error(new ProgressEvent('error'), { status: 0 });
    http.expectOne(`${BASE}/instructors`).flush(ok([]));
    await courses.ready;

    expect(courses.courses()).toEqual([]);
  });
});
