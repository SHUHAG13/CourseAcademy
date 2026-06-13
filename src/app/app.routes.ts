import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { Courses } from './components/courses/courses';
import { PaymentMethods } from './components/payment-methods/payment-methods';
import { WhatsappCta } from './components/whatsapp-cta/whatsapp-cta';
import { Testimonials } from './components/testimonials/testimonials';
import { Faq } from './components/faq/faq';
import { NotFound } from './components/not-found/not-found';
import { Contact } from './components/contact/contact/contact';
import { About } from './components/about/about/about';

export const routes: Routes = [

  {
    path: '',
    component: Home
  },

  {
    path: 'courses',
    component: Courses
  },

  {
    path: 'payment',
    component: PaymentMethods
  },

  {
    path: 'contact',
    component: Contact
  },
  {
     path: 'about',
    component: About
  },

  {
    path: 'testimonials',
    component: Testimonials
  },

  {
    path: 'faq',
    component: Faq
  },

  {
    path: '**',
    component: NotFound
  }

 
];
