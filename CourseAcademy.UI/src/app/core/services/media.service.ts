import { HttpBackend, HttpClient, HttpEventType, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiError } from '../api/api-error';
import { ApiService } from '../api/api.service';
import { MediaAssetDto, fromMediaAssetDto } from '../api/catalog.models';
import { MediaFile } from '../models/course.model';

export type MediaKind = 'video' | 'document';

/** A short-lived link to an uploaded video. `hls` needs an HLS player; `file` is a plain video file. */
export interface Playback {
  url: string;
  format: 'hls' | 'file';
  expiresAt: string;
  durationSeconds: number | null;
}

interface UploadDto {
  mediaAssetId: number;
  uploadUrl: string;
  uploadMethod: string;
  headers: Record<string, string>;
  expiresAt: string;
}

/** File types the API accepts (streaming spec). The browser's file.type is used first; this covers empty ones. */
const MIME_BY_EXTENSION: Record<string, string> = {
  mp4: 'video/mp4',
  webm: 'video/webm',
  mov: 'video/quicktime',
  pdf: 'application/pdf',
  docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  pptx: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
};

/** What the file inputs accept. */
export const ACCEPT = {
  video: '.mp4,.webm,.mov,video/mp4,video/webm,video/quicktime',
  document: '.pdf,.docx,.pptx',
} as const;

const POLL_MS = 3000;
const UPLOAD_FAILED = 'The upload did not finish. Check your connection and try again.';

/**
 * Uploaded course files (API mode only). The API decides who may upload, watch or download; the file bytes go
 * straight from the browser to the storage link the API hands out, and come back through short-lived links.
 */
@Injectable({ providedIn: 'root' })
export class MediaService {
  private readonly api = inject(ApiService);
  /** Without the app's interceptors: the upload goes to storage and must never carry the sign-in token. */
  private readonly storage = new HttpClient(inject(HttpBackend));

  /**
   * Uploads a file for a course: asks the API for an upload link, sends the bytes there (reporting progress
   * 0–100), then tells the API it is complete. The file is processed afterwards; see `waitUntilProcessed`.
   */
  async upload(courseId: number, kind: MediaKind, file: File, onProgress?: (percent: number) => void): Promise<MediaFile> {
    const upload = await this.api.post<UploadDto>('media/uploads', {
      courseId,
      kind,
      fileName: file.name,
      mimeType: mimeOf(file),
      sizeBytes: file.size,
    });
    await this.send(upload, file, onProgress);
    return fromMediaAssetDto(await this.api.post<MediaAssetDto>(`media/uploads/${upload.mediaAssetId}/complete`));
  }

  async status(mediaId: number): Promise<MediaFile> {
    return fromMediaAssetDto(await this.api.get<MediaAssetDto>(`media/assets/${mediaId}`));
  }

  /** Checks the file every few seconds until it is ready or failed, or `stop()` returns true. */
  async waitUntilProcessed(mediaId: number, onChange?: (file: MediaFile) => void, stop?: () => boolean): Promise<MediaFile | null> {
    for (;;) {
      if (stop?.()) return null;
      const file = await this.status(mediaId);
      onChange?.(file);
      if (file.status === 'ready' || file.status === 'failed' || file.status === 'deleting' || file.status === 'deleted') return file;
      await new Promise((resolve) => setTimeout(resolve, POLL_MS));
    }
  }

  playback(videoId: string): Promise<Playback> {
    return this.api.post<Playback>(`media/videos/${videoId}/playback`);
  }

  /** A short-lived download link for an uploaded section document or course material. */
  async downloadLink(kind: 'documents' | 'materials', id: string): Promise<string> {
    return (await this.api.post<{ url: string }>(`media/${kind}/${id}/download`)).url;
  }

  /**
   * Downloads an uploaded document or material through a short-lived link, once the API has checked access.
   * The link is served as a file download, so the page stays where it is.
   */
  async open(kind: 'documents' | 'materials', id: string): Promise<void> {
    const link = document.createElement('a');
    link.href = await this.downloadLink(kind, id);
    link.rel = 'noopener';
    link.click();
  }

  /** Removes a file that no lesson or material uses (e.g. an upload replaced before saving). */
  remove(mediaId: number): Promise<null> {
    return this.api.delete(`media/assets/${mediaId}`);
  }

  private send(upload: UploadDto, file: File, onProgress?: (percent: number) => void): Promise<void> {
    return new Promise((resolve, reject) => {
      this.storage
        .request(upload.uploadMethod || 'PUT', upload.uploadUrl, {
          body: file,
          headers: new HttpHeaders(upload.headers ?? {}),
          reportProgress: true,
          observe: 'events',
          responseType: 'text',
        })
        .subscribe({
          next: (event) => {
            if (event.type === HttpEventType.UploadProgress && event.total) onProgress?.(Math.round((event.loaded / event.total) * 100));
            if (event.type === HttpEventType.Response) resolve();
          },
          error: () => reject(new ApiError(0, UPLOAD_FAILED)),
        });
    });
  }
}

function mimeOf(file: File): string {
  const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
  return file.type || MIME_BY_EXTENSION[extension] || 'application/octet-stream';
}
