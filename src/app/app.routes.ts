import { Routes } from '@angular/router';
import { authGuard, permissionGuard, screenGuard, sectionGuard } from './core/guards/auth.guards';
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
    canActivate: [authGuard, screenGuard('dashboard')],
    loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    // Title is set by the component once the course is loaded.
    path: 'learn/:slug',
    canActivate: [authGuard, screenGuard('learn')],
    loadComponent: () => import('./pages/learn/learn').then((m) => m.Learn),
  },
  {
    path: 'checkout/pay/:id',
    title: `Demo Payment | ${SITE}`,
    canActivate: [authGuard, screenGuard('checkout')],
    loadComponent: () => import('./pages/checkout/demo-gateway').then((m) => m.DemoGateway),
  },
  {
    path: 'checkout/result/:id',
    title: `Payment Status | ${SITE}`,
    canActivate: [authGuard, screenGuard('checkout')],
    loadComponent: () => import('./pages/checkout/payment-result').then((m) => m.PaymentResult),
  },
  {
    path: 'checkout/:slug',
    title: `Checkout | ${SITE}`,
    canActivate: [authGuard, screenGuard('checkout')],
    loadComponent: () => import('./pages/checkout/checkout').then((m) => m.Checkout),
  },
  {
    path: 'admin',
    canActivate: [permissionGuard('admin.access')],
    loadComponent: () => import('./pages/admin/admin-layout').then((m) => m.AdminLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: `Admin Overview | ${SITE}`,
        canActivate: [screenGuard('overview')],
        loadComponent: () => import('./pages/admin/admin-overview').then((m) => m.AdminOverview),
      },
      {
        path: 'courses',
        title: `Manage Courses | ${SITE}`,
        canActivate: [screenGuard('courses')],
        loadComponent: () => import('./pages/admin/admin-courses').then((m) => m.AdminCourses),
      },
      {
        path: 'courses/new',
        title: `New Course | ${SITE}`,
        canActivate: [permissionGuard('courses.create')],
        loadComponent: () => import('./pages/admin/course-form/course-form').then((m) => m.CourseForm),
      },
      {
        path: 'courses/:id/curriculum',
        title: `Edit Curriculum | ${SITE}`,
        canActivate: [permissionGuard('courses.edit', { courseParam: 'id' })],
        loadComponent: () =>
          import('./pages/admin/curriculum-editor/curriculum-editor').then((m) => m.CurriculumEditor),
      },
      {
        path: 'courses/:id/certification',
        title: `Certification | ${SITE}`,
        canActivate: [permissionGuard('certificates.manage', { courseParam: 'id' })],
        loadComponent: () => import('./pages/admin/certification-settings').then((m) => m.CertificationSettings),
      },
      {
        path: 'courses/:id',
        title: `Edit Course | ${SITE}`,
        canActivate: [permissionGuard('courses.edit', { courseParam: 'id' })],
        loadComponent: () => import('./pages/admin/course-form/course-form').then((m) => m.CourseForm),
      },
      {
        path: 'instructors',
        title: `Manage Instructors | ${SITE}`,
        canActivate: [screenGuard('instructors')],
        loadComponent: () => import('./pages/admin/admin-instructors').then((m) => m.AdminInstructors),
      },
      {
        path: 'users',
        title: `Users | ${SITE}`,
        canActivate: [screenGuard('users')],
        loadComponent: () => import('./pages/admin/admin-users').then((m) => m.AdminUsers),
      },
      {
        path: 'enrollments',
        title: `Enrollments | ${SITE}`,
        canActivate: [screenGuard('enrollments')],
        loadComponent: () => import('./pages/admin/admin-enrollments').then((m) => m.AdminEnrollments),
      },
      {
        path: 'transactions',
        title: `Transactions | ${SITE}`,
        canActivate: [screenGuard('transactions')],
        loadComponent: () => import('./pages/admin/admin-transactions').then((m) => m.AdminTransactions),
      },
      {
        path: 'certificates',
        title: `Certificates | ${SITE}`,
        canActivate: [screenGuard('certificates')],
        loadComponent: () => import('./pages/admin/admin-certificates').then((m) => m.AdminCertificates),
      },
      {
        path: 'community',
        title: `Community | ${SITE}`,
        canActivate: [screenGuard('community-moderation')],
        loadComponent: () => import('./pages/admin/admin-community').then((m) => m.AdminCommunity),
      },
      {
        path: 'security',
        loadComponent: () => import('./pages/admin/security/security-layout').then((m) => m.SecurityLayout),
        children: [
          { path: '', pathMatch: 'full', canActivate: [sectionGuard('security')], children: [] },
          {
            path: 'roles',
            title: `Roles & Permissions | ${SITE}`,
            canActivate: [screenGuard('roles')],
            loadComponent: () => import('./pages/admin/security/admin-roles').then((m) => m.AdminRoles),
          },
          {
            path: 'access',
            title: `User Access | ${SITE}`,
            canActivate: [screenGuard('access')],
            loadComponent: () => import('./pages/admin/security/admin-access').then((m) => m.AdminAccess),
          },
          {
            path: 'access/:userId',
            title: `User Access | ${SITE}`,
            canActivate: [screenGuard('access')],
            loadComponent: () => import('./pages/admin/security/user-access').then((m) => m.UserAccess),
          },
        ],
      },
    ],
  },
  {
    path: 'verify',
    title: `Verify a Certificate | ${SITE}`,
    loadComponent: () => import('./pages/verify/verify').then((m) => m.Verify),
  },
  {
    path: 'verify/:code',
    title: `Verify a Certificate | ${SITE}`,
    loadComponent: () => import('./pages/verify/verify').then((m) => m.Verify),
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
