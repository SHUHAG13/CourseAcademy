import { Component, DestroyRef, ElementRef, computed, effect, inject, input, output, signal, untracked, viewChild } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { MediaService, Playback } from '../../../core/services/media.service';
import { failureMessage } from '../../../core/services/permission.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

type Playable = { kind: 'frame'; src: SafeResourceUrl } | { kind: 'file'; src: string } | { kind: 'stream' } | { kind: 'none' };

/** Where a video file is, in whole seconds. */
export interface VideoPosition {
  position: number;
  duration: number;
}

/** How often a playing video file reports its position. */
const REPORT_EVERY_MS = 15_000;
/** A playback link that fails again this soon after being renewed is not renewed a second time. */
const RENEW_GUARD_MS = 15_000;
const CANNOT_PLAY = "This video can't be played right now. Please try again later.";

/**
 * Plays a YouTube or Vimeo link, a direct .mp4/.webm file, or an uploaded video (`uploadId`, API mode).
 * Embed addresses are rebuilt from the video id, so a pasted link can never load another page.
 * Uploads get a short-lived link from the API (HLS through hls.js, loaded only when needed); when the link
 * expires during playback a new one is fetched and the video continues where it was.
 * Only video files and uploads can start at `startAt` and report `watched`; embeds don't expose their position.
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
        <video #player class="screen" [src]="fileSrc()" controls preload="metadata" [attr.aria-label]="title() | t"
          (loadedmetadata)="resume(player)" (timeupdate)="report(player)" (pause)="report(player, true)" (ended)="report(player, true)"></video>
      }
      @case ('stream') {
        @if (failure(); as message) {
          <div class="screen placeholder" role="alert">
            <i class="bi bi-exclamation-circle" aria-hidden="true"></i>
            <p class="mb-0">{{ message | t }}</p>
          </div>
        } @else {
          <video #player class="screen" controls preload="metadata" [attr.aria-label]="title() | t" (loadedmetadata)="resume(player)"
            (timeupdate)="report(player)" (pause)="report(player, true)" (ended)="report(player, true)" (error)="linkFailed(player)"></video>
        }
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
  private readonly media = inject(MediaService);

  readonly url = input('');
  /** API mode: the id of an uploaded video, played through a short-lived link from the API. */
  readonly uploadId = input<string | null>(null);
  readonly title = input('');
  /** Seconds to resume from. Ignored near the end, so a finished video starts again. */
  readonly startAt = input(0);
  /** While a video file plays: every 15 seconds, on pause and at the end. */
  readonly watched = output<VideoPosition>();

  private readonly player = viewChild<ElementRef<HTMLVideoElement>>('player');
  protected readonly stream = signal<Playback | null>(null);
  protected readonly failure = signal<string | null>(null);
  private hls: { destroy(): void } | null = null;
  private lastReport = 0;
  private lastRenewal = 0;
  /** Where to continue after a renewed link, instead of `startAt`. */
  private continueAt: number | null = null;

  constructor() {
    // A new upload id: fetch its playback link.
    effect(() => {
      const id = this.uploadId();
      untracked(() => {
        this.continueAt = null;
        void this.loadStream(id);
      });
    });
    // A link and a player element: start playback.
    effect((onCleanup) => {
      const stream = this.stream();
      const element = this.player()?.nativeElement;
      if (!stream || !element) return;
      let cancelled = false;
      void this.attach(element, stream, () => cancelled);
      onCleanup(() => {
        cancelled = true;
        this.detach();
      });
    });
    inject(DestroyRef).onDestroy(() => this.detach());
  }

  protected readonly playable = computed<Playable>(() => {
    if (this.uploadId()) return { kind: 'stream' };
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

  protected resume(player: HTMLVideoElement): void {
    this.lastReport = Date.now();
    const start = this.continueAt ?? this.startAt();
    this.continueAt = null;
    if (start > 0 && start < player.duration - 5) player.currentTime = start;
  }

  protected report(player: HTMLVideoElement, now = false): void {
    if (!Number.isFinite(player.duration) || player.duration <= 0) return;
    if (!now && Date.now() - this.lastReport < REPORT_EVERY_MS) return;
    this.lastReport = Date.now();
    this.watched.emit({ position: Math.floor(player.currentTime), duration: Math.max(1, Math.round(player.duration)) });
  }

  /** The upload's link stopped working (usually expired): get a new one and continue at the same point, once. */
  protected linkFailed(player: HTMLVideoElement): void {
    const id = this.uploadId();
    if (!id) return;
    if (Date.now() - this.lastRenewal < RENEW_GUARD_MS) {
      this.failure.set(CANNOT_PLAY);
      return;
    }
    this.lastRenewal = Date.now();
    this.continueAt = Math.floor(player.currentTime) || null;
    void this.loadStream(id);
  }

  private async loadStream(id: string | null): Promise<void> {
    this.stream.set(null);
    this.failure.set(null);
    if (!id) return;
    try {
      const playback = await this.media.playback(id);
      if (this.uploadId() === id) this.stream.set(playback);
    } catch (error) {
      if (this.uploadId() === id) this.failure.set(failureMessage(error, CANNOT_PLAY));
    }
  }

  /** Plain files and Safari play HLS natively; other browsers use hls.js, loaded only now. */
  private async attach(element: HTMLVideoElement, stream: Playback, cancelled: () => boolean): Promise<void> {
    if (stream.format === 'file' || element.canPlayType('application/vnd.apple.mpegurl')) {
      element.src = stream.url;
      return;
    }
    const { default: Hls } = await import('hls.js');
    if (cancelled()) return;
    if (!Hls.isSupported()) {
      this.failure.set("This browser can't play this video. Please use an up-to-date browser.");
      return;
    }
    const hls = new Hls();
    hls.on(Hls.Events.ERROR, (_event, data) => {
      if (data.fatal) this.linkFailed(element);
    });
    hls.loadSource(stream.url);
    hls.attachMedia(element);
    this.hls = hls;
  }

  private detach(): void {
    this.hls?.destroy();
    this.hls = null;
  }
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
