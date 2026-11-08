import { COURSES } from '../data/courses.data';
import { CourseFilter, Section } from '../models/course.model';
import {
  DEFAULT_FILTER,
  allSections,
  curriculumError,
  discountPercent,
  documentUrlMatchesType,
  filterCourses,
} from './course.service';

const run = (patch: Partial<CourseFilter>) => filterCourses(COURSES, { ...DEFAULT_FILTER, ...patch });

describe('filterCourses', () => {
  it('returns every course with the default filter, most popular first', () => {
    const result = run({});
    expect(result.length).toBe(COURSES.length);
    for (let i = 1; i < result.length; i++) {
      expect(result[i - 1].students).toBeGreaterThanOrEqual(result[i].students);
    }
  });

  it('filters by category', () => {
    const result = run({ category: 'video-editing' });
    expect(result.length).toBeGreaterThan(0);
    expect(result.every((c) => c.category === 'video-editing')).toBe(true);
  });

  it('searches title, subtitle and cover label case-insensitively', () => {
    expect(run({ search: 'POST-PRODUCTION' }).map((c) => c.slug)).toContain('video-editing-post-production');
    expect(run({ search: 'marketing' }).length).toBeGreaterThan(0);
    expect(run({ search: 'no-such-course-xyz' })).toEqual([]);
  });

  it('filters by price range', () => {
    expect(run({ price: 'under-2500' }).every((c) => c.price < 2500)).toBe(true);
    expect(run({ price: '2500-4500' }).every((c) => c.price >= 2500 && c.price <= 4500)).toBe(true);
    expect(run({ price: 'over-4500' }).every((c) => c.price > 4500)).toBe(true);
  });

  it('filters by one or more levels', () => {
    const result = run({ levels: ['Beginner'] });
    expect(result.length).toBeGreaterThan(0);
    expect(result.every((c) => c.level === 'Beginner')).toBe(true);
    expect(run({ levels: ['Advanced'] })).toEqual([]);
  });

  it('filters by delivery format', () => {
    expect(run({ formats: ['Live'] }).map((c) => c.format)).toEqual(['Live']);
    expect(run({ formats: ['Pre-recorded', 'Hybrid'] }).length).toBe(3);
  });

  it('sorts by price both ways and by rating', () => {
    const asc = run({ sort: 'price-asc' }).map((c) => c.price);
    expect(asc).toEqual([...asc].sort((a, b) => a - b));

    const desc = run({ sort: 'price-desc' }).map((c) => c.price);
    expect(desc).toEqual([...desc].sort((a, b) => b - a));

    const rating = run({ sort: 'rating' }).map((c) => c.rating);
    expect(rating).toEqual([...rating].sort((a, b) => b - a));
  });

  it('does not mutate the source array', () => {
    const before = COURSES.map((c) => c.id);
    run({ sort: 'price-asc' });
    expect(COURSES.map((c) => c.id)).toEqual(before);
  });
});

describe('course data', () => {
  it('has exactly four published courses covering all three formats', () => {
    expect(COURSES.length).toBe(4);
    expect(COURSES.every((c) => c.published)).toBe(true);
    expect(new Set(COURSES.map((c) => c.format))).toEqual(new Set(['Pre-recorded', 'Live', 'Hybrid']));
  });

  it('has unique slugs and a discount on every course', () => {
    const slugs = new Set(COURSES.map((c) => c.slug));
    expect(slugs.size).toBe(COURSES.length);
    expect(COURSES.every((c) => c.price < c.originalPrice)).toBe(true);
  });

  it('schedules live classes for every Live and Hybrid course', () => {
    for (const course of COURSES) {
      expect(course.schedule.length > 0).toBe(course.format !== 'Pre-recorded');
    }
  });
});

describe('discountPercent', () => {
  it('handles courses with no discount or no price', () => {
    expect(discountPercent({ price: 500, originalPrice: 1000 })).toBe(50);
    expect(discountPercent({ price: 1000, originalPrice: 1000 })).toBe(0);
    expect(discountPercent({ price: 0, originalPrice: 0 })).toBe(0);
  });
});

describe('curriculum rules', () => {
  const modules = structuredClone(COURSES[3].modules);

  it('accepts every demo course', () => {
    for (const c of COURSES) expect(curriculumError(c.modules)).toBeNull();
  });

  it('gives every top-level demo section a video with its notes attached under it', () => {
    for (const s of COURSES[3].modules.flatMap((m) => m.submodules.flatMap((sub) => sub.sections))) {
      expect(s.videos.length).toBeGreaterThan(0);
      expect(s.documents.some((d) => d.type === 'PDF' && d.videoId === s.videos[0].id)).toBe(true);
    }
  });

  it('needs 2 or 3 sections in each submodule', () => {
    const sub = modules[0].submodules[0];
    expect(curriculumError([{ ...modules[0], submodules: [{ ...sub, sections: sub.sections.slice(0, 1) }] }])).toContain('2 or 3');
    const four = [...sub.sections, ...sub.sections].slice(0, 4);
    expect(curriculumError([{ ...modules[0], submodules: [{ ...sub, sections: four }] }])).toContain('2 or 3');
  });

  it('checks document links against their type', () => {
    expect(documentUrlMatchesType('https://x.com/notes.PDF?v=2', 'PDF')).toBe(true);
    expect(documentUrlMatchesType('https://x.com/slides.pptx', 'DOCX')).toBe(false);

    const sub = modules[0].submodules[0];
    const [first, ...rest] = sub.sections;
    const badDoc = { ...first, documents: [{ ...first.documents[0], type: 'DOCX' as const }] };
    expect(curriculumError([{ ...modules[0], submodules: [{ ...sub, sections: [badDoc, ...rest] }] }])).toContain('.pdf');
  });
});

describe('curriculum topics', () => {
  const loops = () => allSections(COURSES[3]).find((s) => s.title === 'Variables, conditions and loops')!;

  it('nests the demo topics under their section, each with its own videos and PDF', () => {
    const [staticVars, dynamicVars] = loops().topics;
    expect(staticVars.title).toBe('Static variables');
    expect(staticVars.videos.length).toBe(2);
    expect(staticVars.documents.map((d) => d.type)).toEqual(['PDF']);
    expect(dynamicVars.title).toBe('Dynamic variables');
    expect(dynamicVars.videos.length).toBe(1);
  });

  it('counts topics as sections, right after their parent', () => {
    const sections = allSections(COURSES[3]);
    const at = sections.indexOf(loops());
    expect(sections.slice(at + 1, at + 3).map((s) => s.title)).toEqual(['Static variables', 'Dynamic variables']);
  });

  it('allows three levels of topics but not four, and needs topic titles', () => {
    const level = (depth: number): Section => ({ ...loops(), topics: depth ? [level(depth - 1)] : [] });
    const withTopics = (section: Section) => {
      const modules = structuredClone(COURSES[3].modules);
      modules[0].submodules[0].sections[1] = section;
      return curriculumError(modules);
    };
    expect(withTopics(level(3))).toBeNull();
    expect(withTopics(level(4))).toContain('3 levels');
    expect(withTopics({ ...loops(), topics: [{ ...loops().topics[0], title: ' ' }] })).toContain('topic needs a title');
  });
});

