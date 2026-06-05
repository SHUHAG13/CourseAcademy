import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-faq',
  imports: [CommonModule],
  templateUrl: './faq.html',
  styleUrl: './faq.scss',
})
export class Faq {
  faqs = [
  {
    question:'কোর্সটি কার জন্য?',
    answer:'Beginner দের জন্যও উপযুক্ত'
  },
  {
    question:'সার্টিফিকেট পাবো?',
    answer:'হ্যাঁ'
  }
];

}
