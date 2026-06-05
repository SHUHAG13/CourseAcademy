import { Component } from '@angular/core';
import { WhatsappCta } from '../../components/whatsapp-cta/whatsapp-cta';
import { Faq } from '../../components/faq/faq';
import { Testimonials } from '../../components/testimonials/testimonials';
import { Courses } from '../../components/courses/courses';
import { Navbar } from '../../components/navbar/navbar';
import { Footer } from '../../components/footer/footer';
import { PaymentMethods } from '../../components/payment-methods/payment-methods';
import { FeaturedCourse } from '../../components/featured-course/featured-course';
import { Features } from '../../components/features/features';
import { Hero } from '../../components/hero/hero';

@Component({
  selector: 'app-home',
  imports: [Navbar,Hero, Courses, Testimonials, Faq, WhatsappCta,Home,Footer,PaymentMethods,FeaturedCourse,Courses,Features],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {


}
