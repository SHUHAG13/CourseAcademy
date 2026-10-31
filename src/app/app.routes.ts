import { Routes } from '@angular/router';
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
