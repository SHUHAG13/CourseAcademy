import { PaymentStatus } from '../payments/payment.model';

// TypeScript shapes of the API's progress and overview DTOs.
// A lesson is a video, or a section that has no videos (BR-LRN-15).

export interface VideoProgressDto {
  videoId: number;
  /** Where to resume. Only saved for direct video files and uploads. */
  positionSeconds: number;
  durationSeconds: number | null;
  isCompleted: boolean;
}

export interface CourseProgressDto {
  courseId: number;
  completedLessons: number;
  totalLessons: number;
  percent: number;
  /** Sections whose lessons are all done. */
  completedSectionIds: number[];
  videos: VideoProgressDto[];
}

export interface CourseProgressSummaryDto {
  courseId: number;
  completedLessons: number;
  totalLessons: number;
  percent: number;
}

export interface RecentTransactionDto {
  transactionNo: string;
  createdAt: string;
  userName: string;
  courseTitle: string;
  amount: number;
  statusCode: PaymentStatus;
}

export interface AdminOverviewDto {
  courseCount: number;
  publishedCourseCount: number;
  studentCount: number;
  activeEnrollmentCount: number;
  collectedAmount: number;
  recentTransactions: RecentTransactionDto[];
}
