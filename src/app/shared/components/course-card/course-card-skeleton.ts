import { Component } from '@angular/core';

/** Placeholder shown while course data is loading. */
@Component({
  selector: 'app-course-card-skeleton',
  template: `
    <div class="ca-card sk" aria-hidden="true">
      <span class="skeleton cover"></span>
      <div class="p-3">
        <span class="skeleton" style="width: 40%; height: 10px"></span>
        <span class="skeleton mt-2" style="width: 95%; height: 16px"></span>
        <span class="skeleton mt-2" style="width: 70%; height: 16px"></span>
        <span class="skeleton mt-3" style="width: 50%; height: 12px"></span>
        <span class="skeleton mt-3" style="width: 80%; height: 12px"></span>
      </div>
      <div class="p-3 border-top d-flex justify-content-between">
        <span class="skeleton" style="width: 35%; height: 20px"></span>
        <span class="skeleton" style="width: 30%; height: 30px"></span>
      </div>
    </div>
  `,
  styles: `
    .sk { overflow: hidden; height: 100%; }
    .cover { aspect-ratio: 16 / 9; border-radius: 0; }
  `,
})
export class CourseCardSkeleton {}
