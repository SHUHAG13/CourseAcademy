import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FeaturedCourse } from './featured-course';

describe('FeaturedCourse', () => {
  let component: FeaturedCourse;
  let fixture: ComponentFixture<FeaturedCourse>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturedCourse]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FeaturedCourse);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
