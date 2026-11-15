import { embedUrl, isVideoFile } from './video-embed';

describe('embedUrl', () => {
  it('rebuilds YouTube and Vimeo links as embed addresses', () => {
    expect(embedUrl('https://www.youtube.com/watch?v=dQw4w9WgXcQ')).toBe('https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ');
    expect(embedUrl('https://youtu.be/dQw4w9WgXcQ')).toBe('https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ');
    expect(embedUrl('https://vimeo.com/76979871')).toBe('https://player.vimeo.com/video/76979871');
  });

  it('refuses other sites and odd ids', () => {
    expect(embedUrl('https://example.com/demo-videos/intro')).toBeNull();
    expect(embedUrl('https://evil.example/youtube.com/watch?v=abcdefg')).toBeNull();
    expect(embedUrl('https://www.youtube.com/watch?v=<script>')).toBeNull();
    expect(embedUrl('not a link')).toBeNull();
  });

  it('plays direct video files over https only', () => {
    expect(isVideoFile('https://cdn.example.com/intro.mp4')).toBe(true);
    expect(isVideoFile('http://cdn.example.com/intro.mp4')).toBe(false);
    expect(isVideoFile('https://cdn.example.com/intro.mov')).toBe(false);
  });
});
