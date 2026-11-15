import { Component } from '@angular/core';
import { Hero } from '../../components/hero/hero';
import { Categories } from '../../components/categories/categories';
import { Courses } from '../../components/courses/courses';
import { Features } from '../../components/features/features';
import { LearningJourney } from '../../components/learning-journey/learning-journey';
import { SportsTeaser } from '../../components/sports-teaser/sports-teaser';
import { CareerTeaser } from '../../components/career-teaser/career-teaser';
import { Testimonials } from '../../components/testimonials/testimonials';
import { Faq } from '../../components/faq/faq';
import { FinalCta } from '../../components/final-cta/final-cta';

@Component({
  selector: 'app-home',
  imports: [
    Hero,
    Categories,
    Courses,
    Features,
    LearningJourney,
    SportsTeaser,
    CareerTeaser,
    Testimonials,
    Faq,
    FinalCta,
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {}
