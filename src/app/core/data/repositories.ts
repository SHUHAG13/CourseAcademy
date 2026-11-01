import { InjectionToken } from '@angular/core';
import { AppUser } from '../models/user.model';
import { Course, Instructor } from '../models/course.model';
import { Enrollment } from '../models/enrollment.model';
import { PaymentTransaction } from '../payments/payment.model';
import { COURSES, INSTRUCTORS } from './courses.data';
import { DEMO_ENROLLMENTS, DEMO_TRANSACTIONS, DEMO_USERS } from './demo-records.data';
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
