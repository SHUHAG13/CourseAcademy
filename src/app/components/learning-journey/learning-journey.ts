import { Component } from '@angular/core';

@Component({
  selector: 'app-learning-journey',
  templateUrl: './learning-journey.html',
  styleUrl: './learning-journey.scss',
})
export class LearningJourney {
  protected readonly steps = [
    { icon: 'bi-play-btn', title: 'Learn', text: 'Short, focused video lessons from industry experts.' },
    { icon: 'bi-pencil', title: 'Practice', text: 'Assignments after every module with feedback.' },
    { icon: 'bi-folder2-open', title: 'Build Portfolio', text: 'Finish real projects you can show anyone.' },
    { icon: 'bi-briefcase', title: 'Get Opportunities', text: 'Freelance, internships and work with our teams.' },
    { icon: 'bi-graph-up-arrow', title: 'Grow Your Career', text: 'Keep levelling up with advanced courses.' },
  ];
}
