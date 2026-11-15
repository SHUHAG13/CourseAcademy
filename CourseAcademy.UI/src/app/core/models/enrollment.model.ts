export type EnrollmentStatus = 'active' | 'revoked';

export interface Enrollment {
  id: string;
  userId: string;
  courseId: number;
  status: EnrollmentStatus;
  enrolledAt: string;
  /** The payment that granted access. Absent for free courses. */
  transactionId?: string;
}
