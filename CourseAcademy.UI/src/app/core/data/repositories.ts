import { InjectionToken } from '@angular/core';
import { AppUser } from '../models/user.model';
import { Certificate, CertificationRule, ExamSubmission } from '../models/certificate.model';
import { Comment, Post } from '../models/community.model';
import { Course, Instructor } from '../models/course.model';
import { Enrollment } from '../models/enrollment.model';
import { CourseGrant, Role } from '../models/permission.model';
import { Progress } from '../models/progress.model';
import { PaymentTransaction } from '../payments/payment.model';
import { DEFAULT_ROLES, DEMO_COURSE_GRANTS } from './access.data';
import { DEMO_CERTIFICATES, DEMO_CERTIFICATION_RULES, DEMO_SUBMISSIONS } from './certificate.data';
import { DEMO_COMMENTS, DEMO_POSTS } from './community.data';
import { COURSES, INSTRUCTORS } from './courses.data';
import { DEMO_ENROLLMENTS, DEMO_PROGRESS, DEMO_TRANSACTIONS, DEMO_USERS } from './demo-records.data';
import { LocalStorageRepository, Repository } from './repository';

// To connect the ASP.NET Core API, provide HTTP repositories for these tokens in app.config.ts.

export const COURSE_REPOSITORY = new InjectionToken<Repository<Course>>('CourseRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-courses', () => COURSES),
});

export const INSTRUCTOR_REPOSITORY = new InjectionToken<Repository<Instructor>>('InstructorRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-instructors', () => INSTRUCTORS),
});

export const USER_REPOSITORY = new InjectionToken<Repository<AppUser>>('UserRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-users', () => DEMO_USERS),
});

export const ENROLLMENT_REPOSITORY = new InjectionToken<Repository<Enrollment>>('EnrollmentRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-enrollments', () => DEMO_ENROLLMENTS),
});

export const TRANSACTION_REPOSITORY = new InjectionToken<Repository<PaymentTransaction>>('TransactionRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-transactions', () => DEMO_TRANSACTIONS),
});

export const ROLE_REPOSITORY = new InjectionToken<Repository<Role>>('RoleRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-roles', () => DEFAULT_ROLES),
});

export const COURSE_GRANT_REPOSITORY = new InjectionToken<Repository<CourseGrant>>('CourseGrantRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-course-grants', () => DEMO_COURSE_GRANTS),
});

export const PROGRESS_REPOSITORY = new InjectionToken<Repository<Progress>>('ProgressRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-progress', () => DEMO_PROGRESS),
});

export const POST_REPOSITORY = new InjectionToken<Repository<Post>>('PostRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-posts', () => DEMO_POSTS),
});

export const COMMENT_REPOSITORY = new InjectionToken<Repository<Comment>>('CommentRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-comments', () => DEMO_COMMENTS),
});

export const CERTIFICATION_RULE_REPOSITORY = new InjectionToken<Repository<CertificationRule>>('CertificationRuleRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-certification-rules', () => DEMO_CERTIFICATION_RULES),
});

export const SUBMISSION_REPOSITORY = new InjectionToken<Repository<ExamSubmission>>('SubmissionRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-exam-submissions', () => DEMO_SUBMISSIONS),
});

export const CERTIFICATE_REPOSITORY = new InjectionToken<Repository<Certificate>>('CertificateRepository', {
  providedIn: 'root',
  factory: () => new LocalStorageRepository('ca-certificates', () => DEMO_CERTIFICATES),
});
