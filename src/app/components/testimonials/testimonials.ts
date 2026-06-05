import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
interface Testimonial {
  name: string;
  profession: string;
  image: string;
  rating: number;
  review: string;
}
@Component({
  selector: 'app-testimonials',
  imports: [CommonModule],
  templateUrl: './testimonials.html',
  styleUrl: './testimonials.scss',
})
export class Testimonials {
testimonials: Testimonial[] = [
    {
      name: 'রাকিবুল ইসলাম',
      profession: 'Freelancer',
      image: 'assets/images/student-1.jpg',
      rating: 5,
      review:
        'কোর্সটি আমার ক্যারিয়ারে অনেক সাহায্য করেছে। প্র্যাকটিক্যাল প্রজেক্ট এবং লাইভ সাপোর্ট ছিল অসাধারণ।'
    },
    {
      name: 'সানজিদা আক্তার',
      profession: 'Student',
      image: 'assets/images/student-2.jpg',
      rating: 5,
      review:
        'শুরু থেকে শেষ পর্যন্ত প্রতিটি বিষয় খুব সুন্দরভাবে বুঝানো হয়েছে।'
    },
    {
      name: 'তানভীর আহমেদ',
      profession: 'Junior Developer',
      image: 'assets/images/student-3.jpg',
      rating: 5,
      review:
        'লাইভ ক্লাস এবং কমিউনিটি সাপোর্ট আমার সবচেয়ে ভালো লেগেছে।'
    }
  ];

  stars = [1, 2, 3, 4, 5];
}
