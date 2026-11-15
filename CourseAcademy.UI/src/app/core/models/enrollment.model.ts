export type EnrollmentStatus = 'active' | 'revoked';

export interface Enrollment {
  id: string;
  userId: string;
  courseId: number;
  status: EnrollmentStatus;
  enrolledAt: string;
  /** The payment that granted access. Absent for free courses. */
  transactionId?: string;
  /** Filled by the API, so lists can name the student even when the user list is not loaded. */
  studentName?: string;
  studentEmail?: string;
}
