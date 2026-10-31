import { DOCUMENT } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IsActiveMatchOptions, NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Avatar } from '../../shared/components/avatar/avatar';

interface NavLink {
  label: string;
  path: string;
  query?: Record<string, string>;
  exact?: boolean;
}

const EXACT: IsActiveMatchOptions = { paths: 'exact', queryParams: 'exact', matrixParams: 'ignored', fragment: 'ignored' };
const WITH_QUERY: IsActiveMatchOptions = { paths: 'exact', queryParams: 'subset', matrixParams: 'ignored', fragment: 'ignored' };
const PREFIX: IsActiveMatchOptions = { paths: 'subset', queryParams: 'ignored', matrixParams: 'ignored', fragment: 'ignored' };

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, Avatar],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
  host: {
    '(window:scroll)': 'onScroll()',
    '(document:keydown.escape)': 'menuOpen.set(false)',
  },
})
export class Navbar {
  protected readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly document = inject(DOCUMENT);

  protected readonly menuOpen = signal(false);
  protected readonly scrolled = signal(false);

  protected readonly courseLinks: NavLink[] = [
    { label: 'All Courses', path: '/courses', exact: true },
    { label: 'Video Editing', path: '/courses', query: { category: 'video-editing' } },
    { label: 'Sports Journalism', path: '/courses', query: { category: 'sports-journalism' } },
    { label: 'Development', path: '/courses', query: { category: 'software-development' } },
  ];

  protected readonly siteLinks: NavLink[] = [
    { label: 'Sports Media', path: '/sports' },
    { label: 'Career', path: '/career' },
    { label: 'About Us', path: '/about' },
  ];

  constructor() {
    inject(Router)
      .events.pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.menuOpen.set(false));

    // Prevent the page behind the drawer from scrolling.
    effect(() => {
      this.document.body.style.overflow = this.menuOpen() ? 'hidden' : '';
    });
  }

  protected onScroll(): void {
    this.scrolled.set((this.document.defaultView?.scrollY ?? 0) > 8);
  }

  protected activeOptions(link: NavLink): IsActiveMatchOptions {
    if (link.exact) return EXACT;
    return link.query ? WITH_QUERY : PREFIX;
  }

  protected logout(): void {
    this.auth.logout();
    this.menuOpen.set(false);
    this.toast.show('You have been logged out.');
  }
}
