import { Certificate, CertificationRule, ExamSubmission } from '../models/certificate.model';

// Sample certification settings and records, copied into browser storage on first run.

const DAY_MS = 86_400_000;
const daysAgo = (days: number) => new Date(Date.now() - days * DAY_MS).toISOString();

export const DEMO_CERTIFICATION_RULES: CertificationRule[] = [
  {
    id: 1,
    enabled: true,
    mode: 'exam',
    minProgress: 100,
    examType: 'project',
    examTitle: 'Showreel project',
    examInstructions: 'Edit a 60–90 second showreel from your course projects. Upload it to YouTube or Google Drive and share the link.',
    maxScore: 100,
    passingScore: 60,
    maxAttempts: 2,
  },
  {
    id: 2,
    enabled: true,
    mode: 'exam',
    minProgress: 100,
    examType: 'presentation',
    examTitle: 'Growth strategy presentation',
    examInstructions: 'Prepare a 10-slide growth strategy for a real brand and record yourself presenting it. Share the slides and the recording.',
    maxScore: 100,
    passingScore: 70,
    maxAttempts: 3,
  },
  {
    id: 3,
    enabled: true,
    mode: 'completion',
    minProgress: 100,
    examType: 'project',
    examTitle: 'Final project',
    examInstructions: '',
    maxScore: 100,
    passingScore: 60,
    maxAttempts: 0,
  },
  {
    id: 4,
    enabled: true,
    mode: 'exam',
    minProgress: 100,
    examType: 'project',
    examTitle: 'Capstone project',
    examInstructions:
      'Build and deploy a full-stack web app with an Angular front end and an ASP.NET Core API. Share the GitHub repository and the live link in your notes.',
    maxScore: 100,
    passingScore: 60,
    maxAttempts: 2,
  },
];

/** The sample learner is waiting for a review of their capstone. */
export const DEMO_SUBMISSIONS: ExamSubmission[] = [
  {
    id: 'submission-1',
    userId: 'user-sample',
    courseId: 4,
    examType: 'project',
    url: 'https://github.com/example/sample-learner-capstone',
    notes: 'Live app: https://sample-learner-shop.example.com',
    submittedAt: daysAgo(1),
    status: 'submitted',
  },
];

/** The sample learner finished the sports course. */
export const DEMO_CERTIFICATES: Certificate[] = [
  {
    id: 'certificate-1',
    code: 'CA-2026-K7Q2XM',
    userId: 'user-sample',
    courseId: 3,
    userName: 'Sample Learner',
    courseTitle: 'Sports Journalism & Sports Media',
    instructorName: 'Arif Rahman',
    mode: 'completion',
    issuedAt: daysAgo(2),
    revoked: false,
  },
];
