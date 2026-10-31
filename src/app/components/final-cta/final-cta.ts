import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-final-cta',
  imports: [RouterLink],
  template: `
    <section class="section-tight" aria-labelledby="final-cta-title">
      <div class="container">
        <div class="cta">
          <div>
            <h2 id="final-cta-title">Your Next Skill Could Change Your Future.</h2>
            <p>Start with a beginner-friendly course today. Lifetime access, Bangla instruction, real projects.</p>
          </div>
          <a routerLink="/courses" class="btn btn-lg btn-accent">
            Explore All Courses <i class="bi bi-arrow-right" aria-hidden="true"></i>
          </a>
        </div>
      </div>
    </section>
  `,
  styles: `
    .cta {
      position: relative;
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 1.5rem;
      padding: clamp(2rem, 5vw, 3.5rem);
      border-radius: var(--ca-radius-lg);
      background: var(--ca-navy);
      overflow: hidden;
    }
    .cta::before {
      content: '';
      position: absolute;
      right: -60px;
      top: -60px;
      width: 240px;
      height: 240px;
      border: 40px solid rgba(249, 115, 22, 0.14);
      border-radius: 50%;
    }
    .cta > * { position: relative; }
    h2 {
      margin-bottom: 0.5rem;
      font-size: clamp(1.5rem, 1.1rem + 1.6vw, 2.2rem);
      color: #fff;
    }
    p { margin: 0; max-width: 560px; color: var(--ca-on-dark-muted); }
  `,
})
export class FinalCta {}
