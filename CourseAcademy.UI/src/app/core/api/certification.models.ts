import { Certificate, CertificationMode, CertificationRule, ExamSubmission, ExamType, SubmissionStatus } from '../models/certificate.model';

// TypeScript shapes of the API's certification DTOs, and their mapping to the app's models.

export interface RuleDto {
  courseId: number;
  isEnabled: boolean;
  modeCode: CertificationMode;
  minProgress: number;
  examTypeCode: ExamType;
  examTitle: string;
  examInstructions: string;
  maxScore: number;
  passingScore: number;
  maxAttempts: number;
}

export interface CertificateDto {
  id: number;
  code: string;
  userId: number;
  courseId: number;
  userName: string;
  courseTitle: string;
  instructorName: string;
  modeCode: CertificationMode;
  examTypeCode: ExamType | null;
  scorePercent: number | null;
  issuedAt: string;
  isRevoked: boolean;
  revokedReason: string | null;
}

export interface SubmissionDto {
  id: number;
  userId: number;
  userName: string;
  courseId: number;
  courseTitle: string;
  examTypeCode: ExamType;
  examTitle: string;
  maxScore: number;
  passingScore: number;
  url: string;
  notes: string;
  submittedAt: string;
  statusCode: SubmissionStatus;
  score: number | null;
  feedback: string | null;
  reviewedBy: { id: number; name: string; email: string } | null;
  reviewedAt: string | null;
}

export interface CertificationStatusDto {
  rule: RuleDto;
  progress: number;
  progressMet: boolean;
  submissions: SubmissionDto[];
  attemptsLeft: number | null;
  canSubmit: boolean;
  certificate: CertificateDto | null;
  revokedCertificate: CertificateDto | null;
}

export interface ReviewResultDto {
  submission: SubmissionDto;
  certificate: CertificateDto | null;
}

export interface VerifyCertificateDto {
  code: string;
  isRevoked: boolean;
  revokedReason: string | null;
  userName: string;
  courseTitle: string;
  modeCode: CertificationMode;
  examTypeCode: ExamType | null;
  scorePercent: number | null;
  instructorName: string;
  issuedAt: string;
}

export function fromRuleDto(dto: RuleDto): CertificationRule {
  return {
    id: dto.courseId,
    enabled: dto.isEnabled,
    mode: dto.modeCode,
    minProgress: dto.minProgress,
    examType: dto.examTypeCode,
    examTitle: dto.examTitle,
    examInstructions: dto.examInstructions,
    maxScore: dto.maxScore,
    passingScore: dto.passingScore,
    maxAttempts: dto.maxAttempts,
  };
}

export function toRuleRequest(rule: CertificationRule) {
  return {
    isEnabled: rule.enabled,
    modeCode: rule.mode,
    minProgress: rule.minProgress,
    examTypeCode: rule.examType,
    examTitle: rule.examTitle,
    examInstructions: rule.examInstructions,
    maxScore: rule.maxScore,
    passingScore: rule.passingScore,
    maxAttempts: rule.maxAttempts,
  };
}

export function fromCertificateDto(dto: CertificateDto): Certificate {
  return {
    id: String(dto.id),
    code: dto.code,
    userId: String(dto.userId),
    courseId: dto.courseId,
    userName: dto.userName,
    courseTitle: dto.courseTitle,
    instructorName: dto.instructorName,
    mode: dto.modeCode,
    examType: dto.examTypeCode ?? undefined,
    scorePercent: dto.scorePercent ?? undefined,
    issuedAt: dto.issuedAt,
    revoked: dto.isRevoked,
    revokedReason: dto.revokedReason ?? undefined,
  };
}

/** The public verification result, as a certificate without ids. */
export function fromVerifyDto(dto: VerifyCertificateDto): Certificate {
  return {
    id: dto.code,
    code: dto.code,
    userId: '',
    courseId: 0,
    userName: dto.userName,
    courseTitle: dto.courseTitle,
    instructorName: dto.instructorName,
    mode: dto.modeCode,
    examType: dto.examTypeCode ?? undefined,
    scorePercent: dto.scorePercent ?? undefined,
    issuedAt: dto.issuedAt,
    revoked: dto.isRevoked,
    revokedReason: dto.revokedReason ?? undefined,
  };
}

export function fromSubmissionDto(dto: SubmissionDto): ExamSubmission {
  return {
    id: String(dto.id),
    userId: String(dto.userId),
    courseId: dto.courseId,
    examType: dto.examTypeCode,
    url: dto.url,
    notes: dto.notes,
    submittedAt: dto.submittedAt,
    status: dto.statusCode,
    score: dto.score ?? undefined,
    feedback: dto.feedback ?? undefined,
    reviewedBy: dto.reviewedBy ? String(dto.reviewedBy.id) : undefined,
    reviewedAt: dto.reviewedAt ?? undefined,
    userName: dto.userName,
    courseTitle: dto.courseTitle,
    exam: { title: dto.examTitle, maxScore: dto.maxScore, passingScore: dto.passingScore },
  };
}
