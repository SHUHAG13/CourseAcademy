import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guards/auth.guards';
import { Home } from './pages/home/home';

const SITE = 'Course Academy';

export const routes: Routes = [
  { path: '', component: Home, title: `${SITE} — Build Real Skills. Create Your Future.` },

  {
    path: 'courses',
    title: `All Courses | ${SITE}`,
    loadComponent: () => import('./pages/course-list/course-list').then((m) => m.CourseList),
  },
  {
    // Title is set by the component once the course is loaded.
    path: 'courses/:slug',
    loadComponent: () => import('./pages/course-detail/course-detail').then((m) => m.CourseDetail),
  },
  {
    path: 'payment',
    title: `How to Enroll & Pay | ${SITE}`,
    loadComponent: () => import('./components/payment-methods/payment-methods').then((m) => m.PaymentMethods),
  },
  {
    path: 'sports',
    title: `Sports Media | ${SITE}`,
    loadComponent: () => import('./pages/sports/sports').then((m) => m.Sports),
  },
  {
    path: 'career',
    title: `Career Opportunities | ${SITE}`,
    loadComponent: () => import('./pages/career/career').then((m) => m.Career),
  },
  {
    path: 'about',
    title: `About Us | ${SITE}`,
    loadComponent: () => import('./pages/about/about').then((m) => m.About),
  },
  {
    path: 'contact',
    title: `Contact | ${SITE}`,
    loadComponent: () => import('./pages/contact/contact').then((m) => m.Contact),
  },
  {
    path: 'login',
    title: `Login | ${SITE}`,
    loadComponent: () => import('./pages/auth/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: `Sign Up | ${SITE}`,
    loadComponent: () => import('./pages/auth/register').then((m) => m.Register),
  },
  {
    path: 'dashboard',
    title: `My Dashboard | ${SITE}`,
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    // Title is set by the component once the course is loaded.
    path: 'learn/:slug',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/learn/learn').then((m) => m.Learn),
  },
  {
    path: 'checkout/pay/:id',
    title: `Demo Payment | ${SITE}`,
    canActivate: [authGuard],
    loadComponent: () => import('./pages/checkout/demo-gateway').then((m) => m.DemoGateway),
  },
  {
    path: 'checkout/result/:id',
    title: `Payment Status | ${SITE}`,
    canActivate: [authGuard],
    loadComponent: () => import('./pages/checkout/payment-result').then((m) => m.PaymentResult),
  },
  {
    path: 'checkout/:slug',
    title: `Checkout | ${SITE}`,
    canActivate: [authGuard],
    loadComponent: () => import('./pages/checkout/checkout').then((m) => m.Checkout),
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./pages/admin/admin-layout').then((m) => m.AdminLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: `Admin Overview | ${SITE}`,
        loadComponent: () => import('./pages/admin/admin-overview').then((m) => m.AdminOverview),
      },
      {
        path: 'courses',
        title: `Manage Courses | ${SITE}`,
        loadComponent: () => import('./pages/admin/admin-courses').then((m) => m.AdminCourses),
      },
      {
        path: 'courses/new',
        title: `New Course | ${SITE}`,
        loadComponent: () => import('./pages/admin/course-form/course-form').then((m) => m.CourseForm),
      },
      {
        path: 'courses/:id',
        title: `Edit Course | ${SITE}`,
        loadComponent: () => import('./pages/admin/course-form/course-form').then((m) => m.CourseForm),
      },
      {
        path: 'instructors',
        title: `Manage Instructors | ${SITE}`,
        loadComponent: () => import('./pages/admin/admin-instructors').then((m) => m.AdminInstructors),
      },
      {
        path: 'users',
        title: `Users | ${SITE}`,
        loadComponent: () => import('./pages/admin/admin-users').then((m) => m.AdminUsers),
      },
      {
        path: 'enrollments',
        title: `Enrollments | ${SITE}`,
        loadComponent: () => import('./pages/admin/admin-enrollments').then((m) => m.AdminEnrollments),
      },
      {
        path: 'transactions',
        title: `Transactions | ${SITE}`,
        loadComponent: () => import('./pages/admin/admin-transactions').then((m) => m.AdminTransactions),
      },
    ],
  },
  {
    path: 'testimonials',
    title: `Student Stories | ${SITE}`,
    data: { asPage: true },
    loadComponent: () => import('./components/testimonials/testimonials').then((m) => m.Testimonials),
  },
  {
    path: 'faq',
    title: `FAQ | ${SITE}`,
    data: { asPage: true },
    loadComponent: () => import('./components/faq/faq').then((m) => m.Faq),
  },
  {
    path: 'privacy',
    title: `Privacy Policy | ${SITE}`,
    data: { page: 'privacy' },
    loadComponent: () => import('./pages/legal/legal').then((m) => m.Legal),
  },
  {
    path: 'terms',
    title: `Terms & Conditions | ${SITE}`,
    data: { page: 'terms' },
    loadComponent: () => import('./pages/legal/legal').then((m) => m.Legal),
  },
  {
    path: '**',
    title: `Page not found | ${SITE}`,
    loadComponent: () => import('./pages/not-found/not-found').then((m) => m.NotFound),
  },
];
