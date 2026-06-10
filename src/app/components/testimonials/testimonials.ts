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
      name: 'Rakibul Islam',
      profession: 'Freelance Developer',
      image: 'images/sj1.jpg',
      rating: 5,
      review:
        'This course completely changed my career path. The practical projects and real-world guidance were extremely helpful.'
    },
    {
      name: 'Sanjida Akter',
      profession: 'Computer Science Student',
      image: 'images/sj2.jpg',
      rating: 5,
      review:
        'Every topic was explained in a very clear and structured way. I gained a lot of confidence in web development.'
    },
    {
      name: 'Tanvir Ahmed',
      profession: 'Junior Software Engineer',
      image: 'images/sj3.jpg',
      rating: 5,
      review:
        'The live sessions and community support made learning much easier and more interactive.'
    }
  ];

  stars = [1, 2, 3, 4, 5];
}