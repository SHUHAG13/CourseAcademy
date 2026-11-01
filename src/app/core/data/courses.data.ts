import {
  Category,
  Course,
  CurriculumSection,
  Instructor,
  Lesson,
  LiveSession,
  Material,
  MaterialType,
  Review,
} from '../models/course.model';

// Default demo catalogue, copied into browser storage on first run.

export const CATEGORIES: Category[] = [
  {
    id: 'video-editing',
    name: 'Video Editing',
    shortName: 'Video Editing',
    description: 'Edit, colour-grade and publish professional videos for YouTube, brands and broadcast.',
    icon: 'bi-camera-reels',
    image: '/images/web/creative-studio.webp',
    skills: ['Premiere Pro', 'DaVinci Resolve', 'After Effects'],
  },
  {
    id: 'digital-marketing',
    name: 'Digital Marketing',
    shortName: 'Marketing',
    description: 'Plan campaigns, run paid ads and measure the growth they bring to a real business.',
    icon: 'bi-megaphone',
    image: '/images/web/contact-bg.webp',
    skills: ['SEO', 'Meta Ads', 'Analytics'],
  },
  {
    id: 'sports-journalism',
    name: 'Sports Journalism',
    shortName: 'Sports Journalism',
    description: 'Report, write and present sport like a professional — from match reports to live coverage.',
    icon: 'bi-mic',
    image: '/images/web/pitchside-camera.webp',
    skills: ['Match reporting', 'Interviewing', 'Sports video'],
  },
  {
    id: 'software-development',
    name: 'Software Development',
    shortName: 'Development',
    description: 'Build real websites and apps with modern tools used by software teams today.',
    icon: 'bi-code-slash',
    image: '/images/web/dev-team.webp',
    skills: ['TypeScript', 'Angular', 'ASP.NET Core'],
  },
];

export const INSTRUCTORS: Instructor[] = [
  {
    id: 'tanvir-hasan',
    name: 'Tanvir Hasan',
    title: 'Senior Video Editor & Colourist',
    bio: 'Tanvir has edited TV commercials, documentaries and YouTube series for Bangladeshi brands. He focuses on fast, clean workflows that get students job-ready.',
    expertise: ['Premiere Pro', 'DaVinci Resolve', 'Colour grading', 'Documentary editing'],
    experienceYears: 9,
    students: 6200,
    rating: 4.8,
  },
  {
    id: 'nusrat-jahan',
    name: 'Nusrat Jahan',
    title: 'Growth Marketing Strategist',
    bio: 'Nusrat has planned and run digital campaigns for startups and retail brands across South Asia, with a focus on measurable growth over vanity metrics.',
    expertise: ['Paid social', 'SEO', 'Marketing analytics', 'Content strategy'],
    experienceYears: 8,
    students: 4100,
    rating: 4.7,
  },
  {
    id: 'arif-rahman',
    name: 'Arif Rahman',
    title: 'Sports Journalist & Editor',
    bio: 'Arif has covered domestic and international cricket and football for over a decade, working as reporter, desk editor and on-camera presenter.',
    expertise: ['Match reporting', 'Cricket analysis', 'Interviewing', 'News desk editing'],
    experienceYears: 12,
    students: 5300,
    rating: 4.9,
  },
  {
    id: 'sabbir-ahmed',
    name: 'Sabbir Ahmed',
    title: 'Full-Stack Software Engineer',
    bio: 'Sabbir builds web platforms for startups and has mentored junior developers for six years. He teaches by building complete, deployable projects.',
    expertise: ['Angular', 'TypeScript', 'ASP.NET Core', 'SQL Server'],
    experienceYears: 10,
    students: 7800,
    rating: 4.8,
  },
];

let nextId = 1;

function lesson(title: string, minutes: number, preview = false): Lesson {
  return { id: `lesson-${nextId++}`, title, minutes, preview };
}

function section(title: string, lessons: Lesson[]): CurriculumSection {
  return { id: `section-${nextId++}`, title, lessons };
}

function material(title: string, type: MaterialType, file: string): Material {
  return { id: `material-${nextId++}`, title, type, url: `https://example.com/demo-materials/${file}` };
}

/** Sessions are dated from the first run so a fresh demo always has upcoming classes. */
function session(title: string, daysFromNow: number, hour: number, durationMinutes = 90): LiveSession {
  const date = new Date();
  date.setDate(date.getDate() + daysFromNow);
  const pad = (n: number) => String(n).padStart(2, '0');
  return {
    id: `session-${nextId++}`,
    title,
    startsAt: `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(hour)}:00`,
    durationMinutes,
    meetingUrl: `https://meet.example.com/course-academy-${nextId}`,
  };
}

export const COURSES: Course[] = [
  {
    id: 1,
    slug: 'video-editing-post-production',
    title: 'Video Editing & Post-Production',
    subtitle: 'Go from raw footage to client-ready films, ads and YouTube videos with a professional workflow.',
    description:
      'A complete, self-paced path through professional post-production. You will learn to organise footage, build a story in the edit, clean up sound, grade colour and deliver correctly for every platform. Each module ends with a practical project, so you finish with a portfolio you can show to clients and employers.',
    category: 'video-editing',
    format: 'Pre-recorded',
    published: true,
    instructorId: 'tanvir-hasan',
    rating: 4.8,
    ratingCount: 642,
    students: 2140,
    durationHours: 24,
    level: 'Beginner',
    language: 'Bangla',
    price: 4999,
    originalPrice: 8000,
    coverLabel: 'Video Editing',
    coverIcon: 'bi-film',
    badge: 'Bestseller',
    featured: true,
    updated: '2026-10',
    outcomes: [
      'Edit complete videos from raw footage to final export',
      'Use pacing, cuts and B-roll to tell a clear story',
      'Clean up dialogue and mix music and sound effects',
      'Colour correct and grade footage in DaVinci Resolve',
      'Add animated titles and lower thirds in After Effects',
      'Build a three-project portfolio for clients',
    ],
    requirements: [
      'A computer with at least 8 GB RAM',
      'Adobe Premiere Pro (the free trial is fine to start) and DaVinci Resolve (free)',
      'No previous editing experience needed',
    ],
    curriculum: [
      section('Getting started', [
        lesson('Welcome and course roadmap', 6, true),
        lesson('Setting up your workspace', 12, true),
        lesson('Importing and organising footage', 15),
      ]),
      section('Editing fundamentals', [
        lesson('Cuts, trims and the timeline', 22),
        lesson('Pacing and rhythm', 18),
        lesson('B-roll and cutaways', 16),
      ]),
      section('Sound and colour', [
        lesson('Cleaning dialogue', 20),
        lesson('Music and sound design', 18),
        lesson('Reading scopes and balancing shots', 24),
        lesson('Building a cinematic look', 26),
      ]),
      section('Motion and delivery', [
        lesson('Animated titles and lower thirds', 28),
        lesson('Export settings for every platform', 15),
      ]),
      section('Portfolio projects', [
        lesson('Project 1: YouTube video edit', 40),
        lesson('Project 2: 30-second product ad', 35),
        lesson('Project 3: Mini documentary', 45),
      ]),
    ],
    schedule: [],
    materials: [
      material('Editing workflow checklist', 'PDF', 'editing-workflow-checklist.pdf'),
      material('Practice footage pack', 'Link', 'practice-footage'),
      material('Export settings cheat sheet', 'PDF', 'export-settings.pdf'),
    ],
  },
  {
    id: 2,
    slug: 'digital-marketing-growth-strategy',
    title: 'Digital Marketing & Growth Strategy',
    subtitle: 'Plan, launch and measure campaigns in live classes with a working growth strategist.',
    description:
      'A live, cohort-based programme for people who want to grow a real business or start a marketing career. Across eight weeks you will research an audience, build a content and SEO plan, run paid campaigns and report on results. Every class is interactive, and your work gets direct feedback from the instructor.',
    category: 'digital-marketing',
    format: 'Live',
    published: true,
    instructorId: 'nusrat-jahan',
    rating: 4.7,
    ratingCount: 318,
    students: 1280,
    durationHours: 32,
    level: 'Beginner',
    language: 'Bangla',
    price: 5999,
    originalPrice: 9000,
    coverLabel: 'Digital Marketing',
    coverIcon: 'bi-graph-up-arrow',
    badge: 'New',
    featured: true,
    updated: '2026-10',
    outcomes: [
      'Research an audience and define a clear positioning',
      'Build a content calendar and SEO plan',
      'Launch and optimise Meta and Google ad campaigns',
      'Track results with Google Analytics',
      'Calculate customer acquisition cost and return on ad spend',
      'Present a complete growth strategy for a real brand',
    ],
    requirements: [
      'A computer and a stable internet connection for live classes',
      'A Facebook account for ad exercises',
      'No marketing experience needed',
    ],
    curriculum: [
      section('Strategy foundations', [
        lesson('How digital growth really works', 45, true),
        lesson('Audience research and positioning', 60),
      ]),
      section('Organic growth', [
        lesson('Content strategy and calendars', 60),
        lesson('SEO fundamentals', 60),
      ]),
      section('Paid campaigns', [
        lesson('Meta Ads from setup to launch', 90),
        lesson('Google Search Ads', 75),
      ]),
      section('Measure and grow', [
        lesson('Analytics and reporting', 60),
        lesson('Capstone: present your growth strategy', 90),
      ]),
    ],
    schedule: [
      session('Class 1: Strategy foundations', 5, 20),
      session('Class 2: Audience research workshop', 8, 20),
      session('Class 3: Content and SEO planning', 12, 20),
      session('Class 4: Launching your first campaign', 15, 20, 120),
    ],
    materials: [
      material('Marketing strategy template', 'Worksheet', 'marketing-strategy-template'),
      material('Campaign budget planner', 'Worksheet', 'campaign-budget-planner'),
      material('Class 1 slides', 'Slides', 'class-1-slides'),
    ],
  },
  {
    id: 3,
    slug: 'sports-journalism-sports-media',
    title: 'Sports Journalism & Sports Media',
    subtitle: 'Learn reporting, interviewing and digital sports publishing with recorded lessons and live newsroom sessions.',
    description:
      'Learn the craft from a working sports journalist. Recorded lessons teach news writing, match reports, interviews and video scripting at your own pace. Live newsroom sessions then put those skills to work, with real-time match coverage and honest editorial feedback on your stories.',
    category: 'sports-journalism',
    format: 'Hybrid',
    published: true,
    instructorId: 'arif-rahman',
    rating: 4.9,
    ratingCount: 820,
    students: 3120,
    durationHours: 26,
    level: 'Beginner',
    language: 'Bangla',
    price: 3999,
    originalPrice: 7000,
    coverLabel: 'Sports Journalism',
    coverIcon: 'bi-mic',
    badge: 'Popular',
    featured: true,
    updated: '2026-09',
    outcomes: [
      'Write accurate, engaging sports news and match reports',
      'Prepare for and conduct professional interviews',
      'Cover live matches for web and social media',
      'Script and present short video reports',
      'Apply media ethics and fact-checking',
      'Build a published portfolio of sports stories',
    ],
    requirements: [
      'Good written Bangla or English',
      'An interest in cricket, football or other sports',
      'No journalism experience required',
    ],
    curriculum: [
      section('Foundations of sports journalism', [
        lesson('How a sports newsroom works', 12, true),
        lesson('News values in sport', 14, true),
        lesson('Ethics and fact-checking', 16),
      ]),
      section('Writing sport', [
        lesson('Writing the sports news story', 22),
        lesson('The match report', 24),
        lesson('Features and profiles', 20),
      ]),
      section('Interviews and live coverage', [
        lesson('Preparing for an interview', 16),
        lesson('Press conferences and mixed zones', 18),
        lesson('Live blogging a match', 20),
      ]),
      section('Digital and video', [
        lesson('Scripting a video report', 18),
        lesson('Social media coverage', 16),
        lesson('Final project: cover a full match day', 45),
      ]),
    ],
    schedule: [
      session('Live newsroom: pitching and editing stories', 4, 19),
      session('Live coverage practice: match day desk', 11, 18, 120),
      session('Portfolio review and career Q&A', 18, 19, 60),
    ],
    materials: [
      material('Match report template', 'Worksheet', 'match-report-template'),
      material('Interview preparation guide', 'PDF', 'interview-preparation.pdf'),
      material('Sports media style guide', 'PDF', 'sports-media-style-guide.pdf'),
    ],
  },
  {
    id: 4,
    slug: 'software-development-programming',
    title: 'Software Development & Programming',
    subtitle: 'Build and deploy complete web applications, with recorded lessons and live code reviews.',
    description:
      'A practical route from your first line of code to a deployed full-stack application. Recorded lessons cover programming fundamentals, TypeScript, Angular, ASP.NET Core and SQL Server. Weekly live sessions add code reviews and problem solving, so you learn to work the way professional software teams do.',
    category: 'software-development',
    format: 'Hybrid',
    published: true,
    instructorId: 'sabbir-ahmed',
    rating: 4.8,
    ratingCount: 910,
    students: 3560,
    durationHours: 48,
    level: 'Beginner',
    language: 'Bangla',
    price: 7999,
    originalPrice: 14000,
    coverLabel: 'Software Development',
    coverIcon: 'bi-code-slash',
    badge: 'Bestseller',
    featured: true,
    updated: '2026-10',
    outcomes: [
      'Understand programming fundamentals and problem solving',
      'Build responsive pages with HTML, CSS and Bootstrap',
      'Create single-page applications with Angular and TypeScript',
      'Build REST APIs with ASP.NET Core',
      'Design and query a SQL Server database',
      'Use Git and deploy a full-stack project',
    ],
    requirements: [
      'A computer with at least 8 GB RAM and internet access',
      'Basic computer skills — no coding experience needed',
    ],
    curriculum: [
      section('Programming fundamentals', [
        lesson('How software and the web work', 14, true),
        lesson('Variables, conditions and loops', 40, true),
        lesson('Functions, arrays and objects', 50),
        lesson('Git and GitHub basics', 30),
      ]),
      section('Front end', [
        lesson('HTML, CSS and responsive layout', 60),
        lesson('TypeScript essentials', 45),
        lesson('Angular components and routing', 60),
        lesson('Signals, services and forms', 60),
      ]),
      section('Back end', [
        lesson('Building APIs with ASP.NET Core', 75),
        lesson('SQL Server and Entity Framework', 60),
        lesson('Authentication overview', 40),
      ]),
      section('Capstone', [
        lesson('Connecting front end and API', 50),
        lesson('Capstone: deploy your project', 75),
      ]),
    ],
    schedule: [
      session('Live code review: your first program', 6, 21, 60),
      session('Live workshop: debugging together', 13, 21, 60),
      session('Live code review: Angular project', 20, 21, 60),
      session('Capstone demo day', 27, 20, 120),
    ],
    materials: [
      material('Developer setup guide', 'PDF', 'developer-setup-guide.pdf'),
      material('Starter project repository', 'Link', 'starter-project'),
      material('TypeScript quick reference', 'PDF', 'typescript-quick-reference.pdf'),
    ],
  },
];

/** Demo review pool, shown on course pages. */
export const REVIEWS: Review[] = [
  {
    name: 'Rakibul Islam',
    rating: 5,
    date: '2026-09-14',
    text: 'Very practical. Every module ends with something you actually build, so I finished with work I could show clients.',
  },
  {
    name: 'Sanjida Akter',
    rating: 5,
    date: '2026-08-30',
    text: 'Clear explanations in Bangla and the instructor answered questions in the support group quickly.',
  },
  {
    name: 'Tanvir Ahmed',
    rating: 4,
    date: '2026-08-02',
    text: 'Good structure and pace. I would like a few more advanced examples, but for the price it is excellent value.',
  },
  {
    name: 'Mehedi Hasan',
    rating: 5,
    date: '2026-07-21',
    text: 'The projects helped me land my first freelance work. Worth it.',
  },
  {
    name: 'Fatema Khatun',
    rating: 4,
    date: '2026-07-05',
    text: 'Lessons are short and focused, which made it easy to learn alongside university.',
  },
];
