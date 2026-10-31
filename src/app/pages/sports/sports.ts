import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SPORTS_STORIES, SportsStory, SportsTopic } from '../../core/data/site.data';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import { CourseCard } from '../../shared/components/course-card/course-card';

@Component({
  selector: 'app-sports',
  imports: [RouterLink, DatePipe, CourseCard],
  templateUrl: './sports.html',
  styleUrl: './sports.scss',
})
export class Sports {
  private readonly toast = inject(ToastService);
  private readonly courseService = inject(CourseService);

  protected readonly topics: (SportsTopic | 'All')[] = ['All', 'Cricket', 'Football', 'Analysis', 'Interview', 'Journalism'];
  protected readonly topic = signal<SportsTopic | 'All'>('All');

  protected readonly stories = computed(() => {
    const t = this.topic();
    return t === 'All' ? SPORTS_STORIES : SPORTS_STORIES.filter((s) => s.topic === t);
  });

  protected readonly journalismCourses = this.courseService.all
    .filter((c) => c.category === 'sports-journalism')
    .sort((a, b) => b.students - a.students)
    .slice(0, 3);

  protected open(story: SportsStory): void {
    this.toast.show(`Full articles will be published when our sports media channel launches. “${story.title}” is a preview.`);
  }
}
