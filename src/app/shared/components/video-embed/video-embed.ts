import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { TranslatePipe } from '../../pipes/translate.pipe';

type Playable = { kind: 'frame'; src: SafeResourceUrl } | { kind: 'file'; src: string } | { kind: 'none' };

/**
 * Plays a YouTube or Vimeo link, or a direct .mp4/.webm file. Embed addresses
 * are rebuilt from the video id, so a pasted link can never load another page.
 */
@Component({
  selector: 'app-video-embed',
  imports: [TranslatePipe],
  template: `
    @switch (playable().kind) {
      @case ('frame') {
        <iframe class="screen" [src]="frameSrc()" [title]="title() | t" loading="lazy"
          allow="accelerometer; encrypted-media; gyroscope; picture-in-picture; fullscreen" allowfullscreen></iframe>
      }
      @case ('file') {
        <video class="screen" [src]="fileSrc()" controls preload="metadata" [attr.aria-label]="title() | t"></video>
      }
      @default {
        <div class="screen placeholder">
          <i class="bi bi-play-circle" aria-hidden="true"></i>
          <p class="mb-0">{{ 'Demo player: lesson video would play here.' | t }}</p>
        </div>
      }
    }
  `,
  styles: `
    :host { display: block; }
    .screen { display: block; width: 100%; aspect-ratio: 16 / 9; border: 0; background: var(--ca-navy); }
    .placeholder {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 1rem;
      color: var(--ca-on-dark);
      text-align: center;
    }
    .placeholder i { font-size: 3.5rem; line-height: 1; color: var(--ca-orange); }
  `,
})
export class VideoEmbed {
  private readonly sanitizer = inject(DomSanitizer);

  readonly url = input.required<string>();
  readonly title = input('');

  protected readonly playable = computed<Playable>(() => {
    const embed = embedUrl(this.url());
    if (embed) return { kind: 'frame', src: this.sanitizer.bypassSecurityTrustResourceUrl(embed) };
    if (isVideoFile(this.url())) return { kind: 'file', src: this.url() };
    return { kind: 'none' };
  });
  protected readonly frameSrc = computed(() => {
    const p = this.playable();
    return p.kind === 'frame' ? p.src : '';
  });
  protected readonly fileSrc = computed(() => {
    const p = this.playable();
    return p.kind === 'file' ? p.src : '';
  });
}

/** A trusted embed address for YouTube and Vimeo links, or null for anything else. */
export function embedUrl(url: string): string | null {
  let parsed: URL;
  try {
    parsed = new URL(url);
  } catch {
    return null;
  }
  const host = parsed.hostname.replace(/^www\./, '');
  let youtubeId: string | null = null;
  if (host === 'youtu.be') youtubeId = parsed.pathname.slice(1);
  if (host === 'youtube.com' || host === 'm.youtube.com') {
    youtubeId = parsed.pathname === '/watch' ? parsed.searchParams.get('v') : parsed.pathname.match(/^\/(?:embed|shorts)\/([^/]+)/)?.[1] ?? null;
  }
  if (youtubeId && /^[\w-]{6,20}$/.test(youtubeId)) return `https://www.youtube-nocookie.com/embed/${youtubeId}`;

  const vimeoId = host === 'vimeo.com' ? parsed.pathname.match(/^\/(\d{4,12})$/)?.[1] : undefined;
  return vimeoId ? `https://player.vimeo.com/video/${vimeoId}` : null;
}

export function isVideoFile(url: string): boolean {
  return /^https:\/\/[^\s]+\.(mp4|webm)(\?[^\s]*)?$/i.test(url);
}
