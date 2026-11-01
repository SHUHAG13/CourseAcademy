import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

/** "Why Learn With Us?" section. */
@Component({
  selector: 'app-features',
  imports: [TranslatePipe, RouterLink],
  templateUrl: './features.html',
  styleUrl: './features.scss',
})
export class Features {
  protected readonly reasons = [
    {
      icon: 'bi-tools',
      title: 'Project-based learning',
      text: 'Every module ends with something you build — an edit, a match report or a working app.',
    },
    {
      icon: 'bi-briefcase',
      title: 'Industry-relevant skills',
      text: 'Curriculum shaped by editors, journalists and engineers who hire for these roles.',
    },
    {
      icon: 'bi-person-video3',
      title: 'Expert-led courses',
      text: 'Learn from working professionals, in clear Bangla, with real examples from their work.',
    },
    {
      icon: 'bi-collection',
      title: 'Portfolio development',
      text: 'Finish each course with portfolio pieces you can show clients and employers.',
    },
    {
      icon: 'bi-rocket-takeoff',
      title: 'Career opportunities',
      text: 'Top students can work on our sports media channel, platform and software projects.',
    },
    {
      icon: 'bi-globe2',
      title: 'Real-world experience',
      text: 'Cover live matches, edit for real audiences and ship code that people actually use.',
    },
  ];
}
