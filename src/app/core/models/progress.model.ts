/** One user's progress through one course. */
export interface Progress {
  /** `userId:courseId`, so each user has one record per course. */
  id: string;
  userId: string;
  courseId: number;
  completedSectionIds: string[];
  updatedAt: string;
}

export function progressId(userId: string, courseId: number): string {
  return `${userId}:${courseId}`;
}
