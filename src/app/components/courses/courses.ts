import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-courses',
  imports: [CommonModule],
  templateUrl: './courses.html',
  styleUrl: './courses.scss',
})
export class Courses {
courses = [
  {
    title: 'Sports Journalism Masterclass',
    category: 'Journalism',
    duration: '8 Weeks',
    level: 'Beginner',
    price: 3999,
    image: '/images/sj1.jpg',
    description: 'Sports reporting, match analysis, interviewing and digital journalism.'
  },
  {
    title: 'Sports Content Writing',
    category: 'Content Writing',
    duration: '6 Weeks',
    level: 'Beginner',
    price: 2499,
    image: '/images/sj2.jpg',
    description: 'Learn article writing, match reports and social media content creation.'
  },
  {
    title: 'Sports Video Production',
    category: 'Media Production',
    duration: '8 Weeks',
    level: 'Intermediate',
    price: 4499,
    image: '/images/sj3.jpg',
    description: 'Create professional sports highlights, reels and video reports.'
  },
  {
    title: 'Digital Sports Media',
    category: 'Digital Media',
    duration: '5 Weeks',
    level: 'Beginner',
    price: 2999,
    image: '/images/sj1.jpg',
    description: 'Learn sports publishing, audience growth and media branding.'
  }
];

}
