export type CertificationMode = 'completion' | 'exam';

/** The central exam is assessed work handed in at the end, not a quiz. */
export type ExamType = 'project' | 'presentation';

export const EXAM_TYPES: ExamType[] = ['project', 'presentation'];

/** How a course awards its certificate. One per course. */
export interface CertificationRule {
  /** The course id, so each course has one rule. */
  id: number;
  enabled: boolean;
  /** `completion`: certified when the progress requirement is met. `exam`: also needs a passed central exam. */
  mode: CertificationMode;
  /** Percentage of the course that must be completed, 1–100: sections in demo mode, lessons in API mode. */
  minProgress: number;
  examType: ExamType;
  examTitle: string;
  examInstructions: string;
  maxScore: number;
  passingScore: number;
  /** 0 means unlimited. */
  maxAttempts: number;
}

export type SubmissionStatus = 'submitted' | 'passed' | 'failed';

/** A student's central exam work: a link to the project or presentation. */
export interface ExamSubmission {
  id: string;
  userId: string;
  courseId: number;
  examType: ExamType;
  /** Repository, slides or video link. */
  url: string;
  notes: string;
  submittedAt: string;
  status: SubmissionStatus;
  score?: number;
  feedback?: string;
  reviewedBy?: string;
  reviewedAt?: string;
  // API mode: sent by the server, so the admin list needs no user or course lookups.
  userName?: string;
  courseTitle?: string;
  /** The course's current exam settings. */
  exam?: SubmissionExam;
}

/** The exam settings a submission is reviewed against. */
export interface SubmissionExam {
  title: string;
  maxScore: number;
  passingScore: number;
}

/** An issued certificate. Names are copied in, so later edits don't change a certificate. */
export interface Certificate {
  id: string;
  /** Short public code used to verify the certificate, e.g. CA-2026-K7Q2XM. */
  code: string;
  userId: string;
  courseId: number;
  userName: string;
  courseTitle: string;
  instructorName: string;
  mode: CertificationMode;
  examType?: ExamType;
  /** Exam score as a percentage, for exam certificates. */
  scorePercent?: number;
  issuedAt: string;
  revoked: boolean;
  revokedReason?: string;
}

/** The rule a course uses until someone configures one. */
export function defaultRule(courseId: number): CertificationRule {
  return {
    id: courseId,
    enabled: true,
    mode: 'completion',
    minProgress: 100,
    examType: 'project',
    examTitle: 'Final project',
    examInstructions: '',
    maxScore: 100,
    passingScore: 60,
    maxAttempts: 0,
  };
}
