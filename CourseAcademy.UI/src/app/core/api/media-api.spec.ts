import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localStore, sessionStore } from '../data/browser-storage';
import { DemoPaymentGateway } from '../payments/demo-payment-gateway';
import { PaymentGateway } from '../payments/payment-gateway';
import { AuthService } from '../services/auth.service';
import { MediaService } from '../services/media.service';
import { API_BASE_URL, API_MODE } from './api-mode';
import { MeDto } from './auth.models';
import { MediaAssetDto } from './catalog.models';
import { apiErrorInterceptor, authInterceptor } from './interceptors';

const BASE = 'http://api.test/api/v1';
const ok = <T>(data: T) => ({ success: true, data, message: null, errors: [] });
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

const me: MeDto = {
  id: 1, name: 'Admin', email: 'admin@courseacademy.demo', phone: null, roleCode: 'admin', roleName: 'Admin',
  permissions: ['courses.view', 'courses.edit'], courseGrants: [], hasAdminPanel: true, homeUrl: '/admin', createdAt: '2026-11-01T00:00:00Z',
};

const asset = (status: MediaAssetDto['statusCode']): MediaAssetDto => ({
  id: 9, courseId: 3, kindCode: 'video', statusCode: status, fileName: 'intro.mp4', mimeType: 'video/mp4',
  sizeBytes: 4, durationSeconds: null, errorMessage: null,
});

describe('Uploads in API mode', () => {
  let http: HttpTestingController;
  let media: MediaService;

  const settle = () => new Promise((r) => setTimeout(r, 0));

  beforeEach(async () => {
    localStore.clear();
    sessionStore.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_MODE, useValue: true },
        { provide: API_BASE_URL, useValue: BASE },
        { provide: PaymentGateway, useExisting: DemoPaymentGateway },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const login = TestBed.inject(AuthService).login(me.email, 'Admin@123');
    http.expectOne(`${BASE}/auth/login`).flush(ok({ accessToken: 'secret-token', accessTokenExpiresAt: inOneHour(), refreshToken: 'r', me }));
    await login;
    media = TestBed.inject(MediaService);
    await settle();
    for (const r of http.match(() => true)) r.flush(ok([]));
  });

  afterEach(() => http.verify());

  it('creates the upload, sends the bytes to storage without the sign-in token, then completes it', async () => {
    const progress: number[] = [];
    const file = new File(['abcd'], 'intro.mp4', { type: 'video/mp4' });
    const done = media.upload(3, 'video', file, (p) => progress.push(p));

    const create = http.expectOne(`${BASE}/media/uploads`);
    expect(create.request.headers.get('Authorization')).toBe('Bearer secret-token');
    expect(create.request.body).toEqual({ courseId: 3, kind: 'video', fileName: 'intro.mp4', mimeType: 'video/mp4', sizeBytes: 4 });
    create.flush(ok({ mediaAssetId: 9, uploadUrl: 'https://storage.test/up/9', uploadMethod: 'PUT', headers: { 'Content-Type': 'video/mp4' }, expiresAt: inOneHour() }));
    await settle();

    const put = http.expectOne('https://storage.test/up/9');
    expect(put.request.method).toBe('PUT');
    expect(put.request.body).toBe(file);
    expect(put.request.headers.get('Authorization')).toBeNull();
    expect(put.request.headers.get('Content-Type')).toBe('video/mp4');
    put.event({ type: 1, loaded: 2, total: 4 });
    put.flush('');
    await settle();

    http.expectOne(`${BASE}/media/uploads/9/complete`).flush(ok(asset('uploaded')));
    const uploaded = await done;

    expect(uploaded).toEqual({ id: 9, status: 'uploaded', fileName: 'intro.mp4', sizeBytes: 4, durationSeconds: undefined, errorMessage: undefined });
    expect(progress).toContain(50);
  });

  it('reports a failed transfer without completing the upload', async () => {
    const done = media.upload(3, 'document', new File(['x'], 'notes.pdf', { type: '' }), undefined);

    const create = http.expectOne(`${BASE}/media/uploads`);
    // An empty browser type is filled in from the extension.
    expect(create.request.body.mimeType).toBe('application/pdf');
    create.flush(ok({ mediaAssetId: 9, uploadUrl: 'https://storage.test/up/9', uploadMethod: 'PUT', headers: {}, expiresAt: inOneHour() }));
    await settle();
    http.expectOne('https://storage.test/up/9').error(new ProgressEvent('error'));

    await expect(done).rejects.toThrow('The upload did not finish. Check your connection and try again.');
  });

  it('asks the API for a short-lived playback link', async () => {
    const playback = media.playback('41');
    http.expectOne(`${BASE}/media/videos/41/playback`).flush(ok({ url: 'https://cdn.test/41.m3u8', format: 'hls', expiresAt: inOneHour(), durationSeconds: 60 }));

    expect((await playback).format).toBe('hls');
  });
});
