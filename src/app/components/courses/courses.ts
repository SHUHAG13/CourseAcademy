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
    title: 'Web Development',
    price: 3999,
    icon:'💻'
  },
  {
    title: 'Graphic Design',
    price: 2999,
    icon:'🎨'
  },
  {
    title: 'Digital Marketing',
    price: 2499,
    icon:'📈'
  },
  {
    title: 'Freelancing Master',
    price: 1999,
    icon:'🚀'
  }
];

}
