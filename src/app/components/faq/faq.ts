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
      question: 'Is this course fully online?',
      answer: 'Yes, the course is completely online. You can access it anytime and from anywhere.'
    },
    {
      question: 'How long will I have access to the course?',
      answer: 'You will get access for a fixed duration or lifetime access depending on the course policy.'
    },
    {
      question: 'Will I receive a certificate after completion?',
      answer: 'Yes, you will receive a digital certificate after successfully completing the course.'
    },
    {
      question: 'Can I take the course using a mobile phone?',
      answer: 'Yes, the course is fully compatible with mobile phones, tablets, and computers.'
    },
    {
      question: 'Will I get support during the course?',
      answer: 'Yes, you will get access to a dedicated support group and instructor assistance.'
    },
    {
      question: 'What should I do after making the payment?',
      answer: 'After payment, you need to send the transaction screenshot. Access will be granted after verification.'
    }
  ];

}